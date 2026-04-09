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

            response.Modules.Add(await ProbeCountAsync("tipo_licencia (categoria_vehiculos)",
                () => _central.GetAllLicenseTypesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("cursos",
                () => _central.GetAllCoursesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("categoria_vehiculos",
                () => _central.GetAllVehicleCategoriesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("categorias_examenes_conduccion",
                () => _central.GetAllExamCategoriesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("profesores",
                () => _central.GetAllInstructorsFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("usuarios_web",
                () => _central.GetAllWebUsersAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("alumnos",
                () => _central.GetAllStudentsFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("matriculas (validas)",
                () => _central.GetActiveEnrollmentsFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("matriculas_examen_conduccion",
                () => _central.GetMatriculaExamLinksFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("vehiculos",
                () => _central.GetAllVehiclesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("cond_alumnos_practicas",
                () => _central.GetAllPracticesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("asignacion_instructores_vehiculos",
                () => _central.GetInstructorVehicleAssignmentsFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("cond_alumnos_vehiculos",
                () => _central.GetStudentVehicleAssignmentsFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("cond_alumnos_horarios",
                () => _central.GetAllSchedulesFromCentralAsync(), cancellationToken));
            response.Modules.Add(await ProbeCountAsync("cond_practicas_horarios_alumnos",
                () => _central.GetPracticeScheduleLinksFromCentralAsync(), cancellationToken));

            await ProbeSampleAlumnoAsync(response, cancellationToken);

            return response;
        }

        private static async Task<SigafiProbeModuleResult> ProbeCountAsync<T>(
            string name,
            Func<Task<IEnumerable<T>>> fetch,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var list = await fetch();
                var count = list.Count();
                return new SigafiProbeModuleResult { Name = name, RowCount = count, Ok = true };
            }
            catch (Exception ex)
            {
                return new SigafiProbeModuleResult { Name = name, Ok = false, Error = ex.Message };
            }
        }

        private async Task ProbeSampleAlumnoAsync(SigafiProbeResponse response, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var students = await _central.GetAllStudentsFromCentralAsync();
                var first = students.FirstOrDefault();
                if (first == null)
                {
                    response.SampleAlumnoDetailOk = null;
                    response.SampleAlumnoError = "No hay alumnos en SIGAFI para probar GetFromCentralAsync.";
                    return;
                }

                response.SampleAlumnoId = first.idAlumno;
                var detail = await _central.GetFromCentralAsync(first.idAlumno);
                response.SampleAlumnoDetailOk = detail != null;
                if (detail == null)
                    response.SampleAlumnoError = "GetFromCentralAsync devolvió null (p. ej. sin matrícula en periodo activo).";
            }
            catch (Exception ex)
            {
                response.SampleAlumnoDetailOk = false;
                response.SampleAlumnoError = ex.Message;
            }
        }
    }
}
