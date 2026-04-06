using backend.Data;
using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogisticaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogisticaService _logisticaService;
        private readonly IExternalStudentProvider _externalProvider;

        public LogisticaController(AppDbContext context, ILogisticaService logisticaService, IExternalStudentProvider externalProvider)
        {
            _context = context;
            _logisticaService = logisticaService;
            _externalProvider = externalProvider;
        }

        [HttpGet("estudiante/{cedula}")]
        public async Task<ActionResult<ApiResponse<EstudianteLogisticaResponse>>> BuscarEstudiante(string cedula)
        {
            // 1. INTENTO LOCAL (MySQL)
            var localStudent = await (from m in _context.Matriculas
                               join e in _context.Estudiantes on m.CedulaEstudiante equals e.Cedula
                               join c in _context.Cursos on m.IdCurso equals c.Id_Curso
                               join tl in _context.TipoLicencias on c.IdTipoLicencia equals tl.Id_Tipo
                               where e.Cedula == cedula && m.Estado == "ACTIVO"
                               select new EstudianteLogisticaResponse
                               {
                                   Cedula = e.Cedula,
                                   EstudianteNombre = $"{e.Apellidos} {e.Nombres}".ToUpper(),
                                   CursoDetalle = $"{c.Nombre} {c.Nivel}".ToUpper(),
                                   Paralelo = c.Paralelo.ToUpper(),
                                   Jornada = c.Jornada.ToString().ToUpper(),
                                   TipoLicencia = tl.Codigo.ToUpper(),
                                   Periodo = c.Periodo.ToUpper(),
                                   IdMatricula = m.Id_Matricula
                               }).FirstOrDefaultAsync();

            if (localStudent != null)
            {
                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(localStudent, "Local: Alumno encontrado."));
            }

            // 2. INTENTO EXTERNO (Simulación / Sistema Central)
            var external = await _externalProvider.GetByCedulaAsync(cedula);
            if (external == null)
            {
                return NotFound(ApiResponse<EstudianteLogisticaResponse>.Fail("Estudiante no registrado en ningún sistema conocido."));
            }

            // 3. AUTO-REGISTRO SMART SYNC
            try 
            {
                // Verificar si el estudiante base existe (pudo estar inactivo o sin matrícula)
                var eBase = await _context.Estudiantes.FindAsync(external.Cedula);
                if (eBase == null)
                {
                    eBase = new backend.Models.Estudiante 
                    { 
                        Cedula = external.Cedula, 
                        Nombres = external.Nombres, 
                        Apellidos = external.Apellidos 
                    };
                    _context.Estudiantes.Add(eBase);
                }

                // Buscar un curso local que coincida con el tipo de licencia sugerido por el externo
                var cursoLocal = await _context.Cursos.FirstOrDefaultAsync(c => c.IdTipoLicencia == external.IdTipoLicencia && c.Estado == "ACTIVO")
                                 ?? await _context.Cursos.FirstOrDefaultAsync(c => c.Estado == "ACTIVO");

                if (cursoLocal == null) return BadRequest(ApiResponse<EstudianteLogisticaResponse>.Fail("Externo: No hay cursos locales disponibles para automatizar el ingreso."));

                // Crear Matricula Automática
                var nuevaMatricula = new backend.Models.Matricula
                {
                    CedulaEstudiante = eBase.Cedula,
                    IdCurso = cursoLocal.Id_Curso,
                    FechaMatricula = DateTime.Now,
                    Estado = "ACTIVO"
                };
                _context.Matriculas.Add(nuevaMatricula);
                
                // Migración del Trigger SQL (tg_actualizar_cupos_after_matricula):
                // Reducir los cupos del curso asignado automáticamente
                if (cursoLocal.CuposDisponibles > 0)
                {
                    cursoLocal.CuposDisponibles -= 1;
                }

                await _context.SaveChangesAsync();

                // Devolver respuesta mapeada al nuevo ingreso
                var tlCode = await _context.TipoLicencias.Where(x=>x.Id_Tipo == external.IdTipoLicencia).Select(x=>x.Codigo).FirstOrDefaultAsync() ?? "C";

                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(new EstudianteLogisticaResponse
                {
                    Cedula = eBase.Cedula,
                    EstudianteNombre = $"{eBase.Apellidos} {eBase.Nombres}".ToUpper(),
                    CursoDetalle = $"[SYNC] {cursoLocal.Nombre} {cursoLocal.Nivel}".ToUpper(),
                    Paralelo = external.Paralelo.ToUpper(),
                    Jornada = external.Jornada.ToUpper(),
                    TipoLicencia = tlCode.ToUpper(),
                    Periodo = external.Periodo.ToUpper(),
                    IdMatricula = nuevaMatricula.Id_Matricula
                }, "Sistema Central: Alumno sincronizado y matriculado automáticamente."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<EstudianteLogisticaResponse>.Fail($"Error en Sincronización: {ex.Message}"));
            }
        }


        [HttpGet("vehiculos-disponibles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VehiculoLogisticaResponse>>>> GetVehiculosDisponibles()
        {
            // Devuelve todos los operativos, la BD validará posteriormente si ya están en uso
            var query = await (from v in _context.Vehiculos
                               join i in _context.Instructores on v.IdInstructorFijo equals i.Id_Instructor
                               where v.EstadoMecanico == "OPERATIVO" && v.Activo
                               select new VehiculoLogisticaResponse
                               {
                                   IdVehiculo = v.Id_Vehiculo,
                                   VehiculoStr = $"{v.Placa} - #{v.NumeroVehiculo}",
                                   IdInstructorFijo = v.IdInstructorFijo,
                                   InstructorNombre = $"{i.Apellidos} {i.Nombres}".ToUpper(),
                                   KmActual = v.KmActual
                               }).ToListAsync();

            return Ok(ApiResponse<IEnumerable<VehiculoLogisticaResponse>>.Ok(query));
        }

        [HttpGet("instructores")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InstructorLogisticaResponse>>>> GetInstructores()
        {
            var query = await _context.Instructores
                .Where(i => i.Activo)
                .Select(i => new InstructorLogisticaResponse
                {
                    Id_Instructor = i.Id_Instructor,
                    FullName = $"{i.Apellidos} {i.Nombres}".ToUpper()
                })
                .OrderBy(i => i.FullName)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<InstructorLogisticaResponse>>.Ok(query));
        }

        [HttpPost("salida")]
        public async Task<ActionResult<ApiResponse<string>>> RegistrarSalida([FromBody] SalidaRequest req)
        {
            try
            {
                var result = await _logisticaService.RegistrarSalidaAsync(req.IdMatricula, req.IdVehiculo, req.IdInstructor, req.Observaciones ?? "Ninguna", req.RegistradoPor);
                if (result == "OK")
                {
                    return Ok(ApiResponse<string>.Ok(result, "Salida registrada con éxito."));
                }
                return BadRequest(ApiResponse<string>.Fail($"Ups. Error en BD: {result}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail($"Error interno: {ex.Message}"));
            }
        }

        [HttpPost("llegada")]
        public async Task<ActionResult<ApiResponse<string>>> RegistrarLlegada([FromBody] LlegadaRequest req)
        {
            try
            {
                var result = await _logisticaService.RegistrarLlegadaAsync(req.IdRegistro, req.KmLlegada, req.Observaciones ?? "Ninguna", req.RegistradoPor);
                if (result == "OK")
                {
                    return Ok(ApiResponse<string>.Ok(result, "Llegada registrada con éxito."));
                }
                return BadRequest(ApiResponse<string>.Fail($"Alerta en registro: {result}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail($"Error interno: {ex.Message}"));
            }
        }
    }
}
