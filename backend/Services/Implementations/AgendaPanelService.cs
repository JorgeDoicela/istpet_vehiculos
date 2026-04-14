using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Data;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class AgendaPanelService : IAgendaPanelService
    {
        private readonly AppDbContext _context;
        private readonly ICentralStudentProvider _central;
        private readonly ISigafiMirrorPersistenceService _mirrorPersist;

        public AgendaPanelService(
            AppDbContext context,
            ICentralStudentProvider central,
            ISigafiMirrorPersistenceService mirrorPersist)
        {
            _context = context;
            _central = central;
            _mirrorPersist = mirrorPersist;
        }

        public async Task<AgendaLogisticaResponseDto> GetAgendaAsync(int limit = 100)
        {
            var take = Math.Clamp(limit, 1, 200);
            var fromSigafi = true;
            var list = (await _central.GetRecentSchedulesAsync(take)).ToList();
            /* 
            // COMENTADO: Ahora la fuente es 100% SIGAFI (Directo), se omite el fallback al espejo local.
            if (list.Count == 0)
            {
                list = await GetRecentSchedulesFromLocalMirrorAsync(take);
                fromSigafi = false;
            }
            */

            // await EnrichAgendaEstadoOperativoAsync(list);

            // if (fromSigafi && list.Count > 0)
            //     await _mirrorPersist.PersistPracticesFromScheduleDtosAsync(list);

            return new AgendaLogisticaResponseDto
            {
                Practicas = list,
                FuenteDatos = fromSigafi ? "sigafi" : "local",
                ObtenidoEn = DateTime.UtcNow
            };
        }

        private static string ResolverEstadoOperativoPractica(byte? cancelado, byte? ensalida, TimeSpan? horaLlegada)
        {
            if ((cancelado ?? 0) != 0)
                return "cancelada";
            if (horaLlegada != null)
                return "completada";
            if (ensalida == 1)
                return "en_pista";
            return "pendiente";
        }

/* 
        private async Task EnrichAgendaEstadoOperativoAsync(List<ScheduledPracticeDto> list)
        {
            if (list.Count == 0)
                return;

            var ids = list.Select(x => x.idPractica).Distinct().ToList();
            var states = await _context.Practicas.AsNoTracking()
                .Where(p => ids.Contains(p.idPractica))
                .Select(p => new { p.idPractica, p.cancelado, p.ensalida, p.hora_llegada })
                .ToListAsync();

            var byId = states.ToDictionary(x => x.idPractica, x => x);
            foreach (var row in list)
            {
                if (!byId.TryGetValue(row.idPractica, out var st))
                    continue;
                row.EstadoOperativo = ResolverEstadoOperativoPractica(st.cancelado, st.ensalida, st.hora_llegada);
                row.SigafiCancelado = (st.cancelado ?? 0) != 0 ? 1 : 0;
                row.SigafiEnsalida = (st.ensalida ?? 0) != 0 ? 1 : 0;
                row.SigafiHoraLlegada = st.hora_llegada;
            }
        }
*/

/*
        private async Task<List<ScheduledPracticeDto>> GetRecentSchedulesFromLocalMirrorAsync(int limit)
        {
            var take = Math.Clamp(limit, 1, 200);
            
            // Detectar si estamos en modo directo para saltar el filtro de cancelado si es necesario
            var mode = Environment.GetEnvironmentVariable("DATABASE_MODE") ?? "Mirror";
            bool isDirectMode = mode.Equals("Direct", StringComparison.OrdinalIgnoreCase);

            var query = from p in _context.Practicas.AsNoTracking()
                        join e in _context.Estudiantes on p.idalumno equals e.idAlumno
                        join v in _context.Vehiculos on p.idvehiculo equals v.idVehiculo
                        join i in _context.Instructores on p.idProfesor equals i.idProfesor
                        select new { p, e, v, i };

            if (!isDirectMode)
            {
                query = query.Where(r => (r.p.cancelado ?? 0) == 0);
            }

            var raw = await query.OrderByDescending(r => r.p.fecha)
                                .ThenByDescending(r => r.p.hora_salida)
                                .Take(take)
                                .ToListAsync();

            return raw.Select(r => new ScheduledPracticeDto
            {
                idPractica = r.p.idPractica,
                idalumno = r.p.idalumno,
                idvehiculo = r.p.idvehiculo,
                idProfesor = r.p.idProfesor,
                idPeriodo = r.p.idPeriodo,
                fecha = r.p.fecha,
                hora_salida = r.p.hora_salida,
                SigafiCancelado = (r.p.cancelado ?? 0) != 0 ? 1 : 0,
                SigafiEnsalida = (r.p.ensalida ?? 0) != 0 ? 1 : 0,
                SigafiHoraLlegada = r.p.hora_llegada,
                AlumnoNombre = $"{r.e.apellidoPaterno} {r.e.apellidoMaterno} {r.e.primerNombre} {r.e.segundoNombre}".Trim(),
                VehiculoDetalle = $"#{(r.v.numero_vehiculo ?? "?")} ({(r.v.placa ?? "")})",
                ProfesorNombre = $"{r.i.apellidos} {r.i.nombres}".Trim(),
                EstadoOperativo = ResolverEstadoOperativoPractica(r.p.cancelado, r.p.ensalida, r.p.hora_llegada),
                EsPlanificado = false
            }).ToList();
        }
*/
        public async Task<AgendaLogisticaResponseDto> GetTodayHistoryAsync(int limit = 50)
        {
            var list = (await _central.GetTodayCompletedPracticesAsync(limit)).ToList();

            return new AgendaLogisticaResponseDto
            {
                Practicas = list,
                FuenteDatos = "sigafi",
                ObtenidoEn = DateTime.UtcNow
            };
        }
    }
}
