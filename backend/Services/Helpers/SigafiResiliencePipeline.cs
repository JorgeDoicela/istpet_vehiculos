using Polly;
using Polly.CircuitBreaker;

namespace backend.Services.Helpers
{
    /// <summary>
    /// Circuit breaker singleton para todas las llamadas a la BD remota SIGAFI.
    ///
    /// Comportamiento:
    ///   - Closed   : opera normalmente.
    ///   - Open     : falló ≥50 % de las últimas 3 llamadas en 10 s → rechaza
    ///                rápidamente durante 30 s sin intentar abrir conexión.
    ///   - HalfOpen : tras 30 s prueba UNA llamada; si tiene éxito vuelve a Closed.
    ///
    /// Si el circuito está abierto, los callers reciben BrokenCircuitException
    /// que los try/catch existentes en SqlCentralStudentProvider convierten en
    /// respuesta null/vacía, igual que cualquier otro error de red.
    /// </summary>
    public interface ISigafiResiliencePipeline
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
        Task ExecuteAsync(Func<Task> operation);
    }

    public sealed class SigafiResiliencePipeline : ISigafiResiliencePipeline
    {
        private readonly ResiliencePipeline _pipeline;

        public SigafiResiliencePipeline(ILogger<SigafiResiliencePipeline> logger)
        {
            _pipeline = new ResiliencePipelineBuilder()
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(10),
                    MinimumThroughput = 3,
                    BreakDuration = TimeSpan.FromSeconds(30),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<MySqlConnector.MySqlException>()
                        .Handle<TimeoutException>()
                        .Handle<InvalidOperationException>(),
                    OnOpened = args =>
                    {
                        logger.LogWarning(
                            "⚡ Circuito SIGAFI ABIERTO por {BreakDuration}. " +
                            "Las consultas a SIGAFI fallarán rápidamente hasta que el circuito se cierre.",
                            args.BreakDuration);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("✅ Circuito SIGAFI CERRADO. Conexión a BD central restaurada.");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = _ =>
                    {
                        logger.LogInformation("🔍 Circuito SIGAFI SEMI-ABIERTO. Probando conexión...");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
            => await _pipeline.ExecuteAsync(async _ => await operation(), CancellationToken.None);

        public async Task ExecuteAsync(Func<Task> operation)
            => await _pipeline.ExecuteAsync(async _ => await operation(), CancellationToken.None);
    }
}
