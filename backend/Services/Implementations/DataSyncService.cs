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
        Task<SyncLog> MasterSyncAsync();
        Task<bool> PingSigafiAsync();
    }
}

namespace backend.Services.Implementations
{
    using backend.Services.Interfaces;

    /**
     * Sincronización masiva: SIEMPRE lee desde SIGAFI (ICentralStudentProvider / SigafiConnection)
     * y escribe en la BD local (DefaultConnection). SIGAFI es la fuente; istpet_vehiculos es copia de trabajo.
     */
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

        public async Task<SyncLog> MasterSyncAsync()
        {
            _logger.LogInformation("Iniciando Master Sync SIGAFI integral...");

            // Invalida el caché antes de leer para que todos los módulos
            // obtengan datos frescos de SIGAFI, no entradas expiradas.
            _centralProvider.InvalidateSigafiCatalogCache();

            var log = new SyncLog
            {
                Modulo = "MasterSyncSIGAFI",
                Origen = "SIGAFI_SQL_BRIDGE",
                Fecha = DateTime.Now,
                RegistrosProcesados = 0,
                RegistrosFallidos = 0
            };

            var warnings = new List<string>();

            log.RegistrosProcesados += await ExecuteSyncStepAsync("tipo_licencia", SyncLicenseTypesAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("cursos", SyncCoursesAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("categoria_vehiculos", SyncVehicleCategoriesAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("categorias_examenes_conduccion", SyncExamCategoriesAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("profesores", SyncInstructorsInternalAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("usuarios_web", SyncWebUsersInternalAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("alumnos", SyncStudentsFromSigafiAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("matriculas", SyncEnrollmentsAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("matriculas_examen_conduccion", SyncMatriculaExamLinksAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("vehiculos", SyncVehiclesAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("cond_alumnos_practicas", SyncPracticesFromSigafiAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("asignacion_instructores_vehiculos", SyncInstructorVehicleAssignmentsAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("cond_alumnos_vehiculos", SyncStudentVehicleAssignmentsAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("cond_alumnos_horarios", SyncSchedulesAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("cond_practicas_horarios_alumnos", SyncPracticeScheduleLinksAsync, warnings, log);

            if (warnings.Count == 0)
            {
                log.Estado = "OK";
                log.Mensaje = "Master Sync SIGAFI completado correctamente.";
            }
            else
            {
                log.Estado = "ADVERTENCIA";
                log.Mensaje = $"Master Sync parcial. Módulos omitidos: {string.Join("; ", warnings)}";
            }

            return log;
        }

        public Task<bool> PingSigafiAsync()
        {
            return _centralProvider.PingSigafiAsync();
        }

        private async Task<int> ExecuteSyncStepAsync(string moduleName, Func<Task<int>> syncAction, List<string> warnings, SyncLog log)
        {
            try
            {
                var count = await syncAction();
                _logger.LogInformation("MasterSync módulo {module} OK: {count} registros", moduleName, count);
                return count;
            }
            catch (Exception ex)
            {
                log.RegistrosFallidos++;
                var detail = ex.InnerException != null
                    ? $"{ex.Message} | {ex.InnerException.Message}"
                    : ex.Message;
                warnings.Add($"{moduleName}: {detail}");
                _logger.LogWarning(ex, "MasterSync omitió módulo {module} por error.", moduleName);
                return 0;
            }
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
                        var existing = await _context.Usuarios.FirstOrDefaultAsync(u => u.usuario == eu.usuario);
                        
                        string role = "guardia";
                        if (eu.usuario.ToLower().Contains("admin")) role = "admin";
                        else if (eu.esRrhh == 1) role = "estacionable";
                        else if (eu.salida == 1 || eu.ingreso == 1) role = "guardia";

                        if (existing == null)
                        {
                            _context.Usuarios.Add(new Usuario
                            {
                                usuario = eu.usuario,
                                password = eu.password,
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
                            if (!existing.password.StartsWith("$2a$") && !existing.password.StartsWith("$2b$"))
                            {
                                existing.password = eu.password;
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

        private async Task<int> SyncLicenseTypesAsync()
        {
            var rows = await _centralProvider.GetAllLicenseTypesFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                if (!item.id_categoria_sigafi.HasValue)
                    continue;

                var catId = item.id_categoria_sigafi.Value;
                var codigo = string.IsNullOrWhiteSpace(item.codigo) ? $"V{catId}" : item.codigo.Trim().ToUpperInvariant();
                if (codigo.Length > 10)
                    codigo = codigo[..10];
                var descripcion = string.IsNullOrWhiteSpace(item.descripcion) ? codigo : item.descripcion.Trim();
                if (descripcion.Length > 200)
                    descripcion = descripcion[..200];
                var existing = await _context.TipoLicencias.FirstOrDefaultAsync(x => x.id_categoria_sigafi == catId)
                    ?? await _context.TipoLicencias.FirstOrDefaultAsync(x => x.codigo == codigo);

                if (existing == null)
                {
                    _context.TipoLicencias.Add(new TipoLicencia
                    {
                        id_categoria_sigafi = catId,
                        codigo = codigo,
                        descripcion = descripcion,
                        activo = item.activo == 1
                    });
                }
                else
                {
                    existing.id_categoria_sigafi = catId;
                    existing.codigo = codigo;
                    existing.descripcion = descripcion;
                    existing.activo = item.activo == 1;
                }

                processed++;
            }

            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncCoursesAsync()
        {
            var rows = await _centralProvider.GetAllCoursesFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Cursos.FirstOrDefaultAsync(x => x.idNivel == item.idNivel);
                if (existing == null)
                {
                    _context.Cursos.Add(new Curso
                    {
                        idNivel = item.idNivel,
                        idCarrera = item.idCarrera,
                        Nivel = item.Nivel?.Length > 20 ? item.Nivel[..20] : item.Nivel,
                        jerarquia = item.jerarquia,
                        orden = item.orden,
                        esRecuperacion = item.esRecuperacion.HasValue ? (byte?)item.esRecuperacion.Value : null,
                        aliasCurso = item.aliasCurso?.Length > 10 ? item.aliasCurso[..10] : item.aliasCurso
                    });
                }
                else
                {
                    existing.idCarrera = item.idCarrera;
                    existing.Nivel = item.Nivel?.Length > 20 ? item.Nivel[..20] : item.Nivel;
                    existing.jerarquia = item.jerarquia;
                    existing.orden = item.orden;
                    existing.esRecuperacion = item.esRecuperacion.HasValue ? (byte?)item.esRecuperacion.Value : null;
                    existing.aliasCurso = item.aliasCurso?.Length > 10 ? item.aliasCurso[..10] : item.aliasCurso;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncVehicleCategoriesAsync()
        {
            var rows = await _centralProvider.GetAllVehicleCategoriesFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.CategoriasVehiculos.FirstOrDefaultAsync(x => x.idCategoria == item.idCategoria);
                if (existing == null)
                {
                    _context.CategoriasVehiculos.Add(new CategoriaVehiculo
                    {
                        idCategoria = item.idCategoria,
                        categoria = item.categoria?.Length > 100 ? item.categoria[..100] : (item.categoria ?? string.Empty)
                    });
                }
                else
                {
                    existing.categoria = item.categoria?.Length > 100 ? item.categoria[..100] : (item.categoria ?? string.Empty);
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncExamCategoriesAsync()
        {
            var rows = await _centralProvider.GetAllExamCategoriesFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.CategoriasExamenes.FirstOrDefaultAsync(x => x.IdCategoria == item.IdCategoria);
                if (existing == null)
                {
                    _context.CategoriasExamenes.Add(new CategoriaExamenConduccion
                    {
                        IdCategoria = item.IdCategoria,
                        categoria = item.categoria?.Length > 100 ? item.categoria[..100] : (item.categoria ?? string.Empty),
                        tieneNota = item.tieneNota == 1,
                        activa = item.activa == 1
                    });
                }
                else
                {
                    existing.categoria = item.categoria?.Length > 100 ? item.categoria[..100] : (item.categoria ?? string.Empty);
                    existing.tieneNota = item.tieneNota == 1;
                    existing.activa = item.activa == 1;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncInstructorsInternalAsync()
        {
            var rows = await _centralProvider.GetAllInstructorsFromCentralAsync();
            var byId = await _context.Instructores.ToDictionaryAsync(i => i.idProfesor);
            var processed = 0;
            foreach (var ci in rows)
            {
                if (!byId.TryGetValue(ci.idProfesor, out var existing))
                {
                    var nuevo = new Instructor
                    {
                        idProfesor = ci.idProfesor,
                        primerNombre = ci.primerNombre ?? "",
                        segundoNombre = ci.segundoNombre,
                        primerApellido = ci.primerApellido ?? "",
                        segundoApellido = ci.segundoApellido,
                        nombres = (ci.nombres ?? "").ToUpper(),
                        apellidos = (ci.apellidos ?? "").ToUpper(),
                        celular = ci.celular?.Length > 50 ? ci.celular[..50] : ci.celular,
                        email = ci.email,
                        activo = ci.activo == 1
                    };
                    _context.Instructores.Add(nuevo);
                    byId[ci.idProfesor] = nuevo;
                }
                else
                {
                    existing.primerNombre = ci.primerNombre ?? "";
                    existing.segundoNombre = ci.segundoNombre;
                    existing.primerApellido = ci.primerApellido ?? "";
                    existing.segundoApellido = ci.segundoApellido;
                    existing.nombres = (ci.nombres ?? "").ToUpper();
                    existing.apellidos = (ci.apellidos ?? "").ToUpper();
                    existing.celular = ci.celular?.Length > 50 ? ci.celular[..50] : ci.celular;
                    existing.email = ci.email;
                    existing.activo = ci.activo == 1;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncWebUsersInternalAsync()
        {
            var rows = await _centralProvider.GetAllWebUsersAsync();
            var byUsuario = await _context.Usuarios.ToDictionaryAsync(u => u.usuario);
            var processed = 0;
            foreach (var eu in rows)
            {
                if (!byUsuario.TryGetValue(eu.usuario, out var existing))
                {
                    var nuevo = new Usuario
                    {
                        usuario = eu.usuario,
                        password = eu.password,
                        salida = eu.salida == 1,
                        ingreso = eu.ingreso == 1,
                        activo = eu.activo == 1,
                        asistencia = eu.asistencia == 1,
                        esRrhh = eu.esRrhh == 1
                    };
                    _context.Usuarios.Add(nuevo);
                    byUsuario[eu.usuario] = nuevo;
                }
                else
                {
                    existing.salida = eu.salida == 1;
                    existing.ingreso = eu.ingreso == 1;
                    existing.activo = eu.activo == 1;
                    existing.asistencia = eu.asistencia == 1;
                    existing.esRrhh = eu.esRrhh == 1;
                    if (!existing.password.StartsWith("$2a$") && !existing.password.StartsWith("$2b$"))
                        existing.password = eu.password;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncStudentsFromSigafiAsync()
        {
            var rows = await _centralProvider.GetAllStudentsFromCentralAsync();
            // Una sola carga: evita N consultas SELECT por idAlumno durante el espejo automático / Master Sync.
            var byId = await _context.Estudiantes.ToDictionaryAsync(e => e.idAlumno);
            var processed = 0;
            foreach (var item in rows)
            {
                if (!byId.TryGetValue(item.idAlumno, out var existing))
                {
                    var nuevo = new Estudiante
                    {
                        idAlumno = item.idAlumno,
                        primerNombre = item.primerNombre ?? "",
                        segundoNombre = item.segundoNombre,
                        apellidoPaterno = item.apellidoPaterno ?? "",
                        apellidoMaterno = item.apellidoMaterno,
                        celular = item.celular?.Length > 50 ? item.celular[..50] : item.celular,
                        email = item.email?.Length > 100 ? item.email[..100] : item.email,
                        idPeriodo = item.idPeriodo,
                        idNivel = item.idNivel,
                        idSeccion = item.idSeccion,
                        idModalidad = item.idModalidad,
                        activo = true
                    };
                    _context.Estudiantes.Add(nuevo);
                    byId[item.idAlumno] = nuevo;
                }
                else
                {
                    existing.primerNombre = item.primerNombre ?? existing.primerNombre;
                    existing.segundoNombre = item.segundoNombre;
                    existing.apellidoPaterno = item.apellidoPaterno ?? existing.apellidoPaterno;
                    existing.apellidoMaterno = item.apellidoMaterno;
                    existing.celular = item.celular?.Length > 50 ? item.celular[..50] : item.celular;
                    existing.email = item.email?.Length > 100 ? item.email[..100] : item.email;
                    existing.idPeriodo = item.idPeriodo ?? existing.idPeriodo;
                    existing.idNivel = item.idNivel ?? existing.idNivel;
                    existing.idSeccion = item.idSeccion ?? existing.idSeccion;
                    existing.idModalidad = item.idModalidad ?? existing.idModalidad;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncEnrollmentsAsync()
        {
            var rows = await _centralProvider.GetActiveEnrollmentsFromCentralAsync();
            var byId = await _context.Matriculas.ToDictionaryAsync(m => m.idMatricula);
            var processed = 0;
            foreach (var item in rows)
            {
                if (!byId.TryGetValue(item.idMatricula, out var existing))
                {
                    var nuevo = new Matricula
                    {
                        idMatricula = item.idMatricula,
                        idAlumno = item.idAlumno,
                        idNivel = item.idNivel,
                        idSeccion = item.idSeccion,
                        idModalidad = item.idModalidad,
                        idPeriodo = item.idPeriodo,
                        fechaMatricula = item.fechaMatricula,
                        paralelo = item.paralelo,
                        arrastres = item.arrastres == 1,
                        folio = item.folio,
                        beca_matricula = item.beca_matricula,
                        retirado = item.retirado == 1,
                        esOyente = item.esOyente == 1,
                        valida = item.valida,
                        estado = "ACTIVO"
                    };
                    _context.Matriculas.Add(nuevo);
                    byId[item.idMatricula] = nuevo;
                }
                else
                {
                    existing.idAlumno = item.idAlumno;
                    existing.idNivel = item.idNivel;
                    existing.idSeccion = item.idSeccion;
                    existing.idModalidad = item.idModalidad;
                    existing.idPeriodo = item.idPeriodo;
                    existing.fechaMatricula = item.fechaMatricula;
                    existing.paralelo = item.paralelo;
                    existing.arrastres = item.arrastres == 1;
                    existing.folio = item.folio;
                    existing.beca_matricula = item.beca_matricula;
                    existing.retirado = item.retirado == 1;
                    existing.esOyente = item.esOyente == 1;
                    existing.valida = item.valida;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncMatriculaExamLinksAsync()
        {
            var rows = await _centralProvider.GetMatriculaExamLinksFromCentralAsync();
            var matSet = (await _context.Matriculas.Select(m => m.idMatricula).ToListAsync()).ToHashSet();
            var catSet = (await _context.CategoriasExamenes.Select(c => c.IdCategoria).ToListAsync()).ToHashSet();
            var existingByKey = (await _context.MatriculasExamenesConduccion.ToListAsync())
                .ToDictionary(x => $"{x.idMatricula}:{x.IdCategoria}");

            var processed = 0;
            foreach (var item in rows)
            {
                if (!matSet.Contains(item.idMatricula) || !catSet.Contains(item.IdCategoria))
                    continue;

                var key = $"{item.idMatricula}:{item.IdCategoria}";
                existingByKey.TryGetValue(key, out var existing);
                var usuario = item.usuario?.Length > 50 ? item.usuario[..50] : item.usuario;
                var instructor = item.instructor?.Length > 80 ? item.instructor[..80] : item.instructor;

                if (existing == null)
                {
                    _context.MatriculasExamenesConduccion.Add(new MatriculaExamenConduccion
                    {
                        idMatricula = item.idMatricula,
                        IdCategoria = item.IdCategoria,
                        nota = item.nota,
                        observacion = item.observacion,
                        usuario = usuario,
                        fechaExamen = item.fechaExamen,
                        fechaIngreso = item.fechaIngreso,
                        instructor = instructor
                    });
                }
                else
                {
                    existing.nota = item.nota;
                    existing.observacion = item.observacion;
                    existing.usuario = usuario;
                    existing.fechaExamen = item.fechaExamen;
                    existing.fechaIngreso = item.fechaIngreso;
                    existing.instructor = instructor;
                }

                processed++;
            }

            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncPracticesFromSigafiAsync()
        {
            var rows = await _centralProvider.GetAllPracticesFromCentralAsync();
            var alumnoSet = (await _context.Estudiantes.Select(e => e.idAlumno).ToListAsync()).ToHashSet();
            var vehiculoSet = (await _context.Vehiculos.Select(v => v.idVehiculo).ToListAsync()).ToHashSet();
            var profesorSet = (await _context.Instructores.Select(i => i.idProfesor).ToListAsync()).ToHashSet();
            var existingIds = (await _context.Practicas.Select(p => p.idPractica).ToListAsync()).ToHashSet();
            var processed = 0;
            foreach (var item in rows)
            {
                if (string.IsNullOrWhiteSpace(item.idalumno) || string.IsNullOrWhiteSpace(item.idProfesor))
                    continue;
                if (item.idvehiculo <= 0)
                    continue;
                if (!alumnoSet.Contains(item.idalumno) || !vehiculoSet.Contains(item.idvehiculo) || !profesorSet.Contains(item.idProfesor))
                    continue;

                var mapped = BuildPracticaFromCentralSyncItem(item);
                if (!existingIds.Contains(item.idPractica))
                {
                    _context.Practicas.Add(mapped);
                    existingIds.Add(item.idPractica);
                }
                else
                {
                    _context.Practicas.Update(mapped);
                }

                processed++;
            }

            await _context.SaveChangesAsync();
            return processed;
        }

        private static Practica BuildPracticaFromCentralSyncItem(CentralPracticaSyncDto item)
        {
            var idPeriodo = item.idPeriodo?.Trim() ?? string.Empty;
            if (idPeriodo.Length > 10)
                idPeriodo = idPeriodo[..10];

            var userAsigna = item.user_asigna?.Length > 20 ? item.user_asigna[..20] : item.user_asigna;
            var userLlegada = item.user_llegada?.Length > 20 ? item.user_llegada[..20] : item.user_llegada;
            var dia = item.dia?.Length > 15 ? item.dia[..15] : item.dia;

            return new Practica
            {
                idPractica = item.idPractica,
                idalumno = item.idalumno,
                idvehiculo = item.idvehiculo,
                idProfesor = item.idProfesor,
                idPeriodo = idPeriodo,
                dia = dia,
                fecha = item.fecha,
                hora_salida = item.hora_salida,
                hora_llegada = item.hora_llegada,
                tiempo = item.tiempo,
                ensalida = (byte?)(item.ensalida != 0 ? 1 : 0),
                verificada = (byte?)(item.verificada != 0 ? 1 : 0),
                user_asigna = userAsigna,
                user_llegada = userLlegada,
                cancelado = (byte?)(item.cancelado != 0 ? 1 : 0),
                observaciones = item.observaciones
            };
        }

        private async Task<int> SyncVehiclesAsync()
        {
            var catToTipo = await _context.TipoLicencias
                .Where(t => t.id_categoria_sigafi != null)
                .ToDictionaryAsync(t => t.id_categoria_sigafi!.Value, t => t.id_tipo);

            var rows = await _centralProvider.GetAllVehiclesFromCentralAsync();
            var normalizedRows = rows
                .GroupBy(r => !string.IsNullOrWhiteSpace(r.numero_vehiculo)
                    ? $"NUM:{r.numero_vehiculo}"
                    : !string.IsNullOrWhiteSpace(r.placa)
                        ? $"PLA:{r.placa}"
                        : $"ID:{r.idVehiculo}")
                .Select(g => g.First());

            var processed = 0;
            foreach (var item in normalizedRows)
            {
                int? tipoLicenciaId = null;
                if (item.idCategoria.HasValue && catToTipo.TryGetValue(item.idCategoria.Value, out var mappedTipo))
                    tipoLicenciaId = mappedTipo;

                var existing = await _context.Vehiculos.FirstOrDefaultAsync(x =>
                    x.idVehiculo == item.idVehiculo
                    || (!string.IsNullOrEmpty(item.numero_vehiculo) && x.numero_vehiculo == item.numero_vehiculo)
                    || (!string.IsNullOrEmpty(item.placa) && x.placa == item.placa));
                if (existing == null)
                {
                    _context.Vehiculos.Add(new Vehiculo
                    {
                        idVehiculo = item.idVehiculo,
                        idSubcategoria = item.idSubcategoria,
                        numero_vehiculo = item.numero_vehiculo,
                        placa = item.placa,
                        marca = item.marca,
                        anio = item.anio,
                        idCategoria = item.idCategoria,
                        activo = item.activo == 1,
                        observacion = item.observacion?.Length > 200 ? item.observacion[..200] : item.observacion,
                        chasis = item.chasis?.Length > 100 ? item.chasis[..100] : item.chasis,
                        motor = item.motor?.Length > 100 ? item.motor[..100] : item.motor,
                        modelo = item.modelo?.Length > 100 ? item.modelo[..100] : item.modelo,
                        id_tipo_licencia = tipoLicenciaId ?? 1
                    });
                }
                else
                {
                    existing.idSubcategoria = item.idSubcategoria;
                    existing.numero_vehiculo = item.numero_vehiculo;
                    existing.placa = item.placa;
                    existing.marca = item.marca;
                    existing.anio = item.anio;
                    existing.idCategoria = item.idCategoria;
                    existing.activo = item.activo == 1;
                    existing.observacion = item.observacion?.Length > 200 ? item.observacion[..200] : item.observacion;
                    existing.chasis = item.chasis?.Length > 100 ? item.chasis[..100] : item.chasis;
                    existing.motor = item.motor?.Length > 100 ? item.motor[..100] : item.motor;
                    existing.modelo = item.modelo?.Length > 100 ? item.modelo[..100] : item.modelo;
                    if (tipoLicenciaId.HasValue)
                        existing.id_tipo_licencia = tipoLicenciaId.Value;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncInstructorVehicleAssignmentsAsync()
        {
            var rows = await _centralProvider.GetInstructorVehicleAssignmentsFromCentralAsync();
            var vehiculoSet = (await _context.Vehiculos.Select(v => v.idVehiculo).ToListAsync()).ToHashSet();
            var profesorSet = (await _context.Instructores.Select(i => i.idProfesor).ToListAsync()).ToHashSet();
            var existingIds = (await _context.AsignacionesInstructores.Select(x => x.idAsignacion).ToListAsync()).ToHashSet();
            var processed = 0;
            foreach (var item in rows)
            {
                if (string.IsNullOrWhiteSpace(item.idProfesor))
                    continue;
                if (!vehiculoSet.Contains(item.idVehiculo) || !profesorSet.Contains(item.idProfesor))
                    continue;

                var observacion = item.observacion?.Length > 255 ? item.observacion[..255] : item.observacion;
                var exists = existingIds.Contains(item.idAsignacion);
                if (!exists)
                {
                    _context.AsignacionesInstructores.Add(new AsignacionInstructorVehiculo
                    {
                        idAsignacion = item.idAsignacion,
                        idVehiculo = item.idVehiculo,
                        idProfesor = item.idProfesor,
                        fecha_asignacion = item.fecha_asignacion,
                        fecha_salida = item.fecha_salida,
                        activo = item.activo == 1,
                        usuario_asigna = item.usuario_asigna,
                        usuario_desactiva = item.usuario_desactiva,
                        observacion = observacion
                    });
                }
                else
                {
                    _context.AsignacionesInstructores.Update(new AsignacionInstructorVehiculo
                    {
                        idAsignacion = item.idAsignacion,
                        idVehiculo = item.idVehiculo,
                        idProfesor = item.idProfesor,
                        fecha_asignacion = item.fecha_asignacion,
                        fecha_salida = item.fecha_salida,
                        activo = item.activo == 1,
                        usuario_asigna = item.usuario_asigna,
                        usuario_desactiva = item.usuario_desactiva,
                        observacion = observacion
                    });
                }

                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncStudentVehicleAssignmentsAsync()
        {
            var rows = await _centralProvider.GetStudentVehicleAssignmentsFromCentralAsync();
            var alumnoSet = (await _context.Estudiantes.Select(e => e.idAlumno).ToListAsync()).ToHashSet();
            var vehiculoSet = (await _context.Vehiculos.Select(v => v.idVehiculo).ToListAsync()).ToHashSet();
            var profesorSet = (await _context.Instructores.Select(i => i.idProfesor).ToListAsync()).ToHashSet();
            var existingIds = (await _context.Asignaciones.Select(x => x.idAsignacion).ToListAsync()).ToHashSet();
            var processed = 0;
            foreach (var item in rows)
            {
                if (string.IsNullOrWhiteSpace(item.idAlumno) || string.IsNullOrWhiteSpace(item.idProfesor))
                    continue;
                if (!alumnoSet.Contains(item.idAlumno) || !vehiculoSet.Contains(item.idVehiculo) || !profesorSet.Contains(item.idProfesor))
                    continue;

                var idPeriodo = item.idPeriodo?.Trim() ?? string.Empty;
                if (idPeriodo.Length > 10)
                    idPeriodo = idPeriodo[..10];

                var exists = existingIds.Contains(item.idAsignacion);
                if (!exists)
                {
                    _context.Asignaciones.Add(new Asignacion
                    {
                        idAsignacion = item.idAsignacion,
                        idAlumno = item.idAlumno,
                        idVehiculo = item.idVehiculo,
                        idProfesor = item.idProfesor,
                        idPeriodo = idPeriodo,
                        fechaAsignacion = item.fechaAsignacion ?? DateTime.Now,
                        fechaInicio = item.fechaInicio,
                        fechaFin = item.fechaFin,
                        activa = (byte)(item.activa == 1 ? 1 : 0),
                        observacion = item.observacion
                    });
                }
                else
                {
                    _context.Asignaciones.Update(new Asignacion
                    {
                        idAsignacion = item.idAsignacion,
                        idAlumno = item.idAlumno,
                        idVehiculo = item.idVehiculo,
                        idProfesor = item.idProfesor,
                        idPeriodo = idPeriodo,
                        fechaAsignacion = item.fechaAsignacion ?? DateTime.Now,
                        fechaInicio = item.fechaInicio,
                        fechaFin = item.fechaFin,
                        activa = (byte)(item.activa == 1 ? 1 : 0),
                        observacion = item.observacion
                    });
                }

                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncSchedulesAsync()
        {
            var rows = await _centralProvider.GetAllSchedulesFromCentralAsync();
            var normalizedRows = rows
                .GroupBy(r => r.idAsignacionHorario)
                .Select(g => g.First());
            var processed = 0;
            var existingIds = await _context.HorariosAlumnos.AsNoTracking()
                .Select(x => x.idAsignacionHorario)
                .ToListAsync();
            var existingSet = existingIds.ToHashSet();
            var asignacionSet = (await _context.Asignaciones.AsNoTracking()
                .Select(a => a.idAsignacion)
                .ToListAsync()).ToHashSet();

            foreach (var item in normalizedRows)
            {
                if (!asignacionSet.Contains(item.idAsignacion))
                    continue;

                var activoHorario = item.activo != 0;

                if (!existingSet.Contains(item.idAsignacionHorario))
                {
                    _context.HorariosAlumnos.Add(new HorarioAlumno
                    {
                        idAsignacionHorario = item.idAsignacionHorario,
                        idAsignacion = item.idAsignacion,
                        idFecha = item.idFecha,
                        idHora = item.idHora,
                        asiste = (sbyte)item.asiste,
                        activo = activoHorario,
                        observacion = item.observacion
                    });
                    existingSet.Add(item.idAsignacionHorario);
                }
                else
                {
                    _context.HorariosAlumnos.Update(new HorarioAlumno
                    {
                        idAsignacionHorario = item.idAsignacionHorario,
                        idAsignacion = item.idAsignacion,
                        idFecha = item.idFecha,
                        idHora = item.idHora,
                        asiste = (sbyte)item.asiste,
                        activo = activoHorario,
                        observacion = item.observacion
                    });
                }

                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncPracticeScheduleLinksAsync()
        {
            var rows = await _centralProvider.GetPracticeScheduleLinksFromCentralAsync();
            var processed = 0;
            var existingKeys = await _context.PracticasHorarios
                .Select(x => new { x.idPractica, x.idAsignacionHorario })
                .ToListAsync();
            var keySet = new HashSet<string>(existingKeys.Select(k => $"{k.idPractica}:{k.idAsignacionHorario}"));
            var validPracticas = (await _context.Practicas.Select(p => p.idPractica).ToListAsync()).ToHashSet();
            var validHorarios = (await _context.HorariosAlumnos.Select(h => h.idAsignacionHorario).ToListAsync()).ToHashSet();

            foreach (var item in rows)
            {
                var key = $"{item.idPractica}:{item.idAsignacionHorario}";
                if (!keySet.Contains(key) && validPracticas.Contains(item.idPractica) && validHorarios.Contains(item.idAsignacionHorario))
                {
                    _context.PracticasHorarios.Add(new PracticaHorarioAlumno
                    {
                        idPractica = item.idPractica,
                        idAsignacionHorario = item.idAsignacionHorario
                    });
                    keySet.Add(key);
                    processed++;
                }
            }
            await _context.SaveChangesAsync();
            return processed;
        }
    }
}
