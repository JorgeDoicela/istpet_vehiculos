using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace backend.Services.Interfaces;

/// <summary>
/// Persiste en <c>istpet_vehiculos</c> los datos obtenidos de SIGAFI durante el uso normal
/// (además del Master Sync), para que el sistema siga operando si SIGAFI no está disponible.
/// </summary>
public interface ISigafiMirrorPersistenceService
{
    Task PersistEstudiantesFromLitesAsync(IEnumerable<CentralAlumnoLiteDto> rows, CancellationToken ct = default);

    Task PersistPracticesFromScheduleDtosAsync(IEnumerable<ScheduledPracticeDto> rows, CancellationToken ct = default);
}
