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
                                tipodocumento = ci.tipodocumento,
                                nombres = (ci.nombres ?? "").ToUpper(),
                                apellidos = (ci.apellidos ?? "").ToUpper(),
                                primerApellido = ci.primerApellido,
                                segundoApellido = ci.segundoApellido,
                                primerNombre = ci.primerNombre,
                                segundoNombre = ci.segundoNombre,
                                estadoCivil = ci.estadoCivil,
                                direccion = ci.direccion,
                                callePrincipal = ci.callePrincipal,
                                calleSecundaria = ci.calleSecundaria,
                                numeroCasa = ci.numeroCasa,
                                telefono = ci.telefono,
                                celular = ci.celular?.Length > 50 ? ci.celular[..50] : ci.celular,
                                email = ci.email,
                                fecha_nacimiento = ci.fecha_nacimiento,
                                sexo = ci.sexo,
                                clave = ci.clave,
                                practicas = ci.practicas,
                                tipo = ci.tipo,
                                nacionalidad = ci.nacionalidad,
                                titulo = ci.titulo,
                                abreviatura = ci.abreviatura,
                                abreviatura_post = ci.abreviatura_post,
                                activo = ci.activo == 1,
                                idEtnia = ci.idEtnia,
                                idNacionalidad = ci.idNacionalidad,
                                idParroquiaNacimiento = ci.idParroquiaNacimiento,
                                emailInstitucional = ci.emailInstitucional,
                                fecha_ingreso = ci.fecha_ingreso,
                                fechaIngresoIess = ci.fechaIngresoIess,
                                fecha_retiro = ci.fecha_retiro,
                                idParroquiaResidencia = ci.idParroquiaResidencia,
                                tipoSangre = ci.tipoSangre,
                                codigoPostal = ci.codigoPostal,
                                idDiscapacidad = ci.idDiscapacidad,
                                porcentajeDiscapacidad = ci.porcentajeDiscapacidad,
                                numeroConadis = ci.numeroConadis,
                                foto = ci.foto,
                                esReal = ci.esReal
                            });
                            log.RegistrosProcesados++;
                        }
                        else
                        {
                            existing.tipodocumento = ci.tipodocumento;
                            existing.nombres = (ci.nombres ?? "").ToUpper();
                            existing.apellidos = (ci.apellidos ?? "").ToUpper();
                            existing.primerApellido = ci.primerApellido;
                            existing.segundoApellido = ci.segundoApellido;
                            existing.primerNombre = ci.primerNombre;
                            existing.segundoNombre = ci.segundoNombre;
                            existing.estadoCivil = ci.estadoCivil;
                            existing.direccion = ci.direccion;
                            existing.callePrincipal = ci.callePrincipal;
                            existing.calleSecundaria = ci.calleSecundaria;
                            existing.numeroCasa = ci.numeroCasa;
                            existing.telefono = ci.telefono;
                            existing.celular = ci.celular?.Length > 50 ? ci.celular[..50] : ci.celular;
                            existing.email = ci.email;
                            existing.fecha_nacimiento = ci.fecha_nacimiento;
                            existing.sexo = ci.sexo;
                            existing.clave = ci.clave;
                            existing.practicas = ci.practicas;
                            existing.tipo = ci.tipo;
                            existing.nacionalidad = ci.nacionalidad;
                            existing.titulo = ci.titulo;
                            existing.abreviatura = ci.abreviatura;
                            existing.abreviatura_post = ci.abreviatura_post;
                            existing.activo = ci.activo == 1;
                            existing.idEtnia = ci.idEtnia;
                            existing.idNacionalidad = ci.idNacionalidad;
                            existing.idParroquiaNacimiento = ci.idParroquiaNacimiento;
                            existing.emailInstitucional = ci.emailInstitucional;
                            existing.fecha_ingreso = ci.fecha_ingreso;
                            existing.fechaIngresoIess = ci.fechaIngresoIess;
                            existing.fecha_retiro = ci.fecha_retiro;
                            existing.idParroquiaResidencia = ci.idParroquiaResidencia;
                            existing.tipoSangre = ci.tipoSangre;
                            existing.codigoPostal = ci.codigoPostal;
                            existing.idDiscapacidad = ci.idDiscapacidad;
                            existing.porcentajeDiscapacidad = ci.porcentajeDiscapacidad;
                            existing.numeroConadis = ci.numeroConadis;
                            existing.foto = ci.foto;
                            existing.esReal = ci.esReal;
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

            log.RegistrosProcesados += await ExecuteSyncStepAsync("periodos", SyncPeriodosAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("carreras", SyncCarrerasAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("secciones", SyncSeccionesAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("modalidades", SyncModalidadesAsync, warnings, log);
            log.RegistrosProcesados += await ExecuteSyncStepAsync("instituciones", SyncInstitucionesAsync, warnings, log);
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
                                salida = eu.salida == 1,
                                ingreso = eu.ingreso == 1,
                                activo = eu.activo == 1,
                                asistencia = eu.asistencia == 1,
                                esRrhh = eu.esRrhh == 1,
                                creado_en = DateTime.Now
                            });
                        }
                        else
                        {
                            existing.activo = eu.activo == 1;
                            existing.rol = role;
                            existing.salida = eu.salida == 1;
                            existing.ingreso = eu.ingreso == 1;
                            existing.asistencia = eu.asistencia == 1;
                            existing.esRrhh = eu.esRrhh == 1;
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
                        tipodocumento = ci.tipodocumento,
                        nombres = (ci.nombres ?? "").ToUpper(),
                        apellidos = (ci.apellidos ?? "").ToUpper(),
                        primerApellido = ci.primerApellido,
                        segundoApellido = ci.segundoApellido,
                        primerNombre = ci.primerNombre,
                        segundoNombre = ci.segundoNombre,
                        estadoCivil = ci.estadoCivil,
                        direccion = ci.direccion,
                        callePrincipal = ci.callePrincipal,
                        calleSecundaria = ci.calleSecundaria,
                        numeroCasa = ci.numeroCasa,
                        telefono = ci.telefono,
                        celular = ci.celular?.Length > 50 ? ci.celular[..50] : ci.celular,
                        email = ci.email,
                        fecha_nacimiento = ci.fecha_nacimiento,
                        sexo = ci.sexo,
                        clave = ci.clave,
                        practicas = ci.practicas,
                        tipo = ci.tipo,
                        nacionalidad = ci.nacionalidad,
                        titulo = ci.titulo,
                        abreviatura = ci.abreviatura,
                        abreviatura_post = ci.abreviatura_post,
                        activo = ci.activo == 1,
                        idEtnia = ci.idEtnia,
                        idNacionalidad = ci.idNacionalidad,
                        idParroquiaNacimiento = ci.idParroquiaNacimiento,
                        emailInstitucional = ci.emailInstitucional,
                        fecha_ingreso = ci.fecha_ingreso,
                        fechaIngresoIess = ci.fechaIngresoIess,
                        fecha_retiro = ci.fecha_retiro,
                        idParroquiaResidencia = ci.idParroquiaResidencia,
                        tipoSangre = ci.tipoSangre,
                        codigoPostal = ci.codigoPostal,
                        idDiscapacidad = ci.idDiscapacidad,
                        porcentajeDiscapacidad = ci.porcentajeDiscapacidad,
                        numeroConadis = ci.numeroConadis,
                        foto = ci.foto,
                        esReal = ci.esReal
                    };
                    _context.Instructores.Add(nuevo);
                    byId[ci.idProfesor] = nuevo;
                }
                else
                {
                    existing.tipodocumento = ci.tipodocumento;
                    existing.nombres = (ci.nombres ?? "").ToUpper();
                    existing.apellidos = (ci.apellidos ?? "").ToUpper();
                    existing.primerApellido = ci.primerApellido;
                    existing.segundoApellido = ci.segundoApellido;
                    existing.primerNombre = ci.primerNombre;
                    existing.segundoNombre = ci.segundoNombre;
                    existing.estadoCivil = ci.estadoCivil;
                    existing.direccion = ci.direccion;
                    existing.callePrincipal = ci.callePrincipal;
                    existing.calleSecundaria = ci.calleSecundaria;
                    existing.numeroCasa = ci.numeroCasa;
                    existing.telefono = ci.telefono;
                    existing.celular = ci.celular?.Length > 50 ? ci.celular[..50] : ci.celular;
                    existing.email = ci.email;
                    existing.fecha_nacimiento = ci.fecha_nacimiento;
                    existing.sexo = ci.sexo;
                    existing.clave = ci.clave;
                    existing.practicas = ci.practicas;
                    existing.tipo = ci.tipo;
                    existing.nacionalidad = ci.nacionalidad;
                    existing.titulo = ci.titulo;
                    existing.abreviatura = ci.abreviatura;
                    existing.abreviatura_post = ci.abreviatura_post;
                    existing.activo = ci.activo == 1;
                    existing.idEtnia = ci.idEtnia;
                    existing.idNacionalidad = ci.idNacionalidad;
                    existing.idParroquiaNacimiento = ci.idParroquiaNacimiento;
                    existing.emailInstitucional = ci.emailInstitucional;
                    existing.fecha_ingreso = ci.fecha_ingreso;
                    existing.fechaIngresoIess = ci.fechaIngresoIess;
                    existing.fecha_retiro = ci.fecha_retiro;
                    existing.idParroquiaResidencia = ci.idParroquiaResidencia;
                    existing.tipoSangre = ci.tipoSangre;
                    existing.codigoPostal = ci.codigoPostal;
                    existing.idDiscapacidad = ci.idDiscapacidad;
                    existing.porcentajeDiscapacidad = ci.porcentajeDiscapacidad;
                    existing.numeroConadis = ci.numeroConadis;
                    existing.foto = ci.foto;
                    existing.esReal = ci.esReal;
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
            var byId = await _context.Estudiantes.ToDictionaryAsync(e => e.idAlumno);
            var processed = 0;
            foreach (var item in rows)
            {
                if (!byId.TryGetValue(item.idAlumno, out var existing))
                {
                    var nuevo = new Estudiante
                    {
                        idAlumno = item.idAlumno,
                        tipoDocumento = item.tipoDocumento,
                        primerNombre = item.primerNombre ?? "",
                        segundoNombre = item.segundoNombre,
                        apellidoPaterno = item.apellidoPaterno ?? "",
                        apellidoMaterno = item.apellidoMaterno,
                        fecha_Nacimiento = item.fecha_Nacimiento,
                        direccion = item.direccion,
                        telefono = item.telefono,
                        celular = item.celular?.Length > 50 ? item.celular[..50] : item.celular,
                        email = item.email?.Length > 100 ? item.email[..100] : item.email,
                        ciudad_Nacimiento = item.ciudad_Nacimiento,
                        provincia_Nacimiento = item.provincia_Nacimiento,
                        sexo = item.sexo,
                        nacionalidad = item.nacionalidad,
                        idNivel = item.idNivel,
                        idPeriodo = item.idPeriodo,
                        idSeccion = item.idSeccion,
                        idModalidad = item.idModalidad,
                        idInstitucion = item.idInstitucion,
                        tituloColegio = item.tituloColegio,
                        fecha_Inscripcion = item.fecha_Inscripcion,
                        parroquia_nacimiento = item.parroquia_nacimiento,
                        nombre_padre = item.nombre_padre,
                        ocupacion_padre = item.ocupacion_padre,
                        nacionalidad_padre = item.nacionalidad_padre,
                        nombre_madre = item.nombre_madre,
                        ocupacion_madre = item.ocupacion_madre,
                        nacionalidad_madre = item.nacionalidad_madre,
                        barrio_residencia = item.barrio_residencia,
                        parroquia_residencia = item.parroquia_residencia,
                        ciudad_residencia = item.ciudad_residencia,
                        tipo_sangre = item.tipo_sangre,
                        user_alumno = item.user_alumno,
                        password = item.password,
                        idDiscapacidad = item.idDiscapacidad,
                        idEtnia = item.idEtnia,
                        idNacionalidad = item.idNacionalidad,
                        porcentaje_discapacidad = item.porcentaje_discapacidad,
                        carnet_conadis = item.carnet_conadis,
                        email_institucional = item.email_institucional,
                        primerIngreso = item.primerIngreso,
                        archivofoto = item.archivofoto,
                        activo = true
                    };
                    _context.Estudiantes.Add(nuevo);
                    byId[item.idAlumno] = nuevo;
                }
                else
                {
                    existing.tipoDocumento = item.tipoDocumento;
                    existing.primerNombre = item.primerNombre ?? existing.primerNombre;
                    existing.segundoNombre = item.segundoNombre;
                    existing.apellidoPaterno = item.apellidoPaterno ?? existing.apellidoPaterno;
                    existing.apellidoMaterno = item.apellidoMaterno;
                    existing.fecha_Nacimiento = item.fecha_Nacimiento;
                    existing.direccion = item.direccion;
                    existing.telefono = item.telefono;
                    existing.celular = item.celular?.Length > 50 ? item.celular[..50] : item.celular;
                    existing.email = item.email?.Length > 100 ? item.email[..100] : item.email;
                    existing.ciudad_Nacimiento = item.ciudad_Nacimiento;
                    existing.provincia_Nacimiento = item.provincia_Nacimiento;
                    existing.sexo = item.sexo;
                    existing.nacionalidad = item.nacionalidad;
                    existing.idNivel = item.idNivel;
                    existing.idPeriodo = item.idPeriodo ?? existing.idPeriodo;
                    existing.idSeccion = item.idSeccion ?? existing.idSeccion;
                    existing.idModalidad = item.idModalidad ?? existing.idModalidad;
                    existing.idInstitucion = item.idInstitucion;
                    existing.tituloColegio = item.tituloColegio;
                    existing.fecha_Inscripcion = item.fecha_Inscripcion;
                    existing.parroquia_nacimiento = item.parroquia_nacimiento;
                    existing.nombre_padre = item.nombre_padre;
                    existing.ocupacion_padre = item.ocupacion_padre;
                    existing.nacionalidad_padre = item.nacionalidad_padre;
                    existing.nombre_madre = item.nombre_madre;
                    existing.ocupacion_madre = item.ocupacion_madre;
                    existing.nacionalidad_madre = item.nacionalidad_madre;
                    existing.barrio_residencia = item.barrio_residencia;
                    existing.parroquia_residencia = item.parroquia_residencia;
                    existing.ciudad_residencia = item.ciudad_residencia;
                    existing.tipo_sangre = item.tipo_sangre;
                    existing.user_alumno = item.user_alumno;
                    existing.password = item.password;
                    existing.idDiscapacidad = item.idDiscapacidad;
                    existing.idEtnia = item.idEtnia;
                    existing.idNacionalidad = item.idNacionalidad;
                    existing.porcentaje_discapacidad = item.porcentaje_discapacidad;
                    existing.carnet_conadis = item.carnet_conadis;
                    existing.email_institucional = item.email_institucional;
                    existing.primerIngreso = item.primerIngreso;
                    existing.archivofoto = item.archivofoto;
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
                        beca_colegiatura = item.beca_colegiatura,
                        retirado = item.retirado == 1,
                        fechaRetiro = item.fechaRetiro,
                        observacion = item.observacion,
                        convalidacion = item.convalidacion == 1,
                        carrera_convalidada = item.carrera_convalidada,
                        numero_permiso = item.numero_permiso,
                        user_matricula = item.user_matricula,
                        valida = item.valida,
                        esOyente = item.esOyente == 1,
                        documentoFactura = item.documentoFactura,
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
                    existing.beca_colegiatura = item.beca_colegiatura;
                    existing.retirado = item.retirado == 1;
                    existing.fechaRetiro = item.fechaRetiro;
                    existing.observacion = item.observacion;
                    existing.convalidacion = item.convalidacion == 1;
                    existing.carrera_convalidada = item.carrera_convalidada;
                    existing.numero_permiso = item.numero_permiso;
                    existing.user_matricula = item.user_matricula;
                    existing.valida = item.valida;
                    existing.esOyente = item.esOyente == 1;
                    existing.documentoFactura = item.documentoFactura;
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
                    _context.Set<Vehiculo>().Add(new Vehiculo
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

        private async Task<int> SyncPeriodosAsync()
        {
            var rows = await _centralProvider.GetAllPeriodosFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Set<Periodo>().FirstOrDefaultAsync(x => x.idPeriodo == item.idPeriodo);
                if (existing == null)
                {
                    _context.Set<Periodo>().Add(new Periodo
                    {
                        idPeriodo = item.idPeriodo,
                        detalle = item.detalle,
                        fecha_inicial = item.fecha_inicial,
                        fecha_final = item.fecha_final,
                        cerrado = item.cerrado == 1,
                        fecha_maxima_autocierre = item.fecha_maxima_autocierre,
                        activo = item.activo == 1,
                        creditos = item.creditos == 1,
                        numero_pagos = item.numero_pagos,
                        fecha_matrucla_extraordinaria = item.fecha_matrucla_extraordinaria,
                        foliop = item.foliop,
                        permiteMatricula = item.permiteMatricula == 1,
                        ingresoCalificaciones = item.ingresoCalificaciones == 1,
                        permiteCalificacionesInstituto = item.permiteCalificacionesInstituto == 1,
                        periodoactivoinstituto = item.periodoactivoinstituto == 1,
                        visualizaPowerBi = item.visualizaPowerBi == 1,
                        esInstituto = item.esInstituto == 1,
                        periodoPlanificacion = item.periodoPlanificacion == 1
                    });
                }
                else
                {
                    existing.detalle = item.detalle;
                    existing.fecha_inicial = item.fecha_inicial;
                    existing.fecha_final = item.fecha_final;
                    existing.cerrado = item.cerrado == 1;
                    existing.fecha_maxima_autocierre = item.fecha_maxima_autocierre;
                    existing.activo = item.activo == 1;
                    existing.creditos = item.creditos == 1;
                    existing.numero_pagos = item.numero_pagos;
                    existing.fecha_matrucla_extraordinaria = item.fecha_matrucla_extraordinaria;
                    existing.foliop = item.foliop;
                    existing.permiteMatricula = item.permiteMatricula == 1;
                    existing.ingresoCalificaciones = item.ingresoCalificaciones == 1;
                    existing.permiteCalificacionesInstituto = item.permiteCalificacionesInstituto == 1;
                    existing.periodoactivoinstituto = item.periodoactivoinstituto == 1;
                    existing.visualizaPowerBi = item.visualizaPowerBi == 1;
                    existing.esInstituto = item.esInstituto == 1;
                    existing.periodoPlanificacion = item.periodoPlanificacion == 1;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncCarrerasAsync()
        {
            var rows = await _centralProvider.GetAllCarrerasFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Carreras.FirstOrDefaultAsync(x => x.idCarrera == item.idCarrera);
                if (existing == null)
                {
                    _context.Carreras.Add(new Carrera
                    {
                        idCarrera = item.idCarrera,
                        CarreraNombre = item.Carrera,
                        fechaCreacion = item.fechaCreacion,
                        activa = item.activa == 1,
                        directorCarrera = item.directorCarrera,
                        numero_creditos = item.numero_creditos,
                        ordenCarrera = item.ordenCarrera,
                        numero_alumnos = item.numero_alumnos,
                        revisaArrastres = item.revisaArrastres == 1,
                        codigo_cases = item.codigo_cases,
                        aliasCarrera = item.aliasCarrera,
                        BolsaEmpleo = item.BolsaEmpleo == 1,
                        esInstituto = item.esInstituto == 1
                    });
                }
                else
                {
                    existing.CarreraNombre = item.Carrera;
                    existing.fechaCreacion = item.fechaCreacion;
                    existing.activa = item.activa == 1;
                    existing.directorCarrera = item.directorCarrera;
                    existing.numero_creditos = item.numero_creditos;
                    existing.ordenCarrera = item.ordenCarrera;
                    existing.numero_alumnos = item.numero_alumnos;
                    existing.revisaArrastres = item.revisaArrastres == 1;
                    existing.codigo_cases = item.codigo_cases;
                    existing.aliasCarrera = item.aliasCarrera;
                    existing.BolsaEmpleo = item.BolsaEmpleo == 1;
                    existing.esInstituto = item.esInstituto == 1;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncSeccionesAsync()
        {
            var rows = await _centralProvider.GetAllSeccionesFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Set<Seccion>().FirstOrDefaultAsync(x => x.idSeccion == item.idSeccion);
                if (existing == null)
                {
                    _context.Set<Seccion>().Add(new Seccion
                    {
                        idSeccion = item.idSeccion,
                        seccion = item.seccion,
                        sufijo = item.sufijo
                    });
                }
                else
                {
                    existing.seccion = item.seccion;
                    existing.sufijo = item.sufijo;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncModalidadesAsync()
        {
            var rows = await _centralProvider.GetAllModalidadesFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Modalidades.FirstOrDefaultAsync(x => x.idModalidad == item.idModalidad);
                if (existing == null)
                {
                    _context.Modalidades.Add(new Modalidad
                    {
                        idModalidad = item.idModalidad,
                        modalidad = item.modalidad,
                        sufijo = item.sufijo
                    });
                }
                else
                {
                    existing.modalidad = item.modalidad;
                    existing.sufijo = item.sufijo;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncInstitucionesAsync()
        {
            var rows = await _centralProvider.GetAllInstitucionesFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Instituciones.FirstOrDefaultAsync(x => x.idInstitucion == item.idInstitucion);
                if (existing == null)
                {
                    _context.Instituciones.Add(new Institucion
                    {
                        idInstitucion = item.idInstitucion,
                        InstitucionNombre = item.Institucion,
                        ciudad = item.ciudad,
                        provincia = item.provincia
                    });
                }
                else
                {
                    existing.InstitucionNombre = item.Institucion;
                    existing.ciudad = item.ciudad;
                    existing.provincia = item.provincia;
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
                        fecha_salidad = item.fecha_salidad,
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
                        fecha_salidad = item.fecha_salidad,
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
