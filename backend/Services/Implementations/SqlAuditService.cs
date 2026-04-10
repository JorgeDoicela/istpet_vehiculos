using backend.Data;
using backend.Models;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class SqlAuditService : IAuditService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SqlAuditService> _logger;

        public SqlAuditService(IServiceScopeFactory scopeFactory, ILogger<SqlAuditService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task LogAsync(string usuario, string accion, string? entidadId = null, string? detalles = null, string? ipOrigen = null)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.AuditLogs.Add(new AuditLog
                {
                    usuario = usuario,
                    accion = accion,
                    entidad_id = entidadId,
                    detalles = detalles,
                    ip_origen = ipOrigen,
                    fecha_hora = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría [{Accion}] para usuario [{Usuario}].", accion, usuario);
            }
        }
    }
}
