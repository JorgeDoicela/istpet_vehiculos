using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Services.Interfaces;
using backend.Data;
using backend.Models;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,logistica")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDataSyncService _syncService;

        public DashboardController(AppDbContext context, IDataSyncService syncService)
        {
            _context = context;
            _syncService = syncService;
        }

        [HttpPost("sync-users")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<ApiResponse<SyncLog>>> SyncUsers()
        {
            var result = await _syncService.SyncWebUsersAsync();
            return Ok(ApiResponse<SyncLog>.Ok(result, "Sincronización de usuarios de SIGAFI completada."));
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
