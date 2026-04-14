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

        [HttpGet("historial-practicas")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ReportePracticasDTO>>>> GetHistorialPracticas(
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin,
            [FromQuery] string? instructorId,
            [FromQuery] string? busqueda,
            [FromQuery] string? estado,
            [FromQuery] int limit = 200)
        {
            try
            {
                var take = Math.Clamp(limit, 1, 500);
                var culture = new System.Globalization.CultureInfo("es-EC");

                DateTime? desde = null;
                DateTime? hasta = null;
                if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out var s))
                    desde = s.Date;
                if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out var e))
                    hasta = e.Date;

                // Si no se especifica rango, devolver hoy por defecto
                if (!desde.HasValue && !hasta.HasValue)
                {
                    desde = DateTime.Today;
                    hasta = DateTime.Today;
                }

                var q = _context.Practicas.AsNoTracking();

                if (desde.HasValue)
                    q = q.Where(p => p.fecha >= desde.Value.Date);
                if (hasta.HasValue)
                    q = q.Where(p => p.fecha <= hasta.Value.Date);
                if (!string.IsNullOrWhiteSpace(instructorId))
                    q = q.Where(p => p.idProfesor == instructorId.Trim());

                // Filtro por estado: en_pista = sin llegada y no cancelado, completada = con llegada, cancelada
                if (!string.IsNullOrWhiteSpace(estado))
                {
                    switch (estado.ToLowerInvariant())
                    {
                        case "en_pista":
                            q = q.Where(p => p.hora_llegada == null && (p.cancelado == null || p.cancelado == 0));
                            break;
                        case "completada":
                            q = q.Where(p => p.hora_llegada != null && (p.cancelado == null || p.cancelado == 0));
                            break;
                        case "cancelada":
                            q = q.Where(p => p.cancelado == 1);
                            break;
                    }
                }

                var rows = await (
                    from p in q
                    join a in _context.Estudiantes.AsNoTracking() on p.idalumno equals a.idAlumno into alumnosJoin
                    from a in alumnosJoin.DefaultIfEmpty()
                    join v in _context.Vehiculos.AsNoTracking() on p.idvehiculo equals v.idVehiculo into vehiculosJoin
                    from v in vehiculosJoin.DefaultIfEmpty()
                    join pr in _context.Instructores.AsNoTracking() on p.idProfesor equals pr.idProfesor into profesJoin
                    from pr in profesJoin.DefaultIfEmpty()
                    orderby p.fecha descending, p.hora_salida descending
                    select new { p, a, v, pr }).ToListAsync();

                // Filtro por búsqueda de texto (alumno / instructor) en memoria tras el join
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var term = busqueda.Trim().ToUpperInvariant();
                    rows = rows.Where(x =>
                    {
                        var nomAlumno = x.a != null
                            ? $"{x.a.apellidoPaterno} {x.a.apellidoMaterno} {x.a.primerNombre} {x.a.segundoNombre} {x.p.idalumno}".ToUpperInvariant()
                            : x.p.idalumno.ToUpperInvariant();
                        var nomProf = x.pr != null
                            ? $"{x.pr.apellidos} {x.pr.nombres} {x.p.idProfesor}".ToUpperInvariant()
                            : x.p.idProfesor.ToUpperInvariant();
                        var numVeh = x.v?.numero_vehiculo ?? "";
                        return nomAlumno.Contains(term) || nomProf.Contains(term) || numVeh.Contains(term);
                    }).ToList();
                }

                var list = rows.Take(take).Select(x =>
                {
                    var p = x.p; var a = x.a; var v = x.v; var pr = x.pr;
                    var fecha = p.fecha.Date;
                    var marca = (v?.marca ?? "").Trim();
                    var modelo = (v?.modelo ?? "").Trim();
                    var placa = v?.placa ?? "";
                    var categoriaDetalle = string.Join(" ", new[] { marca, modelo }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    var categoria = !string.IsNullOrWhiteSpace(categoriaDetalle)
                        ? culture.TextInfo.ToUpper(categoriaDetalle)
                        : (!string.IsNullOrWhiteSpace(placa) ? culture.TextInfo.ToUpper($"Placa {placa.Trim()}") : "PRÁCTICA");

                    var tsSalida = p.hora_salida;
                    var tsLlegada = p.hora_llegada;
                    var duracion = TimeSpan.Zero;
                    if (tsSalida.HasValue && tsLlegada.HasValue)
                    {
                        duracion = tsLlegada.Value - tsSalida.Value;
                        if (duracion < TimeSpan.Zero) duracion = TimeSpan.Zero;
                    }

                    var profNom = pr != null ? $"{pr.apellidos} {pr.nombres}".Trim() : p.idProfesor;
                    var alumNom = a != null ? $"{a.apellidoPaterno} {a.apellidoMaterno} {a.primerNombre} {a.segundoNombre}".Trim() : p.idalumno;

                    return new ReportePracticasDTO
                    {
                        idPractica = p.idPractica,
                        idProfesor = p.idProfesor,
                        profesor = culture.TextInfo.ToUpper(profNom),
                        categoria = categoria,
                        numeroVehiculo = v?.numero_vehiculo ?? "?",
                        idAlumno = a?.idAlumno ?? p.idalumno,
                        nomina = culture.TextInfo.ToUpper(alumNom),
                        dia = culture.DateTimeFormat.GetDayName(fecha.DayOfWeek).ToLowerInvariant(),
                        fecha = fecha.ToString("dd/MM/yyyy"),
                        horaSalida = tsSalida.HasValue ? DateTime.Today.Add(tsSalida.Value).ToString("HH:mm:ss") : "--",
                        horaLlegada = tsLlegada.HasValue ? DateTime.Today.Add(tsLlegada.Value).ToString("HH:mm:ss") : null,
                        tiempo = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                            (int)duracion.TotalHours, duracion.Minutes, duracion.Seconds),
                        cancelado = p.cancelado ?? 0,
                        userSalida = p.user_asigna,
                        userLlegada = p.user_llegada,
                        enSalida = (p.ensalida ?? 0) == 1
                    };
                }).ToList();

                return Ok(ApiResponse<IEnumerable<ReportePracticasDTO>>.Ok(list, $"Historial local: {list.Count} registros."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<ReportePracticasDTO>>.Fail($"Historial: {ex.Message}"));
            }
        }
    }
}
