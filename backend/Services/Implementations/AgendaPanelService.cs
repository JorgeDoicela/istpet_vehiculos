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

        public AgendaPanelService(AppDbContext context, ICentralStudentProvider central)
        {
            _context = context;
            _central = central;
        }

        public async Task<AgendaLogisticaResponseDto> GetAgendaAsync(int limit = 100)
        {
            var take = Math.Clamp(limit, 1, 200);
            var fromSigafi = true;
            var list = (await _central.GetRecentSchedulesAsync(take)).ToList();
            if (list.Count == 0)
            {
                list = await GetRecentSchedulesFromLocalMirrorAsync(take);
                fromSigafi = false;
            }

            await EnrichAgendaEstadoOperativoAsync(list);

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
                if (byId.TryGetValue(row.idPractica, out var st))
                    row.EstadoOperativo = ResolverEstadoOperativoPractica(st.cancelado, st.ensalida, st.hora_llegada);
            }
        }

        private async Task<List<ScheduledPracticeDto>> GetRecentSchedulesFromLocalMirrorAsync(int limit)
        {
            var take = Math.Clamp(limit, 1, 200);
            var raw = await (
                from p in _context.Practicas.AsNoTracking()
                join e in _context.Estudiantes on p.idalumno equals e.idAlumno
                join v in _context.Vehiculos on p.idvehiculo equals v.idVehiculo
                join i in _context.Instructores on p.idProfesor equals i.idProfesor
                where (p.cancelado ?? 0) == 0
                orderby p.fecha descending, p.hora_salida descending
                select new { p, e, v, i }).Take(take).ToListAsync();

            return raw.Select(r => new ScheduledPracticeDto
            {
                idPractica = r.p.idPractica,
                idalumno = r.p.idalumno,
                idvehiculo = r.p.idvehiculo,
                idProfesor = r.p.idProfesor,
                fecha = r.p.fecha,
                hora_salida = r.p.hora_salida,
                AlumnoNombre = $"{r.e.apellidoPaterno} {r.e.apellidoMaterno} {r.e.primerNombre} {r.e.segundoNombre}".Trim(),
                VehiculoDetalle = $"#{(r.v.numero_vehiculo ?? "?")} ({(r.v.placa ?? "")})",
                ProfesorNombre = $"{r.i.apellidos} {r.i.nombres}".Trim(),
                EstadoOperativo = ResolverEstadoOperativoPractica(r.p.cancelado, r.p.ensalida, r.p.hora_llegada)
            }).ToList();
        }
    }
}
