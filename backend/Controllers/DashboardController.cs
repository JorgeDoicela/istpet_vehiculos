using backend.Data;
using backend.Models;
using backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    /**
     * ISTPET Enterprise Dashboard Controller
     * Provides data for the Apple Light 2026 KPIs and real-time monitoring.
     */
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("clases-activas")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ClaseActiva>>>> GetClasesActivas()
        {
            try 
            {
                var clases = await _context.ClasesActivas.ToListAsync();
                return Ok(ApiResponse<IEnumerable<ClaseActiva>>.Ok(clases));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<ClaseActiva>>.Fail($"Dashboard Error: {ex.Message}"));
            }
        }

        [HttpGet("alertas-mantenimiento")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AlertaMantenimiento>>>> GetAlertasMantenimiento()
        {
            try 
            {
                var alertas = await _context.AlertasMantenimiento.ToListAsync();
                return Ok(ApiResponse<IEnumerable<AlertaMantenimiento>>.Ok(alertas));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<AlertaMantenimiento>>.Fail($"Maintenance Error: {ex.Message}"));
            }
        }
    }
}
