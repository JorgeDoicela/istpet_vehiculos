using backend.Data;
using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;

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
            // 0. LIMPIEZA DE CEDULA
            cedula = cedula.Trim();

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
                // --- DETECCION DE INSTRUCTOR / PRÁCTICA (Cerebro de Sugerencia) ---
                var scheduled = await _centralProvider.GetScheduledPracticeAsync(localStudent.Cedula);
                CentralInstructorDto? tutor = null;
                if (scheduled == null) 
                    tutor = await _centralProvider.GetAssignedTutorAsync(localStudent.Cedula);

                string? profCedula = (scheduled?.CedulaProfesor ?? tutor?.Cedula)?.Trim();
                if (!string.IsNullOrEmpty(profCedula))
                {
                    var localProf = await _context.Instructores.FirstOrDefaultAsync(i => i.Cedula == profCedula);
                    if (localProf == null)
                    {
                        var cp = scheduled != null 
                            ? new CentralInstructorDto { Cedula = scheduled.CedulaProfesor, Nombres = scheduled.ProfesorNombre, Apellidos = "" } 
                            : tutor;

                        if (cp != null)
                        {
                            // Split name if it comes as one string (from practices)
                            string fn = cp.Nombres;
                            string fa = cp.Apellidos;
                            if (string.IsNullOrEmpty(fa) && fn.Contains(" "))
                            {
                                var parts = fn.Split(' ', 2);
                                fa = parts[0];
                                fn = parts[1];
                            }
                            localProf = new backend.Models.Instructor { Cedula = cp.Cedula, Nombres = fn.ToUpper(), Apellidos = fa.ToUpper(), Activo = true };
                            _context.Instructores.Add(localProf);
                            await _context.SaveChangesAsync();
                        }
                    }
                    localStudent.IdPracticaInstructor = localProf?.Id_Instructor;
                    localStudent.PracticaInstructor = localProf != null ? $"{localProf.Apellidos} {localProf.Nombres}" : (scheduled?.ProfesorNombre ?? $"{tutor?.Apellidos} {tutor?.Nombres}");
                    localStudent.IdPracticaCentral = scheduled?.IdPractica;
                    localStudent.PracticaVehiculo = scheduled?.VehiculoDetalle;
                    localStudent.PracticaHora = scheduled?.HoraSalida?.ToString(@"hh\:mm");
                }

                // --- DETECCION DE DISPONIBILIDAD (SISTEMA LOCAL) ---
                localStudent.IsBusy = await _context.RegistrosSalida
                    .AnyAsync(s => s.IdMatricula == localStudent.IdMatricula 
                               && !_context.RegistrosLlegada.Any(l => l.IdRegistro == s.Id_Registro));

                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(localStudent, "Alumno localizado (Local)."));
            }

            // 2. INTENTO DESDE BASE DE DATOS CENTRAL (ISTPET)
            var centralData = await _centralProvider.GetFromCentralAsync(cedula);
            if (centralData == null)
            {
                return NotFound(ApiResponse<EstudianteLogisticaResponse>.Fail("Estudiante no registrado en la BD Central del ISTPET."));
            }

            // 3. AUTO-REGISTRO Y MATRÍCULA (PUENTE HÍBRIDO UNIVERSAL)
            try 
            {
                // -- LÓGICA DE PRIORIDAD (DATOS LIMPIOS > SMART PARSING) --
                
                string finalNombres = "S/N";
                string finalApellidos = "S/N";
                string finalParalelo = "A";
                string finalJornada = "MATUTINA";

                // 1. Prioridad: Nombres y Apellidos (Formato Estructurado)
                if (!string.IsNullOrEmpty(centralData.Nombres) && !string.IsNullOrEmpty(centralData.Apellidos))
                {
                    finalNombres = centralData.Nombres;
                    finalApellidos = centralData.Apellidos;
                }
                else if (!string.IsNullOrEmpty(centralData.NombreCompleto))
                {
                    // Fallback: Smart Parsing de nombre completo (Messy)
                    var parts = centralData.NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    finalApellidos = parts.Length >= 2 ? $"{parts[0]} {parts[1]}" : centralData.NombreCompleto;
                    finalNombres = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "S/N";
                }

                // 2. Prioridad: Paralelo (Formato Estructurado)
                if (!string.IsNullOrEmpty(centralData.Paralelo))
                {
                    finalParalelo = centralData.Paralelo;
                }
                else if (!string.IsNullOrEmpty(centralData.DetalleRaw))
                {
                    // Fallback: Buscar patrón "PARALELO:X"
                    var matchParalelo = Regex.Match(centralData.DetalleRaw, @"PARALELO:([A-Z])", RegexOptions.IgnoreCase);
                    if (matchParalelo.Success) finalParalelo = matchParalelo.Groups[1].Value.ToUpper();
                }

                // 3. Prioridad: Jornada (Formato Estructurado)
                if (!string.IsNullOrEmpty(centralData.Jornada))
                {
                    finalJornada = centralData.Jornada;
                }
                else if (!string.IsNullOrEmpty(centralData.DetalleRaw))
                {
                    // Fallback: Buscar palabras clave en bloque de texto
                    if (centralData.DetalleRaw.Contains("MATUTINA", StringComparison.OrdinalIgnoreCase)) finalJornada = "MATUTINA";
                    else if (centralData.DetalleRaw.Contains("VESPERTINA", StringComparison.OrdinalIgnoreCase)) finalJornada = "VESPERTINA";
                    else if (centralData.DetalleRaw.Contains("NOCTURNA", StringComparison.OrdinalIgnoreCase)) finalJornada = "NOCTURNA";
                }

                // -- PERSISTENCIA LOCAL (Mapeo Final) --

                // Importamos al alumno a nuestra base local
                var eBase = await _context.Estudiantes.FindAsync(centralData.Cedula);
                if (eBase == null)
                {
                    eBase = new backend.Models.Estudiante 
                    { 
                        Cedula = centralData.Cedula, 
                        Nombres = finalNombres.ToUpper(), 
                        Apellidos = finalApellidos.ToUpper() 
                    };
                    _context.Estudiantes.Add(eBase);
                }

                // Buscamos un curso local (Default Tipo C para puente central)
                var cursoLocal = await _context.Cursos.FirstOrDefaultAsync(c => c.IdTipoLicencia == 1 && c.Estado == "ACTIVO")
                                 ?? await _context.Cursos.FirstOrDefaultAsync(c => c.Estado == "ACTIVO");

                if (cursoLocal == null) 
                    return BadRequest(ApiResponse<EstudianteLogisticaResponse>.Fail("Central: No hay cursos locales activos para automatizar el ingreso."));

                var nuevaMatricula = new backend.Models.Matricula
                {
                    CedulaEstudiante = eBase.Cedula,
                    IdCurso = cursoLocal.Id_Curso,
                    FechaMatricula = DateTime.Now,
                    Estado = "ACTIVO"
                };
                _context.Matriculas.Add(nuevaMatricula);
                
                if (cursoLocal.CuposDisponibles > 0) cursoLocal.CuposDisponibles -= 1;

                await _context.SaveChangesAsync();
                
                // --- DETECCION DE INSTRUCTOR / PRÁCTICA (Cerebro de Sugerencia) ---
                var scheduled = await _centralProvider.GetScheduledPracticeAsync(eBase.Cedula);
                CentralInstructorDto? tutor = null;
                if (scheduled == null) 
                    tutor = await _centralProvider.GetAssignedTutorAsync(eBase.Cedula);

                // Mapeo a Instructor Local (Auto-Sincronización)
                int? suggestedInstructorId = null;
                string? profName = null;
                string? profCedula = (scheduled?.CedulaProfesor ?? tutor?.Cedula)?.Trim();

                if (!string.IsNullOrEmpty(profCedula))
                {
                    var localProf = await _context.Instructores.FirstOrDefaultAsync(i => i.Cedula == profCedula);
                    if (localProf == null)
                    {
                        var cp = scheduled != null 
                            ? new CentralInstructorDto { Cedula = scheduled.CedulaProfesor, Nombres = scheduled.ProfesorNombre, Apellidos = "" } 
                            : tutor;

                        if (cp != null)
                        {
                            string fn = cp.Nombres;
                            string fa = cp.Apellidos;
                            if (string.IsNullOrEmpty(fa) && fn.Contains(" "))
                            {
                                var parts = fn.Split(' ', 2);
                                fa = parts[0];
                                fn = parts[1];
                            }
                            localProf = new backend.Models.Instructor { Cedula = cp.Cedula, Nombres = fn.ToUpper(), Apellidos = fa.ToUpper(), Activo = true };
                            _context.Instructores.Add(localProf);
                            await _context.SaveChangesAsync();
                        }
                    }
                    suggestedInstructorId = localProf?.Id_Instructor;
                    profName = localProf != null ? $"{localProf.Apellidos} {localProf.Nombres}" : (scheduled?.ProfesorNombre ?? $"{tutor?.Apellidos} {tutor?.Nombres}");
                }

                return Ok(ApiResponse<EstudianteLogisticaResponse>.Ok(new EstudianteLogisticaResponse
                {
                    Cedula = eBase.Cedula,
                    EstudianteNombre = $"{eBase.Apellidos} {eBase.Nombres}".ToUpper(),
                    CursoDetalle = (centralData.DetalleRaw ?? "CARRERA GENERAL").ToUpper(),
                    Paralelo = finalParalelo.ToUpper(),
                    Jornada = finalJornada.ToUpper(),
                    TipoLicencia = "C",
                    IdTipoLicencia = 1,
                    Periodo = centralData.Periodo ?? "2026-I",
                    IdMatricula = nuevaMatricula.Id_Matricula,
                    FotoBase64 = centralData.FotoBase64,
                    // Sugerencias de Agenda
                    IdPracticaCentral = scheduled?.IdPractica,
                    IdPracticaInstructor = suggestedInstructorId,
                    PracticaInstructor = profName,
                    PracticaVehiculo = scheduled?.VehiculoDetalle,
                    PracticaHora = scheduled?.HoraSalida?.ToString(@"hh\:mm"),
                    // Disponibilidad
                    IsBusy = await _context.RegistrosSalida
                        .AnyAsync(s => s.IdMatricula == nuevaMatricula.Id_Matricula 
                                   && !_context.RegistrosLlegada.Any(l => l.IdRegistro == s.Id_Registro))
                }, "Sincronizado: Alumno localizado mediante el Puente Híbrido Universal."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<EstudianteLogisticaResponse>.Fail($"Error en Sincronización Híbrida: {ex.Message}"));
            }
        }


        [HttpGet("vehiculos-disponibles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VehiculoLogisticaResponse>>>> GetVehiculosDisponibles()
        {
            // Filtramos vehículos operativos y QUE NO ESTÉN EN PISTA (no tengan salida activa sin llegada)
            var query = await (from v in _context.Vehiculos
                               join i in _context.Instructores on v.IdInstructorFijo equals i.Id_Instructor
                               where v.EstadoMecanico == "OPERATIVO" && v.Activo
                               && !_context.RegistrosSalida.Any(s => s.IdVehiculo == v.Id_Vehiculo 
                                  && !_context.RegistrosLlegada.Any(l => l.IdRegistro == s.Id_Registro))
                               select new VehiculoLogisticaResponse
                               {
                                   IdVehiculo = v.Id_Vehiculo,
                                   NumeroVehiculo = v.NumeroVehiculo,
                                   VehiculoStr = (v.Placa + " - #" + v.NumeroVehiculo),
                                   IdInstructorFijo = v.IdInstructorFijo,
                                   InstructorNombre = (i.Apellidos + " " + i.Nombres).ToUpper(),
                                   IdTipoLicencia = v.IdTipoLicencia
                               }).ToListAsync();

            return Ok(ApiResponse<IEnumerable<VehiculoLogisticaResponse>>.Ok(query));
        }

        [HttpGet("instructores")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InstructorLogisticaResponse>>>> GetInstructores()
        {
            // Filtramos instructores activos Y QUE NO ESTÉN EN PISTA
            var query = await _context.Instructores
                .Where(i => i.Activo && !_context.RegistrosSalida.Any(s => s.IdInstructor == i.Id_Instructor 
                           && !_context.RegistrosLlegada.Any(l => l.IdRegistro == s.Id_Registro)))
                .Select(i => new InstructorLogisticaResponse
                {
                    Id_Instructor = i.Id_Instructor,
                    FullName = (i.Apellidos + " " + i.Nombres).ToUpper()
                })
                .OrderBy(i => i.FullName)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<InstructorLogisticaResponse>>.Ok(query));
        }

        [HttpGet("instructor/{cedula}")]
        public async Task<ActionResult<ApiResponse<InstructorLogisticaResponse>>> BuscarInstructor(string cedula)
        {
            // 1. Intento local
            var localInstr = await _context.Instructores
                .Where(i => i.Cedula == cedula && i.Activo)
                .Select(i => new InstructorLogisticaResponse
                {
                    Id_Instructor = i.Id_Instructor,
                    FullName = (i.Apellidos + " " + i.Nombres).ToUpper()
                })
                .FirstOrDefaultAsync();

            if (localInstr != null) return Ok(ApiResponse<InstructorLogisticaResponse>.Ok(localInstr, "Instructor localizado (Local)."));

            // 2. Intento Central
            var centralData = await _centralProvider.GetInstructorFromCentralAsync(cedula);
            if (centralData == null) return NotFound(ApiResponse<InstructorLogisticaResponse>.Fail("Instructor no hallado en BD Central."));

            // 3. Auto-Registro
            var nuevoInstr = new backend.Models.Instructor
            {
                Cedula = centralData.Cedula,
                Nombres = centralData.Nombres.ToUpper(),
                Apellidos = centralData.Apellidos.ToUpper(),
                Activo = true
            };
            _context.Instructores.Add(nuevoInstr);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<InstructorLogisticaResponse>.Ok(new InstructorLogisticaResponse
            {
                Id_Instructor = nuevoInstr.Id_Instructor,
                FullName = $"{nuevoInstr.Apellidos} {nuevoInstr.Nombres}".ToUpper()
            }, "Instructor sincronizado desde SIGAFI."));
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

        [HttpGet("buscar")]
        public async Task<ActionResult<ApiResponse<IEnumerable<dynamic>>>> BuscarEstudiantes([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 3)
                return Ok(ApiResponse<IEnumerable<dynamic>>.Ok(new List<dynamic>()));

            var term = query.ToUpper();
            
            var sugerencias = await _context.Estudiantes
                .Where(e => e.Cedula.StartsWith(term) || e.Apellidos.Contains(term) || e.Nombres.Contains(term))
                .Select(e => new {
                    Cedula = e.Cedula,
                    NombreCompleto = $"{e.Apellidos} {e.Nombres}".ToUpper(),
                    IsBusy = _context.RegistrosSalida.Any(s => s.IdMatricula == _context.Matriculas
                        .Where(m => m.CedulaEstudiante == e.Cedula && m.Estado == "ACTIVO")
                        .Select(m => m.Id_Matricula).FirstOrDefault()
                        && !_context.RegistrosLlegada.Any(l => l.IdRegistro == s.Id_Registro)),
                    Carrera = _context.Matriculas
                        .Where(m => m.CedulaEstudiante == e.Cedula)
                        .Join(_context.Cursos, m => m.IdCurso, c => c.Id_Curso, (m, c) => c.Nombre)
                        .FirstOrDefault() ?? "ESTUDIANTE REGULAR"
                })
                .Take(5)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<dynamic>>.Ok(sugerencias));
        }

        [HttpPost("llegada")]
        public async Task<ActionResult<ApiResponse<string>>> RegistrarLlegada([FromBody] LlegadaRequest req)
        {
            try
            {
                var result = await _logisticaService.RegistrarLlegadaAsync(req.IdRegistro, req.Observaciones ?? "Ninguna", req.RegistradoPor);
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
        [HttpGet("agendados-hoy")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ScheduledPracticeDto>>>> GetAgendadosHoy()
        {
            var data = await _centralProvider.GetSchedulesForTodayAsync();
            return Ok(ApiResponse<IEnumerable<ScheduledPracticeDto>>.Ok(data, "Listado de agendados del día (SIGAFI)."));
        }
    }
}
