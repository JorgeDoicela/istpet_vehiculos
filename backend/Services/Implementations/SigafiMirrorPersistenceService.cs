using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations;

public class SigafiMirrorPersistenceService : ISigafiMirrorPersistenceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SigafiMirrorPersistenceService> _logger;

    public SigafiMirrorPersistenceService(AppDbContext context, ILogger<SigafiMirrorPersistenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PersistEstudiantesFromLitesAsync(IEnumerable<CentralAlumnoLiteDto> rows, CancellationToken ct = default)
    {
        try
        {
            var list = rows?
                .Where(x => !string.IsNullOrWhiteSpace(x.idAlumno))
                .GroupBy(x => x.idAlumno.Trim())
                .Select(g => g.First())
                .ToList();
            if (list == null || list.Count == 0)
                return;

            foreach (var item in list)
            {
                var id = item.idAlumno.Trim();
                var existing = await _context.Estudiantes.FindAsync(new object[] { id }, ct);
                if (existing == null)
                {
                    _context.Estudiantes.Add(new Estudiante
                    {
                        idAlumno = id,
                        primerNombre = (item.primerNombre ?? "").ToUpper(),
                        segundoNombre = item.segundoNombre?.ToUpper(),
                        apellidoPaterno = (item.apellidoPaterno ?? "").ToUpper(),
                        apellidoMaterno = item.apellidoMaterno?.ToUpper(),
                        celular = item.celular?.Length > 50 ? item.celular[..50] : item.celular,
                        email = item.email?.Length > 100 ? item.email[..100] : item.email,
                        idPeriodo = item.idPeriodo,
                        idNivel = item.idNivel,
                        idSeccion = item.idSeccion,
                        idModalidad = item.idModalidad
                    });
                }
                else
                {
                    existing.primerNombre = (item.primerNombre ?? existing.primerNombre).ToUpper();
                    existing.segundoNombre = item.segundoNombre?.ToUpper();
                    existing.apellidoPaterno = (item.apellidoPaterno ?? existing.apellidoPaterno).ToUpper();
                    existing.apellidoMaterno = item.apellidoMaterno?.ToUpper();
                    existing.celular = item.celular?.Length > 50 ? item.celular[..50] : item.celular;
                    existing.email = item.email?.Length > 100 ? item.email[..100] : existing.email;
                    if (!string.IsNullOrEmpty(item.idPeriodo))
                        existing.idPeriodo = item.idPeriodo;
                    existing.idNivel = item.idNivel;
                    existing.idSeccion = item.idSeccion ?? existing.idSeccion;
                    existing.idModalidad = item.idModalidad ?? existing.idModalidad;
                }
            }

            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo persistir alumnos (lite) desde SIGAFI al espejo local.");
        }
    }

    /// <inheritdoc />
    public async Task PersistPracticesFromScheduleDtosAsync(IEnumerable<ScheduledPracticeDto> rows, CancellationToken ct = default)
    {
        try
        {
            var list = rows?
                .Where(r => r.idPractica > 0)
                .GroupBy(r => r.idPractica)
                .Select(g => g.First())
                .ToList();
            if (list == null || list.Count == 0)
                return;

            var alumnoSet = (await _context.Estudiantes.AsNoTracking().Select(e => e.idAlumno).ToListAsync(ct)).ToHashSet();
            var vehSet = (await _context.Vehiculos.AsNoTracking().Select(v => v.idVehiculo).ToListAsync(ct)).ToHashSet();
            var profSet = (await _context.Instructores.AsNoTracking().Select(i => i.idProfesor).ToListAsync(ct)).ToHashSet();

            foreach (var d in list)
            {
                if (string.IsNullOrWhiteSpace(d.idalumno) || string.IsNullOrWhiteSpace(d.idProfesor) || d.idvehiculo <= 0)
                    continue;
                if (!alumnoSet.Contains(d.idalumno) || !vehSet.Contains(d.idvehiculo) || !profSet.Contains(d.idProfesor))
                    continue;

                var idPeriodo = string.IsNullOrWhiteSpace(d.idPeriodo) ? "SIN_MAT" : d.idPeriodo.Trim();
                if (idPeriodo.Length > 7)
                    idPeriodo = idPeriodo[..7];

                var existing = await _context.Practicas.FindAsync(new object[] { d.idPractica }, ct);
                if (existing == null)
                {
                    _context.Practicas.Add(new Practica
                    {
                        idPractica = d.idPractica,
                        idalumno = d.idalumno,
                        idvehiculo = d.idvehiculo,
                        idProfesor = d.idProfesor,
                        idPeriodo = idPeriodo,
                        fecha = d.fecha,
                        hora_salida = d.hora_salida,
                        hora_llegada = d.SigafiHoraLlegada,
                        ensalida = (byte?)(d.SigafiEnsalida != 0 ? 1 : 0),
                        cancelado = (byte?)(d.SigafiCancelado != 0 ? 1 : 0),
                        verificada = 0
                    });
                }
                else
                {
                    existing.idalumno = d.idalumno;
                    existing.idvehiculo = d.idvehiculo;
                    existing.idProfesor = d.idProfesor;
                    existing.idPeriodo = idPeriodo;
                    existing.fecha = d.fecha;
                    existing.hora_salida = d.hora_salida;
                    existing.hora_llegada = d.SigafiHoraLlegada;
                    existing.ensalida = (byte?)(d.SigafiEnsalida != 0 ? 1 : 0);
                    existing.cancelado = (byte?)(d.SigafiCancelado != 0 ? 1 : 0);
                }
            }

            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo persistir prácticas (agenda) desde SIGAFI al espejo local.");
        }
    }
}
