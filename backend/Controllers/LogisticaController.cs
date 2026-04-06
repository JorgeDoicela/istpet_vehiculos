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
        private readonly ICentralStudentProvider _centralProvider;

        public LogisticaController(AppDbContext context, ILogisticaService logisticaService, ICentralStudentProvider centralProvider)
        {
            _context = context;
            _logisticaService = logisticaService;
            _centralProvider = centralProvider;
        }

        [HttpGet("estudiante/{cedula}")]
        public async Task<ActionResult<ApiResponse<EstudianteLogisticaResponse>>> BuscarEstudiante(string cedula)
        {
            // 1. INTENTO LOCAL (MySQL actual)
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
                                   IdTipoLicencia = c.IdTipoLicencia,
                                   Periodo = c.Periodo.ToUpper(),
                                   IdMatricula = m.Id_Matricula
                               }).FirstOrDefaultAsync();

            if (localStudent != null)
            {
                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(localStudent, "Alumno localizado (Local)."));
            }

            // 2. INTENTO DESDE BASE DE DATOS CENTRAL (ISTPET)
            var centralData = await _centralProvider.GetFromCentralAsync(cedula);
            if (centralData == null)
            {
                return NotFound(ApiResponse<EstudianteLogisticaResponse>.Fail("Estudiante no registrado en la BD Central del ISTPET."));
            }

            // 3. AUTO-REGISTRO Y MATRÍCULA (SMART SYNC REAL)
            try 
            {
                // Importamos al alumno a nuestra base local si no existe
                var eBase = await _context.Estudiantes.FindAsync(centralData.Cedula);
                if (eBase == null)
                {
                    eBase = new backend.Models.Estudiante 
                    { 
                        Cedula = centralData.Cedula, 
                        Nombres = centralData.Nombres, 
                        Apellidos = centralData.Apellidos 
                    };
                    _context.Estudiantes.Add(eBase);
                }

                // Buscamos un curso local activo para auto-matricularlo (Default o por Licencia)
                var cursoLocal = await _context.Cursos.FirstOrDefaultAsync(c => c.IdTipoLicencia == (centralData.IdTipoLicencia != 0 ? centralData.IdTipoLicencia : 1) && c.Estado == "ACTIVO")
                                 ?? await _context.Cursos.FirstOrDefaultAsync(c => c.Estado == "ACTIVO");

                if (cursoLocal == null) 
                    return BadRequest(ApiResponse<EstudianteLogisticaResponse>.Fail("Central: No hay cursos locales activos para automatizar el ingreso."));

                // Creamos la matrícula en Logística
                var nuevaMatricula = new backend.Models.Matricula
                {
                    CedulaEstudiante = eBase.Cedula,
                    IdCurso = cursoLocal.Id_Curso,
                    FechaMatricula = DateTime.Now,
                    Estado = "ACTIVO"
                };
                _context.Matriculas.Add(nuevaMatricula);
                
                // Actualizamos cupos del curso
                if (cursoLocal.CuposDisponibles > 0) cursoLocal.CuposDisponibles -= 1;

                await _context.SaveChangesAsync();

                // Obtenemos código de licencia para respuesta
                var tlCode = await _context.TipoLicencias.Where(x=>x.Id_Tipo == cursoLocal.IdTipoLicencia).Select(x=>x.Codigo).FirstOrDefaultAsync() ?? "C";

                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(new EstudianteLogisticaResponse
                {
                    Cedula = eBase.Cedula,
                    EstudianteNombre = $"{eBase.Apellidos} {eBase.Nombres}".ToUpper(),
                    CursoDetalle = $"[CENTRAL] {cursoLocal.Nombre} {cursoLocal.Nivel}".ToUpper(),
                    Paralelo = centralData.Paralelo.ToUpper(),
                    Jornada = centralData.Jornada.ToUpper(),
                    TipoLicencia = tlCode.ToUpper(),
                    IdTipoLicencia = cursoLocal.IdTipoLicencia,
                    Periodo = centralData.Periodo.ToUpper(),
                    IdMatricula = nuevaMatricula.Id_Matricula
                }, "Sincronizado: Alumno importado desde la Base de Datos Central ISTPET."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<EstudianteLogisticaResponse>.Fail($"Error en Puente Central: {ex.Message}"));
            }
        }


        [HttpGet("vehiculos-disponibles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VehiculoLogisticaResponse>>>> GetVehiculosDisponibles()
        {
            // Filtramos vehículos operativos y QUE NO ESTÉN EN PISTA (no tengan salida activa)
            var query = await (from v in _context.Vehiculos
                               join i in _context.Instructores on v.IdInstructorFijo equals i.Id_Instructor
                               where v.EstadoMecanico == "OPERATIVO" && v.Activo
                               && !_context.ClasesActivas.Any(ca => ca.Id_Vehiculo == v.Id_Vehiculo)
                               select new VehiculoLogisticaResponse
                               {
                                   IdVehiculo = v.Id_Vehiculo,
                                   VehiculoStr = (v.Placa + " - #" + v.NumeroVehiculo),
                                   IdInstructorFijo = v.IdInstructorFijo,
                                   InstructorNombre = (i.Apellidos + " " + i.Nombres).ToUpper(),
                                   KmActual = v.KmActual,
                                   IdTipoLicencia = v.IdTipoLicencia
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
                    FullName = (i.Apellidos + " " + i.Nombres).ToUpper()
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
                if (result == "EXITO")
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
                if (result == "EXITO")
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
