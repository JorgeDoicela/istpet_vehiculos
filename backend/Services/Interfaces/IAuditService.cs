namespace backend.Services.Interfaces
{
    /// <summary>
    /// Persiste eventos de auditoría de forma asíncrona y no bloqueante.
    /// Nunca lanza excepciones al caller; los fallos de escritura se loguean internamente.
    /// </summary>
    public interface IAuditService
    {
        Task LogAsync(string usuario, string accion, string? entidadId = null, string? detalles = null, string? ipOrigen = null);
    }
}
