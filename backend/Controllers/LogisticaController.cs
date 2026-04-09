using backend.Data;
using backend.DTOs;
using backend.Services.Helpers;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
        }

        private readonly AppDbContext _context;
        private readonly ILogisticaService _logisticaService;
        private readonly ICentralStudentProvider _centralProvider;
        private readonly ILogger<LogisticaController> _logger;

        public LogisticaController(
            AppDbContext context,
            ILogisticaService logisticaService,
            ICentralStudentProvider centralProvider,
            ILogger<LogisticaController> logger)
        {
            _context = context;
            _logisticaService = logisticaService;
            _centralProvider = centralProvider;
            _logger = logger;
        }

        [HttpGet("estudiante/{idAlumno}")]
        public async Task<ActionResult<ApiResponse<EstudianteLogisticaResponse>>> BuscarEstudiante(
            string idAlumno,
            [FromQuery] int? idVehiculoAgenda = null,
            [FromQuery] string? idProfesorAgenda = null,
            [FromQuery] int? idPracticaAgenda = null)
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
                await AplicarContextoFilaAgendaAsync(fromCentral, idVehiculoAgenda, idProfesorAgenda, idPracticaAgenda);
                fromCentral.isBusy = await _context.Practicas
                    .AnyAsync(p => p.idalumno == fromCentral.idAlumno && p.ensalida == 1 && p.cancelado == 0);
                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(fromCentral, "Datos vigentes desde SIGAFI."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<EstudianteLogisticaResponse>.Fail($"Error al materializar alumno desde SIGAFI: {ex.Message}"));
            }
        }

        private async Task AplicarContextoFilaAgendaAsync(
            EstudianteLogisticaResponse student,
            int? idVehiculoAgenda,
            string? idProfesorAgenda,
            int? idPracticaAgenda)
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

            if (idPracticaAgenda is > 0)
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
                    .Where(m => m.idAlumno == eBase.idAlumno && m.estado == "ACTIVO")
                    .OrderByDescending(m => m.idMatricula)
                    .FirstOrDefaultAsync() ?? new Matricula();

                if (matriculaUsada.idMatricula == 0)
                {
                    matriculaUsada = new Matricula
                    {
                        idAlumno = eBase.idAlumno,
                        idNivel = idNivelPersist,
                        idSeccion = idSeccionPersist,
                        idModalidad = idModalidadPersist,
                        idPeriodo = "SIN_MAT",
                        paralelo = centralData.paralelo ?? "A",
                        estado = "ACTIVO"
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
                        idAlumno = eBase.idAlumno,
                        idNivel = idNivelPersist,
                        idSeccion = idSeccionPersist,
                        idModalidad = idModalidadPersist,
                        idPeriodo = centralData.idPeriodo,
                        paralelo = centralData.paralelo ?? "A",
                        estado = "ACTIVO"
                    };
                    _context.Matriculas.Add(matriculaUsada);
                }
                else
                {
                    matriculaUsada.paralelo = centralData.paralelo ?? matriculaUsada.paralelo;
                    matriculaUsada.estado = "ACTIVO";
                    if (centralData.idNivel > 0)
                        matriculaUsada.idNivel = centralData.idNivel;
                    if (centralData.idSeccion > 0)
                        matriculaUsada.idSeccion = centralData.idSeccion;
                    if (centralData.idModalidad > 0)
                        matriculaUsada.idModalidad = centralData.idModalidad;
                }
            }

            await _context.SaveChangesAsync();

            var nivelDisplay = (centralData.Nivel ?? nivelLocal?.Nivel ?? "S/N").ToUpper();
            var detalleSigafi = ConstruirDetalleMatriculaSigafi(centralData.DetalleRaw, jornadaEtiqueta);
            var periodoMostrar = !string.IsNullOrEmpty(centralData.idPeriodo) && centralData.idPeriodo != "SIN_MAT"
                ? centralData.idPeriodo
                : matriculaUsada.idPeriodo;

            return new EstudianteLogisticaResponse
            {
                idAlumno = eBase.idAlumno,
                nombreCompleto = (centralData.NombreCompleto ?? $"{centralData.apellidoPaterno} {centralData.apellidoMaterno} {centralData.primerNombre} {centralData.segundoNombre}").Trim().ToUpper(),
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

        private async Task EnrichEstudianteLogisticaDesdeSigafiAsync(EstudianteLogisticaResponse student)
        {
            var scheduled = await _centralProvider.GetScheduledPracticeAsync(student.idAlumno);
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
                        primerNombre = (cp.primerNombre ?? cp.nombres).ToUpper(),
                        primerApellido = (cp.primerApellido ?? cp.apellidos).ToUpper(),
                        nombres = (cp.nombres ?? "").ToUpper(),
                        apellidos = (cp.apellidos ?? "").ToUpper(),
                        activo = true
                    };
                    _context.Instructores.Add(localProf);
                    await _context.SaveChangesAsync();
                }
            }

            student.practicaInstructor = localProf != null
                ? $"{localProf.apellidos} {localProf.nombres}"
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
                numeroVehiculo = int.TryParse(v.numero_vehiculo, out int n) ? n : 0,
                vehiculoStr = $"#{v.numero_vehiculo} ({v.placa})",
                instructorNombre = "DOCENTE ASIGNADO"
            });

            return Ok(ApiResponse<IEnumerable<VehiculoLogisticaResponse>>.Ok(query));
        }

        private async Task<List<VehiculoLite>> GetVehiculosOperativosLocalesAsync()
        {
            return await (from v in _context.Vehiculos
                          where v.activo && v.estado_mecanico == "OPERATIVO"
                          && !_context.Practicas.Any(p => p.idvehiculo == v.idVehiculo && p.ensalida == 1 && p.cancelado == 0)
                          select new VehiculoLite
                          {
                              idVehiculo = v.idVehiculo,
                              numero_vehiculo = v.numero_vehiculo,
                              placa = v.placa
                          }).ToListAsync();
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
                    .Where(i => i.idProfesor == idProfesor && i.activo)
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
                    activo = centralData.activo == 1
                };
                _context.Instructores.Add(existing);
            }
            else
            {
                existing.primerNombre = (centralData.primerNombre ?? existing.primerNombre).ToUpper();
                existing.primerApellido = (centralData.primerApellido ?? existing.primerApellido).ToUpper();
                existing.nombres = (centralData.nombres ?? existing.nombres).ToUpper();
                existing.apellidos = (centralData.apellidos ?? existing.apellidos).ToUpper();
                existing.activo = centralData.activo == 1;
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
            // Note: Service expects integer IDs for local tracking, conversion handled inside service
            var result = await _logisticaService.RegistrarSalidaAsync(req.idMatricula, req.idVehiculo, req.idInstructor, req.observaciones ?? "Ninguna", req.registradoPor, req.idAsignacionHorario);
            if (result == "EXITO") return Ok(ApiResponse<string>.Ok(result, "Salida registrada con éxito."));
            return BadRequest(ApiResponse<string>.Fail($"Error: {result}"));
        }

        [HttpPost("llegada")]
        public async Task<ActionResult<ApiResponse<string>>> RegistrarLlegada([FromBody] LlegadaRequest req)
        {
            var result = await _logisticaService.RegistrarLlegadaAsync(req.idPractica, req.observaciones ?? "Ninguna", req.registradoPor);
            if (result == "EXITO") return Ok(ApiResponse<string>.Ok(result, "Llegada registrada con éxito."));
            return BadRequest(ApiResponse<string>.Fail($"Alerta: {result}"));
        }

        [HttpGet("buscar")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AlumnoSugerenciaLogisticaDto>>>> BuscarSugerencias([FromQuery] string query)
        {
            var q = (query ?? string.Empty).Trim();
            if (q.Length < 3)
                return Ok(ApiResponse<IEnumerable<AlumnoSugerenciaLogisticaDto>>.Ok(Array.Empty<AlumnoSugerenciaLogisticaDto>()));

            var list = await _context.Estudiantes.AsNoTracking()
                .Where(e => e.activo && (
                    e.idAlumno.StartsWith(q)
                    || (e.primerNombre != null && e.primerNombre.Contains(q))
                    || (e.segundoNombre != null && e.segundoNombre.Contains(q))
                    || (e.apellidoPaterno != null && e.apellidoPaterno.Contains(q))
                    || (e.apellidoMaterno != null && e.apellidoMaterno.Contains(q))))
                .OrderBy(e => e.idAlumno)
                .Take(15)
                .Select(e => new AlumnoSugerenciaLogisticaDto
                {
                    idAlumno = e.idAlumno,
                    nombreCompleto = $"{e.apellidoPaterno} {e.apellidoMaterno} {e.primerNombre} {e.segundoNombre}".Trim(),
                    esAgendado = false,
                    isBusy = false
                })
                .ToListAsync();

            if (list.Count > 0)
            {
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
