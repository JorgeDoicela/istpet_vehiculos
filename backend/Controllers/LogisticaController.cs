using backend.Data;
using backend.DTOs;
using backend.Services.Helpers;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using backend.Models;

namespace backend.Controllers
{
    /**
     * Logistica Controller: Absolute SIGAFI Parity Edition 2026.
     * Guaranteed 1:1 naming with central database for idAlumno, idProfesor, idVehiculo.
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,logistica,guardia")]
    public class LogisticaController : ControllerBase
    {
        private sealed class VehiculoLite
        {
            public int idVehiculo { get; set; }
            public string? numero_vehiculo { get; set; }
            public string? placa { get; set; }
            public string? idInstructorFijo { get; set; }
            public int idTipoLicencia { get; set; }
            public string estadoMecanico { get; set; } = "OPERATIVO";
        }

        private readonly AppDbContext _context;
        private readonly ILogisticaService _logisticaService;
        private readonly ICentralStudentProvider _centralProvider;
        private readonly IAuditService _audit;
        private readonly ISigafiMirrorPersistenceService _mirrorPersist;
        private readonly ILogger<LogisticaController> _logger;

        public LogisticaController(
            AppDbContext context,
            ILogisticaService logisticaService,
            ICentralStudentProvider centralProvider,
            IAuditService audit,
            ISigafiMirrorPersistenceService mirrorPersist,
            ILogger<LogisticaController> logger)
        {
            _context = context;
            _logisticaService = logisticaService;
            _centralProvider = centralProvider;
            _audit = audit;
            _mirrorPersist = mirrorPersist;
            _logger = logger;
        }

        [HttpGet("estudiante/{idAlumno}")]
        public async Task<ActionResult<ApiResponse<EstudianteLogisticaResponse>>> BuscarEstudiante(
            string idAlumno,
            [FromQuery] int? idVehiculoAgenda = null,
            [FromQuery] string? idProfesorAgenda = null,
            [FromQuery] int? idPracticaAgenda = null,
            [FromQuery] int? idAsignacionHorario = null)
        {
            idAlumno = idAlumno.Trim();

            // Solo SIGAFI: el espejo local antes rellenaba periodo/nivel viejos (p. ej. ABR2024 / PRIMERO) cuando fallaba la lectura central.
            CentralStudentDto? centralData;
            try
            {
                centralData = await _centralProvider.GetFromCentralAsync(idAlumno);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SIGAFI no disponible al buscar estudiante {idAlumno}", idAlumno);
                return StatusCode(503, ApiResponse<EstudianteLogisticaResponse>.Fail(
                    "SIGAFI no disponible. No se muestran datos del espejo local para evitar periodos o niveles desactualizados.",
                    ex.Message));
            }

            if (centralData == null)
            {
                return NotFound(ApiResponse<EstudianteLogisticaResponse>.Fail(
                    "Estudiante no localizado en SIGAFI. Verifique la cédula o que exista en alumnos."));
            }

            try
            {
                var fromCentral = await BuildLogisticaFromSigafiAndPersistAsync(centralData);
                await EnrichEstudianteLogisticaDesdeSigafiAsync(fromCentral);
                await AplicarContextoFilaAgendaAsync(fromCentral, idVehiculoAgenda, idProfesorAgenda, idPracticaAgenda, idAsignacionHorario);
                fromCentral.isBusy = await _context.Practicas
                    .AnyAsync(p => p.idalumno == fromCentral.idAlumno && p.ensalida == 1 && p.cancelado == 0);
                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(fromCentral, "Datos vigentes desde SIGAFI."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CRÍTICO al materializar alumno {idAlumno} desde SIGAFI. Detalle: {message}", idAlumno, ex.Message);
                var inner = ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "";
                return StatusCode(500, ApiResponse<EstudianteLogisticaResponse>.Fail($"Error al materializar alumno desde SIGAFI: {ex.Message}{inner}"));
            }
        }

        private async Task AplicarContextoFilaAgendaAsync(
            EstudianteLogisticaResponse student,
            int? idVehiculoAgenda,
            string? idProfesorAgenda,
            int? idPracticaAgenda,
            int? idAsignacionHorario)
        {
            if (idVehiculoAgenda is > 0)
            {
                student.idPracticaCentral = idVehiculoAgenda;
                var v = await _context.Vehiculos.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.idVehiculo == idVehiculoAgenda.Value);
                if (v != null)
                    student.practicaVehiculo = $"#{v.numero_vehiculo} ({v.placa})";
            }

            var profAg = idProfesorAgenda?.Trim();
            if (!string.IsNullOrEmpty(profAg))
            {
                student.idPracticaInstructor = profAg;
                var ins = await _context.Instructores.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.idProfesor == profAg);
                if (ins != null)
                    student.practicaInstructor = $"{ins.apellidos} {ins.nombres}".Trim();
            }

            if (idAsignacionHorario is > 0)
            {
                student.idAsignacionHorario = idAsignacionHorario;
            }
            else if (idPracticaAgenda is > 0)
            {
                var link = await _context.PracticasHorarios.AsNoTracking()
                    .Where(x => x.idPractica == idPracticaAgenda.Value)
                    .Select(x => (int?)x.idAsignacionHorario)
                    .FirstOrDefaultAsync();
                if (link.HasValue)
                    student.idAsignacionHorario = link;
            }
        }

        private static string ConstruirDetalleMatriculaSigafi(string? detalleRaw, string? jornadaDisplay)
        {
            var det = (detalleRaw ?? "").Trim();
            var j = string.IsNullOrWhiteSpace(jornadaDisplay) ? "MATUTINA" : jornadaDisplay.Trim();
            if (string.IsNullOrEmpty(det))
                return j.ToUpperInvariant();
            if (!string.IsNullOrEmpty(j) && det.IndexOf(j, StringComparison.OrdinalIgnoreCase) < 0)
                return $"{det} {j}".Trim();
            return det;
        }

        private async Task<EstudianteLogisticaResponse> BuildLogisticaFromSigafiAndPersistAsync(CentralStudentDto centralData)
        {
            // [JIT RESILIENCE] Ensure all catalog dependencies exist locally BEFORE any persistence or fallback calculations.
            await EnsureCatalogDependenciesExistAsync(centralData.idPeriodo, centralData.idNivel, centralData.idSeccion, centralData.idModalidad);

            var eBase = await _context.Estudiantes.FindAsync(centralData.idAlumno);
            if (eBase == null)
            {
                eBase = new Estudiante
                {
                    idAlumno = centralData.idAlumno,
                    primerNombre = (centralData.primerNombre ?? "S/N").ToUpper(),
                    segundoNombre = (centralData.segundoNombre ?? "").ToUpper(),
                    apellidoPaterno = (centralData.apellidoPaterno ?? "S/N").ToUpper(),
                    apellidoMaterno = (centralData.apellidoMaterno ?? "").ToUpper(),
                    idPeriodo = !string.IsNullOrEmpty(centralData.idPeriodo) && centralData.idPeriodo != "SIN_MAT"
                        ? centralData.idPeriodo
                        : null,
                    idNivel = centralData.idNivel > 0 ? centralData.idNivel : null,
                    idSeccion = centralData.idSeccion > 0 ? centralData.idSeccion : null,
                    idModalidad = centralData.idModalidad > 0 ? centralData.idModalidad : null
                };
                _context.Estudiantes.Add(eBase);
            }
            else
            {
                eBase.primerNombre = (centralData.primerNombre ?? eBase.primerNombre).ToUpper();
                eBase.segundoNombre = (centralData.segundoNombre ?? "").ToUpper();
                eBase.apellidoPaterno = (centralData.apellidoPaterno ?? eBase.apellidoPaterno).ToUpper();
                eBase.apellidoMaterno = (centralData.apellidoMaterno ?? "").ToUpper();
                if (!string.IsNullOrEmpty(centralData.idPeriodo) && centralData.idPeriodo != "SIN_MAT")
                    eBase.idPeriodo = centralData.idPeriodo;
                if (centralData.idNivel > 0)
                    eBase.idNivel = centralData.idNivel;
                if (centralData.idSeccion > 0)
                    eBase.idSeccion = centralData.idSeccion;
                if (centralData.idModalidad > 0)
                    eBase.idModalidad = centralData.idModalidad;
            }

            Curso? nivelLocal = null;
            if (centralData.idNivel > 0)
                nivelLocal = await _context.Cursos.FirstOrDefaultAsync(c => c.idNivel == centralData.idNivel);
            if (nivelLocal == null)
                nivelLocal = await _context.Cursos.OrderBy(c => c.idNivel).FirstOrDefaultAsync();

            var idNivelPersist = centralData.idNivel > 0 ? centralData.idNivel : (nivelLocal?.idNivel ?? 1);
            var idSeccionPersist = centralData.idSeccion > 0 ? centralData.idSeccion : 1;
            var idModalidadPersist = centralData.idModalidad > 0 ? centralData.idModalidad : 1;
            var jornadaEtiqueta = !string.IsNullOrWhiteSpace(centralData.seccion)
                ? centralData.seccion.Trim().ToUpperInvariant()
                : (!string.IsNullOrWhiteSpace(centralData.JornadaSigafi)
                    ? centralData.JornadaSigafi.Trim().ToUpperInvariant()
                    : "MATUTINA");

            Matricula matriculaUsada;
            if (centralData.idPeriodo == "SIN_MAT")
            {
                matriculaUsada = await _context.Matriculas
                    .Where(m => m.idAlumno == eBase.idAlumno)
                    .OrderByDescending(m => m.idMatricula)
                    .FirstOrDefaultAsync() ?? new Matricula();

                if (matriculaUsada.idMatricula == 0)
                {
                    matriculaUsada = new Matricula
                    {
                        idMatricula = centralData.idMatricula,
                        idAlumno = eBase.idAlumno,
                        idNivel = idNivelPersist,
                        idSeccion = idSeccionPersist,
                        idModalidad = idModalidadPersist,
                        idPeriodo = "SIN_MAT",
                        paralelo = centralData.paralelo ?? "A"
                    };
                    _context.Matriculas.Add(matriculaUsada);
                }
            }
            else
            {
                matriculaUsada = await _context.Matriculas
                                     .FirstOrDefaultAsync(m => m.idAlumno == eBase.idAlumno && m.idPeriodo == centralData.idPeriodo)
                                 ?? new Matricula();

                if (matriculaUsada.idMatricula == 0)
                {
                    matriculaUsada = new Matricula
                    {
                        idMatricula = centralData.idMatricula,
                        idAlumno = eBase.idAlumno,
                        idNivel = idNivelPersist,
                        idSeccion = idSeccionPersist,
                        idModalidad = idModalidadPersist,
                        idPeriodo = centralData.idPeriodo,
                        paralelo = centralData.paralelo ?? "A"
                    };
                    _context.Matriculas.Add(matriculaUsada);
                }
                else
                {
                    matriculaUsada.paralelo = centralData.paralelo ?? matriculaUsada.paralelo;
                    if (centralData.idNivel > 0)
                        matriculaUsada.idNivel = centralData.idNivel;
                    if (centralData.idSeccion > 0)
                        matriculaUsada.idSeccion = centralData.idSeccion;
                    if (centralData.idModalidad > 0)
                        matriculaUsada.idModalidad = centralData.idModalidad;
                }
            }

            await _context.SaveChangesAsync();

            var carreraNombre = centralData.CarreraNombre?.Trim();
            if (string.IsNullOrEmpty(carreraNombre) && nivelLocal != null && nivelLocal.idCarrera > 0)
            {
                carreraNombre = (await _context.Carreras.AsNoTracking()
                    .Where(c => c.idCarrera == nivelLocal.idCarrera)
                    .Select(c => c.NombreCarrera)
                    .FirstOrDefaultAsync())?.Trim();
            }

            var nivelSemestre = !string.IsNullOrWhiteSpace(centralData.NivelCurso)
                ? centralData.NivelCurso.Trim()
                : (nivelLocal?.Nivel ?? "S/N").Trim();
            if (string.IsNullOrEmpty(nivelSemestre))
                nivelSemestre = "S/N";

            var nivelDisplay = nivelSemestre.ToUpperInvariant();
            var carreraDisplay = string.IsNullOrEmpty(carreraNombre) ? string.Empty : carreraNombre.ToUpperInvariant();
            var detalleSigafi = ConstruirDetalleMatriculaSigafi(centralData.DetalleRaw, jornadaEtiqueta);
            var periodoMostrar = !string.IsNullOrEmpty(centralData.idPeriodo) && centralData.idPeriodo != "SIN_MAT"
                ? centralData.idPeriodo
                : matriculaUsada.idPeriodo;

            return new EstudianteLogisticaResponse
            {
                idAlumno = eBase.idAlumno,
                nombreCompleto = (centralData.NombreCompleto ?? $"{centralData.apellidoPaterno} {centralData.apellidoMaterno} {centralData.primerNombre} {centralData.segundoNombre}").Trim().ToUpper(),
                carrera = carreraDisplay,
                nivel = nivelDisplay,
                detalleMatriculaSigafi = detalleSigafi,
                paralelo = matriculaUsada.paralelo ?? "A",
                jornada = jornadaEtiqueta,
                idPeriodo = periodoMostrar,
                idMatricula = matriculaUsada.idMatricula,
                fotoBase64 = centralData.FotoBase64,
                isBusy = false
            };
        }

        /// <summary>
        /// JIT (Just-In-Time) Synchronization of Catalog Dependencies.
        /// Checks if the required foreign keys exist locally; if not, fetches the catalog from SIGAFI.
        /// </summary>
        private async Task EnsureCatalogDependenciesExistAsync(string idPeriodo, int idNivel, int idSeccion, int idModalidad)
        {
            // 1. Periodo
            if (!string.IsNullOrEmpty(idPeriodo) && idPeriodo != "SIN_MAT")
            {
                var exists = await _context.Periodos.AnyAsync(p => p.idPeriodo == idPeriodo);
                if (!exists)
                {
                    _logger.LogInformation("[JIT-SYNC] Periodo {id} no hallado. Sincronizando catálogo completo...", idPeriodo);
                    var items = await _centralProvider.GetAllPeriodosFromCentralAsync();
                    foreach (var i in items)
                    {
                        if (!await _context.Periodos.AnyAsync(p => p.idPeriodo == i.idPeriodo))
                        {
                            _context.Periodos.Add(new Periodo { 
                                idPeriodo = i.idPeriodo, 
                                detalle = i.detalle, 
                                fecha_inicial = i.fecha_inicial, 
                                fecha_final = i.fecha_final, 
                                activo = i.activo == 1 
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // 2. Nivel (Curso)
            if (idNivel > 0)
            {
                var exists = await _context.Cursos.AnyAsync(c => c.idNivel == idNivel);
                if (!exists)
                {
                    _logger.LogInformation("[JIT-SYNC] Nivel {id} no hallado. Sincronizando catálogo completo...", idNivel);
                    var items = await _centralProvider.GetAllCoursesFromCentralAsync();
                    foreach (var i in items)
                    {
                        if (!await _context.Cursos.AnyAsync(c => c.idNivel == i.idNivel))
                        {
                            _context.Cursos.Add(new Curso { 
                                idNivel = i.idNivel, 
                                idCarrera = i.idCarrera, 
                                Nivel = i.Nivel, 
                                jerarquia = i.jerarquia ?? 1 
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // 3. Seccion
            if (idSeccion > 0)
            {
                var exists = await _context.Secciones.AnyAsync(s => s.idSeccion == idSeccion);
                if (!exists)
                {
                    _logger.LogInformation("[JIT-SYNC] Sección {id} no hallada. Sincronizando catálogo completo...", idSeccion);
                    var items = await _centralProvider.GetAllSeccionesFromCentralAsync();
                    foreach (var i in items)
                    {
                        if (!await _context.Secciones.AnyAsync(s => s.idSeccion == i.idSeccion))
                        {
                            _context.Secciones.Add(new Seccion { idSeccion = i.idSeccion, seccion = i.seccion });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // 4. Modalidad
            if (idModalidad > 0)
            {
                var exists = await _context.Modalidades.AnyAsync(m => m.idModalidad == idModalidad);
                if (!exists)
                {
                    _logger.LogInformation("[JIT-SYNC] Modalidad {id} no hallada. Sincronizando catálogo completo...", idModalidad);
                    var items = await _centralProvider.GetAllModalidadesFromCentralAsync();
                    foreach (var i in items)
                    {
                        if (!await _context.Modalidades.AnyAsync(m => m.idModalidad == i.idModalidad))
                        {
                            _context.Modalidades.Add(new Modalidad { 
                                idModalidad = i.idModalidad, 
                                modalidad = i.modalidad 
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task EnrichEstudianteLogisticaDesdeSigafiAsync(EstudianteLogisticaResponse student)
        {
            var scheduled = await _centralProvider.GetScheduledPracticeAsync(student.idAlumno);
            if (scheduled != null)
                await _mirrorPersist.PersistPracticesFromScheduleDtosAsync(new[] { scheduled });

            CentralInstructorDto? tutor = null;
            if (scheduled == null)
                tutor = await _centralProvider.GetAssignedTutorAsync(student.idAlumno);

            string? profCedula = (scheduled?.idProfesor ?? tutor?.idProfesor)?.Trim();
            if (string.IsNullOrEmpty(profCedula))
                return;

            var localProf = await _context.Instructores.FirstOrDefaultAsync(i => i.idProfesor == profCedula);
            if (localProf == null)
            {
                var cp = scheduled != null
                    ? new CentralInstructorDto { idProfesor = scheduled.idProfesor, nombres = scheduled.ProfesorNombre, apellidos = "" }
                    : tutor;
                if (cp != null)
                {
                    localProf = new Instructor
                    {
                        idProfesor = cp.idProfesor,
                        primerNombre = (cp.primerNombre ?? cp.nombres ?? "S/N").ToUpper(),
                        primerApellido = (cp.primerApellido ?? cp.apellidos ?? "S/N").ToUpper(),
                        nombres = (cp.nombres ?? "").ToUpper(),
                        apellidos = (cp.apellidos ?? "").ToUpper(),
                        activo = 1
                    };
                    _context.Instructores.Add(localProf);
                    await _context.SaveChangesAsync();
                }
            }

            student.practicaInstructor = localProf != null
                ? $"{localProf.apellidos} {localProf.nombres}".Trim()
                : (scheduled?.ProfesorNombre ?? $"{tutor?.apellidos} {tutor?.nombres}");
            student.idPracticaInstructor = profCedula;
            student.idPracticaCentral = scheduled?.idvehiculo;
            student.practicaVehiculo = scheduled?.VehiculoDetalle;
            student.practicaHora = scheduled?.hora_salida?.ToString(@"hh\:mm");

            var nextSched = await _centralProvider.GetNextScheduleAsync(student.idAlumno);
            if (nextSched != null)
            {
                student.horarioProximo = nextSched.observacion ?? string.Empty;
                student.asistenciaHoy = nextSched.asiste == 1;
            }
        }

        [HttpGet("vehiculos-disponibles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VehiculoLogisticaResponse>>>> GetVehiculosDisponibles()
        {
            var centralVehicles = await _centralProvider.GetAllVehiclesFromCentralAsync();
            await SigafiVehicleUpsert.MergeFromCentralAsync(_context, centralVehicles);

            var rawList = await GetVehiculosOperativosLocalesAsync();

            var query = rawList.Select(v => new VehiculoLogisticaResponse {
                idVehiculo = v.idVehiculo,
                numeroVehiculo = v.numero_vehiculo,
                vehiculoStr = $"#{v.numero_vehiculo} ({v.placa})",
                instructorNombre = "DOCENTE ASIGNADO",
                idInstructorFijo = v.idInstructorFijo,
                idTipoLicencia = v.idTipoLicencia,
                estadoMecanico = v.estadoMecanico
            });

            return Ok(ApiResponse<IEnumerable<VehiculoLogisticaResponse>>.Ok(query));
        }

        private async Task<List<VehiculoLite>> GetVehiculosOperativosLocalesAsync()
        {
            var list = await (from v in _context.Vehiculos
                          join vo in _context.VehiculosOperaciones on v.idVehiculo equals vo.idVehiculo into voJoin
                          from vo in voJoin.DefaultIfEmpty()
                          where v.activo == 1 
                          && !_context.Practicas.Any(p => p.idvehiculo == v.idVehiculo && p.ensalida == 1 && (p.cancelado ?? 0) == 0)
                          select new VehiculoLite
                          {
                              idVehiculo = v.idVehiculo,
                              numero_vehiculo = v.numero_vehiculo ?? "0",
                              placa = v.placa,
                              idInstructorFijo = vo != null ? vo.id_instructor_fijo : null,
                              idTipoLicencia = vo != null && vo.id_tipo_licencia.HasValue ? vo.id_tipo_licencia.Value : 0,
                              estadoMecanico = vo != null ? vo.estado_mecanico : "OPERATIVO"
                          }).ToListAsync();

            // Sugerencia inteligente de Instructor Fijo desde SIGAFI (si local está vacío)
            foreach (var v in list.Where(x => string.IsNullOrEmpty(x.idInstructorFijo)))
            {
                var lastAsign = await _context.AsignacionesInstructores
                    .Where(a => a.idVehiculo == v.idVehiculo && a.activo)
                    .OrderByDescending(a => a.idAsignacion)
                    .Select(a => a.idProfesor)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(lastAsign))
                {
                    v.idInstructorFijo = lastAsign;
                }
            }

            return list;
        }

        [HttpGet("instructores")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InstructorLogisticaResponse>>>> GetInstructoresCatalogo()
        {
            var list = await _centralProvider.GetAllInstructorsFromCentralAsync();
            var dto = list
                .Where(i => i.activo == 1)
                .Select(i => new InstructorLogisticaResponse
                {
                    idInstructor = i.idProfesor,
                    fullName = (!string.IsNullOrWhiteSpace(i.apellidos) || !string.IsNullOrWhiteSpace(i.nombres))
                        ? $"{i.apellidos} {i.nombres}".Trim().ToUpper()
                        : $"{i.primerApellido} {i.segundoApellido} {i.primerNombre} {i.segundoNombre}".Trim().ToUpper()
                })
                .OrderBy(x => x.fullName)
                .ToList();

            return Ok(ApiResponse<IEnumerable<InstructorLogisticaResponse>>.Ok(dto, "Catálogo desde SIGAFI."));
        }

        [HttpGet("instructor/{idProfesor}")]
        public async Task<ActionResult<ApiResponse<InstructorLogisticaResponse>>> BuscarInstructor(string idProfesor)
        {
            idProfesor = idProfesor.Trim();
            var centralData = await _centralProvider.GetInstructorFromCentralAsync(idProfesor);
            if (centralData == null)
            {
                var localOnly = await _context.Instructores
                    .Where(i => i.idProfesor == idProfesor && i.activo == 1)
                    .Select(i => new InstructorLogisticaResponse
                    {
                        idInstructor = i.idProfesor,
                        fullName = $"{i.apellidos} {i.nombres}".Trim().ToUpper()
                    })
                    .FirstOrDefaultAsync();
                if (localOnly != null)
                    return Ok(ApiResponse<InstructorLogisticaResponse>.Ok(localOnly, "Instructor solo en espejo local (no hallado en SIGAFI)."));
                return NotFound(ApiResponse<InstructorLogisticaResponse>.Fail("No hallado en SIGAFI."));
            }

            var existing = await _context.Instructores.FirstOrDefaultAsync(i => i.idProfesor == centralData.idProfesor);
            if (existing == null)
            {
                existing = new Instructor
                {
                    idProfesor = centralData.idProfesor,
                    primerNombre = (centralData.primerNombre ?? "S/N").ToUpper(),
                    primerApellido = (centralData.primerApellido ?? "S/N").ToUpper(),
                    nombres = (centralData.nombres ?? "").ToUpper(),
                    apellidos = (centralData.apellidos ?? "").ToUpper(),
                    activo = centralData.activo
                };
                _context.Instructores.Add(existing);
            }
            else
            {
                existing.primerNombre = (centralData.primerNombre ?? existing.primerNombre ?? "S/N").ToUpper();
                existing.primerApellido = (centralData.primerApellido ?? existing.primerApellido ?? "S/N").ToUpper();
                existing.nombres = (centralData.nombres ?? existing.nombres ?? "").ToUpper();
                existing.apellidos = (centralData.apellidos ?? existing.apellidos ?? "").ToUpper();
                existing.activo = centralData.activo;
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<InstructorLogisticaResponse>.Ok(new InstructorLogisticaResponse
            {
                idInstructor = existing.idProfesor,
                fullName = $"{existing.apellidos} {existing.nombres}".Trim().ToUpper()
            }, "Datos vigentes desde SIGAFI."));
        }

        [HttpPost("salida")]
        public async Task<ActionResult<ApiResponse<string>>> RegistrarSalida([FromBody] SalidaRequest req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _logisticaService.RegistrarSalidaAsync(
                req.idMatricula, req.idVehiculo, req.idInstructor,
                req.registradoPor, req.idAsignacionHorario);

            if (result == "EXITO")
            {
                await _audit.LogAsync(
                    req.registradoPor.ToString(), "SALIDA",
                    entidadId: $"mat:{req.idMatricula}/veh:{req.idVehiculo}",
                    detalles: $"Instructor: {req.idInstructor}.",
                    ipOrigen: ip);
                return Ok(ApiResponse<string>.Ok(result, "Salida registrada con éxito."));
            }

            return BadRequest(ApiResponse<string>.Fail($"Error: {result}"));
        }

        [HttpPost("llegada")]
        public async Task<ActionResult<ApiResponse<string>>> RegistrarLlegada([FromBody] LlegadaRequest req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _logisticaService.RegistrarLlegadaAsync(
                req.idPractica, req.registradoPor);

            if (result == "EXITO")
            {
                await _audit.LogAsync(
                    req.registradoPor.ToString(), "LLEGADA",
                    entidadId: $"practica:{req.idPractica}",
                    detalles: $"Salió: OK.",
                    ipOrigen: ip);
                return Ok(ApiResponse<string>.Ok(result, "Llegada registrada con éxito."));
            }

            return BadRequest(ApiResponse<string>.Fail($"Alerta: {result}"));
        }

        [HttpGet("buscar")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AlumnoSugerenciaLogisticaDto>>>> BuscarSugerencias([FromQuery] string query)
        {
            var q = (query ?? string.Empty).Trim();
            if (q.Length < 3)
                return Ok(ApiResponse<IEnumerable<AlumnoSugerenciaLogisticaDto>>.Ok(Array.Empty<AlumnoSugerenciaLogisticaDto>()));

            // Buscar en local y en SIGAFI en paralelo para no penalizar al usuario.
            // SIGAFI es la fuente de verdad: sus resultados se agregan aunque el alumno
            // aún no exista en el espejo local (p. ej. recién matriculado hoy).
            var localTask = _context.Estudiantes.AsNoTracking()
                .Where(e => (
                    e.idAlumno.StartsWith(q)
                    || (e.primerNombre   != null && e.primerNombre.Contains(q))
                    || (e.segundoNombre  != null && e.segundoNombre.Contains(q))
                    || (e.apellidoPaterno != null && e.apellidoPaterno.Contains(q))
                    || (e.apellidoMaterno != null && e.apellidoMaterno.Contains(q))))
                .OrderBy(e => e.idAlumno)
                .Take(15)
                .Select(e => new AlumnoSugerenciaLogisticaDto
                {
                    idAlumno      = e.idAlumno,
                    nombreCompleto = $"{e.apellidoPaterno} {e.apellidoMaterno} {e.primerNombre} {e.segundoNombre}".Trim(),
                    esAgendado = false,
                    isBusy     = false
                })
                .ToListAsync();

            var sigafiTask = _centralProvider.SearchStudentsFromCentralAsync(q);

            await Task.WhenAll(localTask, sigafiTask);

            var list = localTask.Result;
            var sigafiRaw = sigafiTask.Result;

            await _mirrorPersist.PersistEstudiantesFromLitesAsync(sigafiRaw);

            // Fusionar: agregar los que SIGAFI devolvió pero que no están aún en el espejo local.
            var knownIds = list.Select(x => x.idAlumno).ToHashSet(StringComparer.Ordinal);
            foreach (var s in sigafiRaw)
            {
                if (knownIds.Contains(s.idAlumno))
                    continue;
                list.Add(new AlumnoSugerenciaLogisticaDto
                {
                    idAlumno      = s.idAlumno,
                    nombreCompleto = $"{s.apellidoPaterno} {s.apellidoMaterno} {s.primerNombre} {s.segundoNombre}".Trim(),
                    esAgendado = false,
                    isBusy     = false
                });
                knownIds.Add(s.idAlumno);
            }

            if (list.Count > 0)
            {
                try
                {
                    var porSigafi = await _centralProvider.GetNextOpenPracticesForAlumnosAsync(list.Select(x => x.idAlumno));
                    if (porSigafi.Count > 0)
                        await _mirrorPersist.PersistPracticesFromScheduleDtosAsync(porSigafi.Values);

                    foreach (var item in list)
                    {
                        if (!porSigafi.TryGetValue(item.idAlumno, out var pr))
                            continue;
                        item.esAgendado      = true;
                        item.horaAgenda      = pr.hora_salida.HasValue
                            ? pr.hora_salida.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture)
                            : null;
                        item.vehiculoAgenda  = string.IsNullOrWhiteSpace(pr.VehiculoDetalle) ? null : pr.VehiculoDetalle;
                        item.instructorAgenda = string.IsNullOrWhiteSpace(pr.ProfesorNombre) ? null : pr.ProfesorNombre;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SIGAFI no enriqueció sugerencias de alumnos (se devuelve lista sin práctica central).");
                }

                var ids = list.Select(x => x.idAlumno).ToList();
                var busyIds = await _context.Practicas.AsNoTracking()
                    .Where(p => p.ensalida == 1 && (p.cancelado ?? 0) == 0 && ids.Contains(p.idalumno))
                    .Select(p => p.idalumno)
                    .Distinct()
                    .ToListAsync();
                var busy = busyIds.ToHashSet();
                foreach (var item in list)
                    item.isBusy = busy.Contains(item.idAlumno);
            }

            return Ok(ApiResponse<IEnumerable<AlumnoSugerenciaLogisticaDto>>.Ok(list));
        }
    }
}
