using backend.DTOs;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    /// <summary>
    /// Ejecuta cada extracción SIGAFI y devuelve conteos o error por módulo (sin escribir en BD local).
    /// </summary>
    public class SigafiExtractionProbe
    {
        private readonly ICentralStudentProvider _central;

        public SigafiExtractionProbe(ICentralStudentProvider central)
        {
            _central = central;
        }

        public async Task<SigafiProbeResponse> RunAsync(CancellationToken cancellationToken = default)
        {
            var response = new SigafiProbeResponse { CheckedAtUtc = DateTime.UtcNow };
            response.Connected = await _central.PingSigafiAsync();

            if (!response.Connected)
            {
                response.Modules.Add(new SigafiProbeModuleResult
                {
                    Name = "conexion",
                    Ok = false,
                    Error = "Ping SIGAFI falló (revisar SigafiConnection y red)."
                });
                return response;
            }

            // 1. Catálogos Operativos (Master Data)
            response.Modules.Add(await ProbeDeepAsync("periodos", () => _central.GetAllPeriodosFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("carreras", () => _central.GetAllCarrerasFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("secciones", () => _central.GetAllSeccionesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("modalidades", () => _central.GetAllModalidadesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("instituciones", () => _central.GetAllInstitucionesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("cursos", () => _central.GetAllCoursesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("categoria_vehiculos", () => _central.GetAllVehicleCategoriesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("categorias_examenes_conduccion", () => _central.GetAllExamCategoriesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("tipo_licencia", () => _central.GetAllLicenseTypesFromCentralAsync(), cancellationToken));

            // 2. Personal y Seguridad
            response.Modules.Add(await ProbeDeepAsync("profesores", () => _central.GetAllInstructorsFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("usuarios_web", () => _central.GetAllWebUsersAsync(), cancellationToken));
            
            // 3. Estudiantes y Matrículas
            response.Modules.Add(await ProbeDeepAsync("alumnos", () => _central.GetAllStudentsFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("matriculas", () => _central.GetActiveEnrollmentsFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("matriculas_examen_conduccion", () => _central.GetMatriculaExamLinksFromCentralAsync(), cancellationToken));

            // 4. Operativa Logística y Vehículos
            response.Modules.Add(await ProbeDeepAsync("vehiculos", () => _central.GetAllVehiclesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("cond_alumnos_practicas", () => _central.GetAllPracticesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("asignacion_instructores_vehiculos", () => _central.GetInstructorVehicleAssignmentsFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("cond_alumnos_vehiculos", () => _central.GetStudentVehicleAssignmentsFromCentralAsync(), cancellationToken));
            
            // 5. Calendario Operativo (Sincronización de Agenda)
            response.Modules.Add(await ProbeDeepAsync("cond_alumnos_horarios", () => _central.GetAllSchedulesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("cond_practicas_horarios_alumnos", () => _central.GetPracticeScheduleLinksFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("fechas_horarios", () => _central.GetAllFechasHorariosFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeDeepAsync("horario_profesores", () => _central.GetAllHorariosProfesoresFromCentralAsync(), cancellationToken));


            return response;
        }

        private static async Task<SigafiProbeModuleResult> ProbeDeepAsync<T>(
            string name,
            Func<Task<IEnumerable<T>>> fetch,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var list = await fetch();
                var enumerable = list.ToList();
                var count = enumerable.Count;
                var sample = enumerable.FirstOrDefault();
                return new SigafiProbeModuleResult 
                { 
                    Name = name, 
                    RowCount = count, 
                    Ok = true,
                    Sample = sample
                };
            }
            catch (Exception ex)
            {
                return new SigafiProbeModuleResult { Name = name, Ok = false, Error = ex.Message };
            }
        }
    }
}
