using backend.Data;
using backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace backend.Controllers
{
    /**
     * ISTPET Reports Controller
     * Generates data-dense reports for high-level administration.
     * Matches the visual structure of institutional SIGAFI records.
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("practicas")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ReportePracticasDTO>>>> GetReportePracticas(
            [FromQuery] string? fechaInicio, 
            [FromQuery] string? fechaFin,
            [FromQuery] string? instructorId)
        {
            try
            {
                var query = from s in _context.RegistrosSalida
                            join m in _context.Matriculas on s.IdMatricula equals m.Id_Matricula
                            join e in _context.Estudiantes on m.CedulaEstudiante equals e.Cedula
                            join i in _context.Instructores on s.IdInstructor equals i.Id_Instructor
                            join v in _context.Vehiculos on s.IdVehiculo equals v.Id_Vehiculo
                            join tl in _context.TipoLicencias on v.IdTipoLicencia equals tl.Id_Tipo
                            join l in _context.RegistrosLlegada on s.Id_Registro equals l.IdRegistro into arrival
                            from llegada in arrival.DefaultIfEmpty()
                            select new { s, e, i, v, tl, llegada };

                // Filtros
                if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out var start))
                {
                    query = query.Where(x => x.s.FechaHoraSalida >= start);
                }

                if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out var end))
                {
                    // Ajustar a fin de día
                    var endOfDay = end.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.s.FechaHoraSalida <= endOfDay);
                }

                if (!string.IsNullOrEmpty(instructorId) && int.TryParse(instructorId, out var instId))
                {
                    query = query.Where(x => x.s.IdInstructor == instId);
                }

                var data = await query
                    .OrderByDescending(x => x.s.FechaHoraSalida)
                    .ToListAsync();

                var result = data.Select(x => {
                    var duracion = x.llegada != null ? (x.llegada.FechaHoraLlegada - x.s.FechaHoraSalida) : TimeSpan.Zero;
                    var culture = new CultureInfo("es-EC");

                    return new ReportePracticasDTO
                    {
                        IdRegistro = x.s.Id_Registro,
                        IdProfesor = x.i.Cedula,
                        Profesor = $"{x.i.Apellidos} {x.i.Nombres}".ToUpper(),
                        Categoria = $"LICENCIA TIPO {x.tl.Codigo}".ToUpper(),
                        NumeroVehiculo = x.v.NumeroVehiculo,
                        IdAlumno = x.e.Cedula,
                        Nomina = $"{x.e.Apellidos} {x.e.Nombres}".ToUpper(),
                        Dia = culture.DateTimeFormat.GetDayName(x.s.FechaHoraSalida.DayOfWeek).ToLower(),
                        Fecha = x.s.FechaHoraSalida.ToString("dd/M/yyyy"),
                        HoraSalida = x.s.FechaHoraSalida.ToString("HH:mm:ss"),
                        HoraLlegada = x.llegada?.FechaHoraLlegada.ToString("HH:mm:ss"),
                        Tiempo = string.Format("{0:D2}:{1:D2}:{2:D2}", duracion.Hours, duracion.Minutes, duracion.Seconds),
                        Observaciones = x.s.ObservacionesSalida
                    };
                });

                return Ok(ApiResponse<IEnumerable<ReportePracticasDTO>>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<ReportePracticasDTO>>.Fail($"Error generating report: {ex.Message}"));
            }
        }
    }
}
