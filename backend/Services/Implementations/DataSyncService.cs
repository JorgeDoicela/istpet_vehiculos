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

        public async Task<SyncLog> MasterSyncAsync()
        {
            _logger.LogInformation("Iniciando Master Sync SIGAFI integral...");
            var log = new SyncLog
            {
                Modulo = "MasterSyncSIGAFI",
                Origen = "SIGAFI_SQL_BRIDGE",
                Fecha = DateTime.Now,
                RegistrosProcesados = 0,
                RegistrosFallidos = 0
            };

            try
            {
                var licenseCount = await SyncLicenseTypesAsync();
                var courseCount = await SyncCoursesAsync();
                var vehCategoriesCount = await SyncVehicleCategoriesAsync();
                var examCategoriesCount = await SyncExamCategoriesAsync();
                var instructorsCount = await SyncInstructorsInternalAsync();
                var usersCount = await SyncWebUsersInternalAsync();
                var studentsCount = await SyncStudentsFromSigafiAsync();
                var enrollmentCount = await SyncEnrollmentsAsync();
                var vehiclesCount = await SyncVehiclesAsync();
                var assignInstructorCount = await SyncInstructorVehicleAssignmentsAsync();
                var assignStudentCount = await SyncStudentVehicleAssignmentsAsync();
                var schedulesCount = await SyncSchedulesAsync();
                var practiceLinksCount = await SyncPracticeScheduleLinksAsync();

                log.RegistrosProcesados = licenseCount + courseCount + vehCategoriesCount + examCategoriesCount
                    + instructorsCount + usersCount + studentsCount + enrollmentCount + vehiclesCount
                    + assignInstructorCount + assignStudentCount + schedulesCount + practiceLinksCount;
                log.Estado = "OK";
                log.Mensaje = "Master Sync SIGAFI completado correctamente.";
            }
            catch (Exception ex)
            {
                log.Estado = "ERROR";
                log.RegistrosFallidos++;
                log.Mensaje = $"Error crítico en Master Sync: {ex.Message}";
                _logger.LogError(ex, "Master Sync SIGAFI falló.");
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
                var existing = await _context.TipoLicencias.FirstOrDefaultAsync(x => x.id_tipo == item.id_tipo);
                if (existing == null)
                {
                    _context.TipoLicencias.Add(new TipoLicencia
                    {
                        id_tipo = item.id_tipo,
                        codigo = item.codigo,
                        descripcion = item.descripcion,
                        activo = item.activo == 1
                    });
                }
                else
                {
                    existing.codigo = item.codigo;
                    existing.descripcion = item.descripcion;
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
                        Nivel = item.Nivel?.Length > 20 ? item.Nivel[..20] : item.Nivel
                    });
                }
                else
                {
                    existing.idCarrera = item.idCarrera;
                    existing.Nivel = item.Nivel?.Length > 20 ? item.Nivel[..20] : item.Nivel;
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
            var processed = 0;
            foreach (var ci in rows)
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
                        celular = ci.celular?.Length > 50 ? ci.celular[..50] : ci.celular,
                        email = ci.email,
                        activo = ci.activo == 1
                    });
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
            var processed = 0;
            foreach (var eu in rows)
            {
                var existing = await _context.Usuarios.FirstOrDefaultAsync(u => u.usuario == eu.usuario);
                if (existing == null)
                {
                    _context.Usuarios.Add(new Usuario
                    {
                        usuario = eu.usuario,
                        password = eu.password,
                        salida = eu.salida == 1,
                        ingreso = eu.ingreso == 1,
                        activo = eu.activo == 1,
                        asistencia = eu.asistencia == 1,
                        esRrhh = eu.esRrhh == 1
                    });
                }
                else
                {
                    existing.salida = eu.salida == 1;
                    existing.ingreso = eu.ingreso == 1;
                    existing.activo = eu.activo == 1;
                    existing.asistencia = eu.asistencia == 1;
                    existing.esRrhh = eu.esRrhh == 1;
                    if (!existing.password.StartsWith("$2a$") && !existing.password.StartsWith("$2b$"))
                    {
                        existing.password = eu.password;
                    }
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncStudentsFromSigafiAsync()
        {
            var rows = await _centralProvider.GetAllStudentsFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Estudiantes.FindAsync(item.idAlumno);
                if (existing == null)
                {
                    _context.Estudiantes.Add(new Estudiante
                    {
                        idAlumno = item.idAlumno,
                        primerNombre = item.primerNombre ?? "",
                        segundoNombre = item.segundoNombre,
                        apellidoPaterno = item.apellidoPaterno ?? "",
                        apellidoMaterno = item.apellidoMaterno,
                        celular = item.celular?.Length > 50 ? item.celular[..50] : item.celular,
                        email = item.email?.Length > 100 ? item.email[..100] : item.email,
                        activo = true
                    });
                }
                else
                {
                    existing.primerNombre = item.primerNombre ?? existing.primerNombre;
                    existing.segundoNombre = item.segundoNombre;
                    existing.apellidoPaterno = item.apellidoPaterno ?? existing.apellidoPaterno;
                    existing.apellidoMaterno = item.apellidoMaterno;
                    existing.celular = item.celular?.Length > 50 ? item.celular[..50] : item.celular;
                    existing.email = item.email?.Length > 100 ? item.email[..100] : item.email;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncEnrollmentsAsync()
        {
            var rows = await _centralProvider.GetActiveEnrollmentsFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Matriculas.FirstOrDefaultAsync(x => x.idMatricula == item.idMatricula);
                if (existing == null)
                {
                    _context.Matriculas.Add(new Matricula
                    {
                        idMatricula = item.idMatricula,
                        idAlumno = item.idAlumno,
                        idNivel = item.idNivel,
                        idSeccion = item.idSeccion,
                        idModalidad = item.idModalidad,
                        idPeriodo = item.idPeriodo,
                        fechaMatricula = item.fechaMatricula,
                        paralelo = item.paralelo,
                        valida = item.valida,
                        estado = "ACTIVO"
                    });
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
                    existing.valida = item.valida;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncVehiclesAsync()
        {
            var rows = await _centralProvider.GetAllVehiclesFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Vehiculos.FirstOrDefaultAsync(x => x.idVehiculo == item.idVehiculo);
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
                        modelo = item.modelo?.Length > 100 ? item.modelo[..100] : item.modelo
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
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncInstructorVehicleAssignmentsAsync()
        {
            var rows = await _centralProvider.GetInstructorVehicleAssignmentsFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.AsignacionesInstructores.FirstOrDefaultAsync(x => x.idAsignacion == item.idAsignacion);
                if (existing == null)
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
                        observacion = item.observacion?.Length > 255 ? item.observacion[..255] : item.observacion
                    });
                }
                else
                {
                    existing.idVehiculo = item.idVehiculo;
                    existing.idProfesor = item.idProfesor;
                    existing.fecha_asignacion = item.fecha_asignacion;
                    existing.fecha_salida = item.fecha_salida;
                    existing.activo = item.activo == 1;
                    existing.usuario_asigna = item.usuario_asigna;
                    existing.usuario_desactiva = item.usuario_desactiva;
                    existing.observacion = item.observacion?.Length > 255 ? item.observacion[..255] : item.observacion;
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncStudentVehicleAssignmentsAsync()
        {
            var rows = await _centralProvider.GetStudentVehicleAssignmentsFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.Asignaciones.FirstOrDefaultAsync(x => x.idAsignacion == item.idAsignacion);
                if (existing == null)
                {
                    _context.Asignaciones.Add(new Asignacion
                    {
                        idAsignacion = item.idAsignacion,
                        idAlumno = item.idAlumno,
                        idVehiculo = item.idVehiculo,
                        idProfesor = item.idProfesor,
                        idPeriodo = item.idPeriodo ?? "",
                        activa = (byte)(item.activa == 1 ? 1 : 0)
                    });
                }
                else
                {
                    existing.idAlumno = item.idAlumno;
                    existing.idVehiculo = item.idVehiculo;
                    existing.idProfesor = item.idProfesor;
                    existing.idPeriodo = item.idPeriodo ?? existing.idPeriodo;
                    existing.activa = (byte)(item.activa == 1 ? 1 : 0);
                }
                processed++;
            }
            await _context.SaveChangesAsync();
            return processed;
        }

        private async Task<int> SyncSchedulesAsync()
        {
            var rows = await _centralProvider.GetAllSchedulesFromCentralAsync();
            var processed = 0;
            foreach (var item in rows)
            {
                var existing = await _context.HorariosAlumnos.FirstOrDefaultAsync(x => x.idAsignacionHorario == item.idAsignacionHorario);
                if (existing == null)
                {
                    _context.HorariosAlumnos.Add(new HorarioAlumno
                    {
                        idAsignacionHorario = item.idAsignacionHorario,
                        idAsignacion = item.idAsignacion,
                        asiste = (sbyte)item.asiste,
                        activo = true,
                        observacion = item.Hora
                    });
                }
                else
                {
                    existing.idAsignacion = item.idAsignacion;
                    existing.asiste = (sbyte)item.asiste;
                    existing.activo = true;
                    existing.observacion = item.Hora;
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
            foreach (var item in rows)
            {
                var existing = await _context.PracticasHorarios.FirstOrDefaultAsync(x =>
                    x.idPractica == item.idPractica && x.idAsignacionHorario == item.idAsignacionHorario);
                if (existing == null)
                {
                    _context.PracticasHorarios.Add(new PracticaHorarioAlumno
                    {
                        idPractica = item.idPractica,
                        idAsignacionHorario = item.idAsignacionHorario
                    });
                    processed++;
                }
            }
            await _context.SaveChangesAsync();
            return processed;
        }
    }
}
