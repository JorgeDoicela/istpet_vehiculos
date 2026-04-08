using backend.Data;
using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    /**
     * Reportes administrativos desde la BD central SIGAFI (prácticas agendadas / histórico en cond_alumnos_practicas).
     * La conexión se define en ConnectionStrings:SigafiConnection (IP/host del servidor SIGAFI).
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISigafiReportService _sigafiReports;

        public ReportsController(AppDbContext context, ISigafiReportService sigafiReports)
        {
            _context = context;
            _sigafiReports = sigafiReports;
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
                if (!string.IsNullOrEmpty(instructorId) && int.TryParse(instructorId, out var instPk))
                {
                    cedulaProfesor = await _context.Instructores
                        .Where(i => i.Id_Instructor == instPk)
                        .Select(i => i.Cedula)
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrEmpty(cedulaProfesor))
                        return Ok(ApiResponse<IEnumerable<ReportePracticasDTO>>.Ok(Array.Empty<ReportePracticasDTO>(), "Instructor no encontrado; sin resultados."));
                }

                var result = await _sigafiReports.GetReportePracticasAsync(desde, hasta, cedulaProfesor);
                return Ok(ApiResponse<IEnumerable<ReportePracticasDTO>>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<ReportePracticasDTO>>.Fail($"Error generando reporte SIGAFI: {ex.Message}"));
            }
        }
    }
}
