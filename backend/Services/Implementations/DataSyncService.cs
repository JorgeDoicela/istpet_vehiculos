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
                        var existing = await _context.Instructores.FirstOrDefaultAsync(i => i.idProfesor == ci.idProfesor);

                        if (existing == null)
                        {
                            _context.Instructores.Add(new Instructor
                            {
                                idProfesor = ci.idProfesor,
                                primerNombre = ci.primerNombre ?? "",
                                segundoNombre = ci.segundoNombre,
                                primerApellido = ci.primerApellido ?? "",
                                segundoApellido = ci.segundoApellido,
                                nombres = (ci.nombres ?? "").ToUpper(),
                                apellidos = (ci.apellidos ?? "").ToUpper(),
                                celular = ci.celular?.Length > 50 ? ci.celular.Substring(0, 50) : ci.celular,
                                email = ci.email,
                                activo = ci.activo == 1
                            });
                            log.RegistrosProcesados++;
                        }
                        else
                        {
                            existing.primerNombre = ci.primerNombre ?? "";
                            existing.segundoNombre = ci.segundoNombre;
                            existing.primerApellido = ci.primerApellido ?? "";
                            existing.segundoApellido = ci.segundoApellido;
                            existing.nombres = (ci.nombres ?? "").ToUpper();
                            existing.apellidos = (ci.apellidos ?? "").ToUpper();
                            existing.celular = ci.celular?.Length > 50 ? ci.celular.Substring(0, 50) : ci.celular;
                            existing.email = ci.email;
                            existing.activo = ci.activo == 1;
                            log.RegistrosProcesados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.RegistrosFallidos++;
                        _logger.LogError("Error sincronizando instructor {cedula}: {message}", ci.idProfesor, ex.Message);
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

            foreach (var item in externalData)
            {
                try
                {
                    string extCedula = item.GetProperty("id_externo").GetString() ?? "";
                    string extNombre = item.GetProperty("nombre_completo").GetString() ?? "S/N";
                    string extEmail = item.GetProperty("correo_universidad").GetString() ?? "";

                    if (!DataValidator.IsValidCedula(extCedula))
                    {
                        log.RegistrosFallidos++;
                        continue;
                    }

                    var names = extNombre.Split(' ', 2);
                    string firstName = DataValidator.CleanName(names[0]);
                    string lastName = names.Length > 1 ? DataValidator.CleanName(names[1]) : "EXTERNO";

                    var existing = await _context.Estudiantes.FindAsync(extCedula);
                    if (existing == null)
                    {
                        _context.Estudiantes.Add(new Estudiante
                        {
                            idAlumno = extCedula,
                            primerNombre = firstName,
                            apellidoPaterno = lastName,
                            email = DataValidator.IsValidEmail(extEmail) ? extEmail : null,
                            activo = true
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
                        var existing = await _context.Usuarios.FirstOrDefaultAsync(u => u.usuario_login == eu.usuario);
                        
                        string role = "guardia";
                        if (eu.usuario.ToLower().Contains("admin")) role = "admin";
                        else if (eu.esRrhh == 1) role = "estacionable";
                        else if (eu.salida == 1 || eu.ingreso == 1) role = "guardia";

                        if (existing == null)
                        {
                            _context.Usuarios.Add(new Usuario
                            {
                                usuario_login = eu.usuario,
                                password_hash = eu.password,
                                rol = role,
                                nombre_completo = eu.usuario.ToUpper(),
                                activo = eu.activo == 1,
                                creado_en = DateTime.Now
                            });
                        }
                        else
                        {
                            existing.activo = eu.activo == 1;
                            existing.rol = role;
                            if (!existing.password_hash.StartsWith("$2a$") && !existing.password_hash.StartsWith("$2b$"))
                            {
                                existing.password_hash = eu.password;
                            }
                        }
                        log.RegistrosProcesados++;
                    }
                    catch (Exception ex)
                    {
                        log.RegistrosFallidos++;
                        _logger.LogError("Error sincronizando usuario {user}: {message}", eu.usuario, ex.Message);
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
