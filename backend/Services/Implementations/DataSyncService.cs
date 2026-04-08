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
        Task<SyncLog> SyncInstructorsAsync();
        Task<SyncLog> SyncWebUsersAsync();
    }
}

namespace backend.Services.Implementations
{
    using backend.Services.Interfaces;

    public class DataSyncService : IDataSyncService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataSyncService> _logger;
        private readonly ICentralStudentProvider _centralProvider;

        public DataSyncService(AppDbContext context, ILogger<DataSyncService> logger, ICentralStudentProvider centralProvider)
        {
            _context = context;
            _logger = logger;
            _centralProvider = centralProvider;
        }

        public async Task<SyncLog> SyncInstructorsAsync()
        {
            _logger.LogInformation("Iniciando Sincronización Automática de Instructores (SIGAFI)...");

            var log = new SyncLog
            {
                Modulo = "Instructores",
                Origen = "SIGAFI_SQL_BRIDGE",
                Fecha = DateTime.Now,
                RegistrosProcesados = 0,
                RegistrosFallidos = 0
            };

            try
            {
                var centralInstructors = await _centralProvider.GetAllInstructorsFromCentralAsync();

                foreach (var ci in centralInstructors)
                {
                    try
                    {
                        var existing = await _context.Instructores.FirstOrDefaultAsync(i => i.Cedula == ci.Cedula);

                        if (existing == null)
                        {
                            _context.Instructores.Add(new Instructor
                            {
                                Cedula = ci.Cedula,
                                Nombres = ci.Nombres.ToUpper(),
                                Apellidos = ci.Apellidos.ToUpper(),
                                Telefono = ci.Telefono?.Length > 50 ? ci.Telefono.Substring(0, 50) : ci.Telefono,
                                Email = ci.Email,
                                Activo = ci.Activo
                            });
                            log.RegistrosProcesados++;
                        }
                        else
                        {
                            // Actualizamos datos existentes para mantener consistencia
                            existing.Nombres = ci.Nombres.ToUpper();
                            existing.Apellidos = ci.Apellidos.ToUpper();
                            existing.Telefono = ci.Telefono?.Length > 50 ? ci.Telefono.Substring(0, 50) : ci.Telefono;
                            existing.Email = ci.Email;
                            existing.Activo = ci.Activo;
                            log.RegistrosProcesados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.RegistrosFallidos++;
                        _logger.LogError("Error sincronizando instructor {cedula}: {message}", ci.Cedula, ex.Message);
                    }
                }

                await _context.SaveChangesAsync();
                log.Estado = "OK";
                log.Mensaje = $"Sincronización de instructores completada. Procesados: {log.RegistrosProcesados}";
            }
            catch (Exception ex)
            {
                log.Estado = "ERROR";
                log.Mensaje = $"Fallo crítico en sincronización: {ex.Message}";
                _logger.LogError(ex, "Fallo crítico en sincronización de instructores.");
            }

            return log;
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

            return log;
        }

        public async Task<SyncLog> SyncWebUsersAsync()
        {
            _logger.LogInformation("Iniciando Sincronización de Usuarios SIGAFI (usuarios_web)...");

            var log = new SyncLog
            {
                Modulo = "Usuarios",
                Origen = "SIGAFI_WEB_USERS",
                Fecha = DateTime.Now,
                RegistrosProcesados = 0,
                RegistrosFallidos = 0
            };

            try
            {
                var externalUsers = await _centralProvider.GetAllWebUsersAsync();

                foreach (var eu in externalUsers)
                {
                    try
                    {
                        var existing = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioLogin == eu.Usuario);
                        
                        // Rol según bits SIGAFI (enum real: admin, guardia, estacionable)
                        string role = "guardia";
                        if (eu.Usuario.ToLower().Contains("admin")) role = "admin";
                        else if (eu.EsRrhh == 1) role = "estacionable";
                        else if (eu.Salida == 1 || eu.Ingreso == 1) role = "guardia";

                        if (existing == null)
                        {
                            _context.Usuarios.Add(new Usuario
                            {
                                UsuarioLogin = eu.Usuario,
                                PasswordHash = eu.Password, // Se guardará tal cual, AuthController manejará el migrate-on-login
                                Rol = role,
                                NombreCompleto = eu.Usuario.ToUpper(),
                                Activo = eu.Activo == 1,
                                CreadoEn = DateTime.Now
                            });
                        }
                        else
                        {
                            existing.Activo = eu.Activo == 1;
                            existing.Rol = role;
                            // No sobreescribimos el password local si ya ha sido hasheado por nuestro sistema
                            if (!existing.PasswordHash.StartsWith("$2a$") && !existing.PasswordHash.StartsWith("$2b$"))
                            {
                                existing.PasswordHash = eu.Password;
                            }
                        }
                        log.RegistrosProcesados++;
                    }
                    catch (Exception ex)
                    {
                        log.RegistrosFallidos++;
                        _logger.LogError("Error sincronizando usuario {user}: {message}", eu.Usuario, ex.Message);
                    }
                }

                await _context.SaveChangesAsync();
                log.Estado = "OK";
                log.Mensaje = $"Sincronización de usuarios completada. Procesados: {log.RegistrosProcesados}";
            }
            catch (Exception ex)
            {
                log.Estado = "ERROR";
                log.Mensaje = $"Fallo crítico en sincronización de usuarios: {ex.Message}";
            }

            return log;
        }
    }
}
