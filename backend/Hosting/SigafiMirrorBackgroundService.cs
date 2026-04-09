using backend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace backend.Hosting;

/// <summary>
/// Mantiene el espejo local alineado con sigafi_es sin intervención manual.
/// </summary>
public sealed class SigafiMirrorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<SigafiMirrorSyncOptions> _options;
    private readonly ILogger<SigafiMirrorBackgroundService> _logger;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public SigafiMirrorBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<SigafiMirrorSyncOptions> options,
        ILogger<SigafiMirrorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.CurrentValue;
        if (!opts.Enabled)
        {
            _logger.LogInformation(
                "Espejo SIGAFI automático desactivado ({Section}:Enabled=false). Use POST /api/Sync/master para sincronizar a mano.",
                SigafiMirrorSyncOptions.SectionName);
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, opts.IntervalMinutes));

        if (opts.RunOnStartup)
        {
            var delay = TimeSpan.FromSeconds(Math.Max(0, opts.StartupDelaySeconds));
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            await RunSyncOnceAsync(stoppingToken).ConfigureAwait(false);
        }

        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunSyncOnceAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunSyncOnceAsync(CancellationToken ct)
    {
        if (!await _runLock.WaitAsync(0, ct).ConfigureAwait(false))
        {
            _logger.LogWarning("Ciclo de espejo SIGAFI omitido: el ciclo anterior sigue en ejecución.");
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sync = scope.ServiceProvider.GetRequiredService<IDataSyncService>();
            var log = await sync.MasterSyncAsync().ConfigureAwait(false);
            _logger.LogInformation(
                "Espejo SIGAFI: {Estado}. Registros procesados (suma por módulo): {Procesados}, fallos de módulo: {Fallidos}. {Mensaje}",
                log.Estado,
                log.RegistrosProcesados,
                log.RegistrosFallidos,
                log.Mensaje);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ciclo automático de espejo SIGAFI → local.");
        }
        finally
        {
            _runLock.Release();
        }
    }
}
