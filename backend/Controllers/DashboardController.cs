using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Services.Interfaces;
using backend.Services.Helpers;
using backend.Data;
using backend.Models;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,logistica,guardia")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDataSyncService _syncService;
        private readonly ICentralStudentProvider _central;
        private readonly IAgendaPanelService _agendaPanel;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            AppDbContext context,
            IDataSyncService syncService,
            ICentralStudentProvider central,
            IAgendaPanelService agendaPanel,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _syncService = syncService;
            _central = central;
            _agendaPanel = agendaPanel;
            _logger = logger;
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
                var desdeSigafi = await _central.GetClasesActivasEnRutaFromCentralAsync();
                List<ClaseActiva> desdeLocal = new();
                try
                {
                    desdeLocal = await _context.ClasesActivas.ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Vista local v_clases_activas no disponible; se usa solo SIGAFI.");
                }

                var merged = SigafiLocalReadMerge.MergeClasesActivas(desdeSigafi, desdeLocal);
                return Ok(ApiResponse<IEnumerable<ClaseActiva>>.Ok(merged,
                    "SIGAFI + espejo local (sin duplicar por idPractica)."));
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
                var desdeSigafi = await _central.GetAlertasVehiculoDesdeCentralAsync();
                List<AlertaMantenimiento> desdeLocal = new();
                try
                {
                    desdeLocal = await _context.AlertasMantenimiento.ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Vista local v_alerta_mantenimiento no disponible; se usa solo SIGAFI.");
                }

                var merged = SigafiLocalReadMerge.MergeAlertasVehiculo(desdeSigafi, desdeLocal);
                return Ok(ApiResponse<IEnumerable<AlertaMantenimiento>>.Ok(merged,
                    "SIGAFI (vehículos inactivos) + alertas locales (sin duplicar por id_vehiculo)."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<AlertaMantenimiento>>.Fail($"Maintenance Error: {ex.Message}"));
            }
        }

        [HttpGet("agenda-reciente")]
        public async Task<ActionResult<ApiResponse<AgendaLogisticaResponseDto>>> GetAgendaReciente([FromQuery] int limit = 100)
        {
            try
            {
                var take = Math.Clamp(limit, 1, 200);
                var payload = await _agendaPanel.GetAgendaAsync(take);
                return Ok(ApiResponse<AgendaLogisticaResponseDto>.Ok(payload));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AgendaLogisticaResponseDto>.Fail($"Agenda: {ex.Message}"));
            }
        }

        [HttpGet("agenda-historial")]
        public async Task<ActionResult<ApiResponse<AgendaLogisticaResponseDto>>> GetAgendaHistorial([FromQuery] int limit = 50)
        {
            try
            {
                var take = Math.Clamp(limit, 1, 100);
                var payload = await _agendaPanel.GetTodayHistoryAsync(take);
                return Ok(ApiResponse<AgendaLogisticaResponseDto>.Ok(payload));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AgendaLogisticaResponseDto>.Fail($"Historial: {ex.Message}"));
            }
        }
    }
}
