using backend.Data;
using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        private readonly AppDbContext _context;
        private readonly ILogisticaService _logisticaService;
        private readonly ICentralStudentProvider _centralProvider;

        public LogisticaController(AppDbContext context, ILogisticaService logisticaService, ICentralStudentProvider centralProvider)
        {
            _context = context;
            _logisticaService = logisticaService;
            _centralProvider = centralProvider;
        }

        [HttpGet("estudiante/{idAlumno}")]
        public async Task<ActionResult<ApiResponse<EstudianteLogisticaResponse>>> BuscarEstudiante(string idAlumno)
        {
            idAlumno = idAlumno.Trim();

            // 1. LOCAL SEARCH
            var localStudent = await (from m in _context.Matriculas
                                join e in _context.Estudiantes on m.idAlumno equals e.idAlumno
                                join c in _context.Cursos on m.idNivel equals c.idNivel
                                where e.idAlumno == idAlumno && m.estado == "ACTIVO"
                                select new EstudianteLogisticaResponse
                                {
                                    idAlumno = e.idAlumno,
                                    nombreCompleto = $"{e.apellidoPaterno} {e.apellidoMaterno} {e.primerNombre} {e.segundoNombre}".Trim(),
                                    nivel = (c.Nivel ?? "S/N").ToUpper(),
                                    paralelo = m.paralelo ?? "A",
                                    jornada = "MATUTINA",
                                    idPeriodo = m.idPeriodo ?? "S/P",
                                    idMatricula = m.idMatricula
                                }).FirstOrDefaultAsync();

            if (localStudent != null)
            {
                var scheduled = await _centralProvider.GetScheduledPracticeAsync(localStudent.idAlumno);
                CentralInstructorDto? tutor = null;
                if (scheduled == null)
                    tutor = await _centralProvider.GetAssignedTutorAsync(localStudent.idAlumno);

                string? profCedula = (scheduled?.idProfesor ?? tutor?.idProfesor)?.Trim();
                if (!string.IsNullOrEmpty(profCedula))
                {
                    var localProf = await _context.Instructores.FirstOrDefaultAsync(i => i.idProfesor == profCedula);
                    if (localProf == null)
                    {
                        var cp = scheduled != null ? new CentralInstructorDto { idProfesor = scheduled.idProfesor, nombres = scheduled.ProfesorNombre, apellidos = "" } : tutor;
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

                    localStudent.practicaInstructor = localProf != null ? $"{localProf.apellidos} {localProf.nombres}" : (scheduled?.ProfesorNombre ?? $"{tutor?.apellidos} {tutor?.nombres}");
                    localStudent.idPracticaCentral = scheduled?.idvehiculo;
                    localStudent.practicaVehiculo = scheduled?.VehiculoDetalle;
                    localStudent.practicaHora = scheduled?.hora_salida?.ToString(@"hh\:mm");
                }

                localStudent.isBusy = await _context.Practicas
                    .AnyAsync(p => p.idalumno == localStudent.idAlumno && p.ensalida == 1 && p.cancelado == 0);

                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(localStudent, "Alumno localizado (Local)."));
            }

            // 2. REMOTE SEARCH & AUTO-SYNC
            var centralData = await _centralProvider.GetFromCentralAsync(idAlumno);
            if (centralData == null) return NotFound(ApiResponse<EstudianteLogisticaResponse>.Fail("Estudiante no registrado en SIGAFI."));

            try
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
                        apellidoMaterno = (centralData.apellidoMaterno ?? "").ToUpper()
                    };
                    _context.Estudiantes.Add(eBase);
                }

                var nivelLocal = await _context.Cursos.FirstOrDefaultAsync()
                                  ?? new Curso { idNivel = 1, Nivel = centralData.Nivel };

                var nuevaMatricula = new Matricula
                {
                    idAlumno = eBase.idAlumno,
                    idNivel = nivelLocal.idNivel,
                    idSeccion = 1,
                    idModalidad = 1,
                    idPeriodo = centralData.idPeriodo,
                    paralelo = centralData.paralelo ?? "A",
                    estado = "ACTIVO"
                };
                _context.Matriculas.Add(nuevaMatricula);
                await _context.SaveChangesAsync();

                var scheduledResult = await _centralProvider.GetScheduledPracticeAsync(eBase.idAlumno);
                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(new EstudianteLogisticaResponse
                {
                    idAlumno = eBase.idAlumno,
                    nombreCompleto = centralData.NombreCompleto ?? $"{centralData.apellidoPaterno} {centralData.apellidoMaterno} {centralData.primerNombre} {centralData.segundoNombre}".ToUpper(),
                    nivel = (nivelLocal.Nivel ?? "S/N").ToUpper(),
                    paralelo = nuevaMatricula.paralelo,
                    jornada = "MATUTINA",
                    idPeriodo = nuevaMatricula.idPeriodo,
                    idMatricula = nuevaMatricula.idMatricula,
                    fotoBase64 = centralData.FotoBase64,
                    idPracticaCentral = scheduledResult?.idvehiculo,
                    practicaVehiculo = scheduledResult?.VehiculoDetalle,
                    practicaHora = scheduledResult?.hora_salida?.ToString(@"hh\:mm"),
                    isBusy = false
                }, "Sincronizado desde SIGAFI."));
            }
            catch (System.Exception ex) { return StatusCode(500, ApiResponse<EstudianteLogisticaResponse>.Fail($"Error Sync: {ex.Message}")); }
        }

        [HttpGet("vehiculos-disponibles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VehiculoLogisticaResponse>>>> GetVehiculosDisponibles()
        {
            var rawList = await (from v in _context.Vehiculos
                               where v.activo && v.estado_mecanico == "OPERATIVO"
                               && !_context.Practicas.Any(p => p.idvehiculo == v.idVehiculo && p.ensalida == 1 && p.cancelado == 0)
                               select new {
                                   v.idVehiculo,
                                   v.numero_vehiculo,
                                   v.placa
                               }).ToListAsync();

            var query = rawList.Select(v => new VehiculoLogisticaResponse {
                idVehiculo = v.idVehiculo,
                numeroVehiculo = int.TryParse(v.numero_vehiculo, out int n) ? n : 0,
                vehiculoStr = $"#{v.numero_vehiculo} ({v.placa})",
                instructorNombre = "DOCENTE ASIGNADO"
            });

            return Ok(ApiResponse<IEnumerable<VehiculoLogisticaResponse>>.Ok(query));
        }

        [HttpGet("instructor/{idProfesor}")]
        public async Task<ActionResult<ApiResponse<InstructorLogisticaResponse>>> BuscarInstructor(string idProfesor)
        {
            var localInstr = await _context.Instructores
                .Where(i => i.idProfesor == idProfesor && i.activo)
                .Select(i => new InstructorLogisticaResponse
                {
                    idInstructor = i.idProfesor,
                    fullName = $"{i.apellidos} {i.nombres}".Trim().ToUpper()
                })
                .FirstOrDefaultAsync();

            if (localInstr != null) return Ok(ApiResponse<InstructorLogisticaResponse>.Ok(localInstr, "Instructor localizado (Local)."));

            var centralData = await _centralProvider.GetInstructorFromCentralAsync(idProfesor);
            if (centralData == null) return NotFound(ApiResponse<InstructorLogisticaResponse>.Fail("No hallado en SIGAFI."));

            var nuevoInstr = new Instructor
            {
                idProfesor = centralData.idProfesor,
                primerNombre = (centralData.primerNombre ?? "S/N").ToUpper(),
                primerApellido = (centralData.primerApellido ?? "S/N").ToUpper(),
                nombres = (centralData.nombres ?? "").ToUpper(),
                apellidos = (centralData.apellidos ?? "").ToUpper(),
                activo = true
            };
            _context.Instructores.Add(nuevoInstr);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<InstructorLogisticaResponse>.Ok(new InstructorLogisticaResponse
            {
                idInstructor = nuevoInstr.idProfesor,
                fullName = $"{nuevoInstr.apellidos} {nuevoInstr.nombres}".Trim().ToUpper()
            }, "Sincronizado desde SIGAFI."));
        }

        [HttpPost("salida")]
        public async Task<ActionResult<ApiResponse<string>>> RegistrarSalida([FromBody] SalidaRequest req)
        {
            // Note: Service expects integer IDs for local tracking, conversion handled inside service
            var result = await _logisticaService.RegistrarSalidaAsync(req.idMatricula, req.idVehiculo, req.idInstructor, req.observaciones ?? "Ninguna", req.registradoPor);
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

        [HttpGet("agendados-hoy")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ScheduledPracticeDto>>>> GetAgendadosHoy()
        {
            var data = await _centralProvider.GetSchedulesForTodayAsync();
            return Ok(ApiResponse<IEnumerable<ScheduledPracticeDto>>.Ok(data));
        }
    }
}
