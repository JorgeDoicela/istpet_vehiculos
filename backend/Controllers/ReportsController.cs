using backend.Data;
using backend.DTOs;
using backend.Services.Helpers;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace backend.Controllers
{
    /**
     * Reportes: SIGAFI primero + espejo local sin duplicar (por idPractica).
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISigafiReportService _sigafiReports;
        private readonly ICentralStudentProvider _central;

        public ReportsController(
            AppDbContext context,
            ISigafiReportService sigafiReports,
            ICentralStudentProvider central)
        {
            _context = context;
            _sigafiReports = sigafiReports;
            _central = central;
        }

        [HttpGet("practicas")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ReportePracticasDTO>>>> GetReportePracticas(
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin,
            [FromQuery] string? instructorId)
        {
            try
            {
                DateTime? desde = null;
                DateTime? hasta = null;
                if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out var s))
                    desde = s.Date;
                if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out var e))
                    hasta = e.Date;

                string? cedulaProfesor = null;
                if (!string.IsNullOrWhiteSpace(instructorId))
                {
                    var key = instructorId.Trim();
                    var centralInstr = await _central.GetInstructorFromCentralAsync(key);
                    if (centralInstr != null)
                        cedulaProfesor = centralInstr.idProfesor;
                    else
                    {
                        cedulaProfesor = await _context.Instructores.AsNoTracking()
                            .Where(i => i.idProfesor == key)
                            .Select(i => i.idProfesor)
                            .FirstOrDefaultAsync();
                    }

                    if (string.IsNullOrEmpty(cedulaProfesor))
                    {
                        return Ok(ApiResponse<IEnumerable<ReportePracticasDTO>>.Ok(
                            Array.Empty<ReportePracticasDTO>(),
                            "Instructor no encontrado en SIGAFI ni en el espejo local."));
                    }
                }

                // Ejecución en PARALELO para máxima eficiencia, esperando a que AMBOS terminen
                var taskSigafi = _sigafiReports.GetReportePracticasAsync(desde, hasta, cedulaProfesor);
                var taskLocal = BuildReportePracticasLocalAsync(desde, hasta, cedulaProfesor);

                await Task.WhenAll(taskSigafi, taskLocal);
                
                var desdeSigafi = await taskSigafi;
                var desdeLocal = await taskLocal;
                
                Console.WriteLine($"[REPORTS-SYNC] SIGAFI: {desdeSigafi.Count} | Local: {desdeLocal.Count} | Total Real: {desdeSigafi.Count + desdeLocal.Count}");
                
                var merged = SigafiLocalReadMerge.MergeReportePracticas(desdeSigafi, desdeLocal);
                Console.WriteLine($"[REPORTS-RESULT] Merged total (deduplicated): {merged.Count} items.");

                return Ok(ApiResponse<IEnumerable<ReportePracticasDTO>>.Ok(merged, "Reporte unificado (SIGAFI + Espejo local)."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<ReportePracticasDTO>>.Fail($"Error: {ex.Message}"));
            }
        }

        private async Task<List<ReportePracticasDTO>> BuildReportePracticasLocalAsync(
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? cedulaProfesor)
        {
            var culture = new CultureInfo("es-EC");
            var q = _context.Practicas.AsNoTracking();

            if (fechaInicio.HasValue)
                q = q.Where(p => p.fecha >= fechaInicio.Value.Date);
            if (fechaFin.HasValue)
                q = q.Where(p => p.fecha <= fechaFin.Value.Date);
            if (!string.IsNullOrWhiteSpace(cedulaProfesor))
                q = q.Where(p => p.idProfesor == cedulaProfesor);

            var rows = await (
                from p in q
                join a in _context.Estudiantes.AsNoTracking() on p.idalumno equals a.idAlumno into alumnosJoin
                from a in alumnosJoin.DefaultIfEmpty()
                join v in _context.Vehiculos.AsNoTracking() on p.idvehiculo equals v.idVehiculo into vehiculosJoin
                from v in vehiculosJoin.DefaultIfEmpty()
                join pr in _context.Instructores.AsNoTracking() on p.idProfesor equals pr.idProfesor into profesJoin
                from pr in profesJoin.DefaultIfEmpty()
                select new { p, a, v, pr }).ToListAsync();

            var list = new List<ReportePracticasDTO>(rows.Count);
            foreach (var x in rows)
            {
                var p = x.p;
                var a = x.a;
                var v = x.v;
                var pr = x.pr;

                var fecha = p.fecha.Date;
                var marca = (v?.marca ?? "").Trim();
                var modelo = (v?.modelo ?? "").Trim();
                var placa = v?.placa ?? "";
                var numeroVehiculo = v?.numero_vehiculo ?? "0";

                // Para reportes locales, usamos como predeterminado 'LICENCIA TIPO C' (alineado con la solicitud del usuario)
                var categoria = culture.TextInfo.ToUpper("LICENCIA TIPO C");

                var tsSalida = p.hora_salida;
                var tsLlegada = p.hora_llegada;
                var duracion = TimeSpan.Zero;
                if (tsSalida.HasValue && tsLlegada.HasValue)
                {
                    duracion = tsLlegada.Value - tsSalida.Value;
                    if (duracion < TimeSpan.Zero)
                        duracion = TimeSpan.Zero;
                }

                var profNom = pr != null ? $"{pr.apellidos} {pr.nombres}".Trim() : p.idProfesor;
                var alumNom = a != null ? $"{a.apellidoPaterno} {a.apellidoMaterno} {a.primerNombre} {a.segundoNombre}".Trim() : p.idalumno;


                list.Add(new ReportePracticasDTO
                {
                    idPractica = p.idPractica,
                    idProfesor = p.idProfesor,
                    profesor = culture.TextInfo.ToUpper(profNom),
                    categoria = categoria,
                    numeroVehiculo = numeroVehiculo,
                    idAlumno = a?.idAlumno ?? p.idalumno,
                    nomina = culture.TextInfo.ToUpper(alumNom),
                    dia = culture.DateTimeFormat.GetDayName(fecha.DayOfWeek).ToLowerInvariant(),
                    fecha = fecha.ToString("dd/M/yyyy"),
                    horaSalida = tsSalida.HasValue ? DateTime.Today.Add(tsSalida.Value).ToString("HH:mm") : "--",
                    horaLlegada = tsLlegada.HasValue ? DateTime.Today.Add(tsLlegada.Value).ToString("HH:mm") : null,
                    tiempo = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                        (int)duracion.TotalHours, duracion.Minutes, duracion.Seconds),
                    cancelado = p.cancelado ?? 0
                });
            }

            return list;
        }
    }
}
