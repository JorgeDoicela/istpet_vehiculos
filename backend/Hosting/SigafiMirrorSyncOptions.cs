namespace backend.Hosting;

/// <summary>
/// Sincronización automática SIGAFI (fuente) → BD local istpet_vehiculos (espejo).
/// </summary>
public class SigafiMirrorSyncOptions
{
    public const string SectionName = "SigafiMirrorSync";

    /// <summary>Si false, no se programa ningún ciclo (solo sync manual vía API).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Minutos entre ejecuciones completas (mínimo 1).</summary>
    public int IntervalMinutes { get; set; } = 30;

    /// <summary>Ejecutar un ciclo al arrancar la API (tras el retraso inicial).</summary>
    public bool RunOnStartup { get; set; } = true;

    /// <summary>Espera tras el arranque antes del primer sync (dar tiempo a MySQL/red).</summary>
    public int StartupDelaySeconds { get; set; } = 20;
}
