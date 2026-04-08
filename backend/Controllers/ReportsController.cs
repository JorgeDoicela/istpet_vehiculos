using backend.Data;
using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    /**
     * Reports Controller: Refactored 2026.
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
                if (!string.IsNullOrEmpty(instructorId))
                {
                    cedulaProfesor = await _context.Instructores
                        .Where(i => i.idProfesor == instructorId)
                        .Select(i => i.idProfesor)
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrEmpty(cedulaProfesor))
                        return Ok(ApiResponse<IEnumerable<ReportePracticasDTO>>.Ok(Array.Empty<ReportePracticasDTO>(), "Instructor no encontrado."));
                }

                var result = await _sigafiReports.GetReportePracticasAsync(desde, hasta, cedulaProfesor);
                return Ok(ApiResponse<IEnumerable<ReportePracticasDTO>>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<ReportePracticasDTO>>.Fail($"Error: {ex.Message}"));
            }
        }
    }
}
