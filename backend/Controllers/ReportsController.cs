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

                var desdeSigafi = await _sigafiReports.GetReportePracticasAsync(desde, hasta, cedulaProfesor);
                var desdeLocal = await BuildReportePracticasLocalAsync(desde, hasta, cedulaProfesor);
                var merged = SigafiLocalReadMerge.MergeReportePracticas(desdeSigafi, desdeLocal);
                return Ok(ApiResponse<IEnumerable<ReportePracticasDTO>>.Ok(merged,
                    "SIGAFI + espejo local (sin duplicar por idPractica)."));
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
            var q = _context.Practicas.AsNoTracking()
                .Where(p => (p.cancelado ?? 0) == 0);

            if (fechaInicio.HasValue)
                q = q.Where(p => p.fecha >= fechaInicio.Value.Date);
            if (fechaFin.HasValue)
                q = q.Where(p => p.fecha <= fechaFin.Value.Date);
            if (!string.IsNullOrWhiteSpace(cedulaProfesor))
                q = q.Where(p => p.idProfesor == cedulaProfesor);

            var rows = await (
                from p in q
                join a in _context.Estudiantes.AsNoTracking() on p.idalumno equals a.idAlumno
                join v in _context.Vehiculos.AsNoTracking() on p.idvehiculo equals v.idVehiculo
                join pr in _context.Instructores.AsNoTracking() on p.idProfesor equals pr.idProfesor
                select new { p, a, v, pr }).ToListAsync();

            var list = new List<ReportePracticasDTO>(rows.Count);
            foreach (var x in rows)
            {
                var fecha = x.p.fecha.Date;
                var marca = (x.v.marca ?? "").Trim();
                var modelo = (x.v.modelo ?? "").Trim();
                var placa = x.v.placa ?? "";
                var categoriaDetalle = string.Join(" ", new[] { marca, modelo }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var categoria = !string.IsNullOrWhiteSpace(categoriaDetalle)
                    ? culture.TextInfo.ToUpper(categoriaDetalle)
                    : (!string.IsNullOrWhiteSpace(placa)
                        ? culture.TextInfo.ToUpper($"Placa {placa.Trim()}")
                        : culture.TextInfo.ToUpper("Práctica local"));


                var tsSalida = x.p.hora_salida;
                var tsLlegada = x.p.hora_llegada;
                var duracion = TimeSpan.Zero;
                if (tsSalida.HasValue && tsLlegada.HasValue)
                {
                    duracion = tsLlegada.Value - tsSalida.Value;
                    if (duracion < TimeSpan.Zero)
                        duracion = TimeSpan.Zero;
                }

                var profNom = $"{x.pr.apellidos} {x.pr.nombres}".Trim();
                var alumNom = $"{x.a.apellidoPaterno} {x.a.apellidoMaterno} {x.a.primerNombre} {x.a.segundoNombre}".Trim();

                list.Add(new ReportePracticasDTO
                {
                    idPractica = x.p.idPractica,
                    idProfesor = x.p.idProfesor,
                    profesor = culture.TextInfo.ToUpper(profNom),
                    categoria = categoria,
                    numeroVehiculo = x.v.numero_vehiculo ?? "0",
                    idAlumno = x.a.idAlumno,
                    nomina = culture.TextInfo.ToUpper(alumNom),
                    dia = culture.DateTimeFormat.GetDayName(fecha.DayOfWeek).ToLowerInvariant(),
                    fecha = fecha.ToString("dd/M/yyyy"),
                    horaSalida = tsSalida.HasValue ? DateTime.Today.Add(tsSalida.Value).ToString("HH:mm") : "--",
                    horaLlegada = tsLlegada.HasValue ? DateTime.Today.Add(tsLlegada.Value).ToString("HH:mm") : null,
                    tiempo = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                        (int)duracion.TotalHours, duracion.Minutes, duracion.Seconds),
                    observaciones = x.p.observaciones
                });
            }

            return list;
        }
    }
}
