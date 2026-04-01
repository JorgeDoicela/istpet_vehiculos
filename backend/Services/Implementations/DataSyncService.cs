using backend.Models;
using backend.Data;
using backend.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace backend.Services.Interfaces
{
    public interface IDataSyncService
    {
        Task<SyncLog> SyncExternalStudentsAsync(List<JsonElement> externalData);
    }
}

namespace backend.Services.Implementations
{
    using backend.Services.Interfaces;

    public class DataSyncService : IDataSyncService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataSyncService> _logger;

        public DataSyncService(AppDbContext context, ILogger<DataSyncService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /**
         * Dynamic Sync with Data-Shield Protection
         * Processes uncertain external JSON and maps it safely to ISTPET schema.
         */
        public async Task<SyncLog> SyncExternalStudentsAsync(List<JsonElement> externalData)
        {
            _logger.LogInformation("Iniciando Ingesta Protegida de Datos...");
            
            var log = new SyncLog 
            { 
                Modulo = "Estudiantes", 
                Origen = "DYNAMIC_JSON_API",
                RegistrosProcesados = 0,
                RegistrosFallidos = 0
            };

            // Configuración de Mapeo (Esto podría venir de un archivo config o DB)
            var mapping = new List<SyncMapping>
            {
                new SyncMapping { SourceField = "id_externo", DestinationField = "Cedula" },
                new SyncMapping { SourceField = "nombre_completo", DestinationField = "Nombres" },
                // El campo Apellidos lo extraeremos del nombre si es necesario
                new SyncMapping { SourceField = "correo_universidad", DestinationField = "Email" }
            };

            foreach (var item in externalData)
            {
                try
                {
                    // 1. Extraer datos usando el mapeo ajustable
                    string extCedula = item.GetProperty("id_externo").GetString() ?? "";
                    string extNombre = item.GetProperty("nombre_completo").GetString() ?? "S/N";
                    string extEmail = item.GetProperty("correo_universidad").GetString() ?? "";

                    // 2. PASAR POR LA ADUANA (Validación)
                    if (!DataValidator.IsValidCedula(extCedula))
                    {
                        log.RegistrosFallidos++;
                        _logger.LogWarning("Registro rechazado por Aduana: Cédula inválida {extCedula}", extCedula);
                        continue;
                    }

                    // 3. Transformación Segura
                    var names = extNombre.Split(' ', 2);
                    string firstName = DataValidator.CleanName(names[0]);
                    string lastName = names.Length > 1 ? DataValidator.CleanName(names[1]) : "EXTERNO";

                    // 4. Verificación de Existencia y Persistencia
                    var existing = await _context.Estudiantes.FindAsync(extCedula);
                    if (existing == null)
                    {
                        _context.Estudiantes.Add(new Estudiante
                        {
                            Cedula = extCedula,
                            Nombres = firstName,
                            Apellidos = lastName,
                            Email = DataValidator.IsValidEmail(extEmail) ? extEmail : null,
                            Activo = true
                        });
                        log.RegistrosProcesados++;
                    }
                }
                catch (Exception ex)
                {
                    log.RegistrosFallidos++;
                    _logger.LogError("Fallo crítico en registro individual: {message}", ex.Message);
                }
            }

            await _context.SaveChangesAsync();
            
            log.Estado = log.RegistrosFallidos > 0 ? "ADVERTENCIA" : "OK";
            log.Mensaje = $"Proceso finalizado. Éxito: {log.RegistrosProcesados}, Fallidos: {log.RegistrosFallidos}";
            
            _context.SyncLogs.Add(log);
            await _context.SaveChangesAsync();

            return log;
        }
    }
}
