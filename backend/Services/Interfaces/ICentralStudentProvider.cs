using backend.DTOs;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /**
     * Interface for the ISTPET Centralized Student Database Bridge.
     * Updated 2026: Absolute SIGAFI Naming Parity.
     */
    public class CentralStudentDto
    {
        public string idAlumno { get; set; } = string.Empty;
        public string? primerNombre { get; set; }
        public string? apellidoPaterno { get; set; }
        public string? apellidoMaterno { get; set; }
        public string? segundoNombre { get; set; }
        public string? paralelo { get; set; }
        public string? seccion { get; set; }
        public string? Nivel { get; set; }
        public string idPeriodo { get; set; } = string.Empty;
        public byte[]? foto { get; set; }

        // --- Helper for UI Compatibility ---
        public string? NombreCompleto { get; set; } 
        public string? DetalleRaw { get; set; }   
        public string? FotoBase64 { get; set; }
    }

    public class CentralInstructorDto
    {
        public string idProfesor { get; set; } = string.Empty;
        public string nombres { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        
        public string? primerApellido { get; set; }
        public string? segundoApellido { get; set; }
        public string? primerNombre { get; set; }
        public string? segundoNombre { get; set; }

        public string? celular { get; set; }
        public string? email { get; set; }
        public int activo { get; set; } 
    }

    public class ScheduledPracticeDto
    {
        public int idPractica { get; set; }
        public string idalumno { get; set; } = string.Empty; 
        public int idvehiculo { get; set; } 
        public string idProfesor { get; set; } = string.Empty;
        public DateTime fecha { get; set; }
        public TimeSpan? hora_salida { get; set; }
        
        public string AlumnoNombre { get; set; } = string.Empty;
        public string VehiculoDetalle { get; set; } = string.Empty;
        public string ProfesorNombre { get; set; } = string.Empty;
    }

    public class CentralHorarioDto
    {
        public int idAsignacionHorario { get; set; }
        public int idAsignacion { get; set; }
        public DateTime Fecha { get; set; }
        public string Hora { get; set; } = string.Empty;
        public int asiste { get; set; }
    }

    public class CentralVehiculoDto
    {
        public int idVehiculo { get; set; }
        public int? idSubcategoria { get; set; }
        public string? numero_vehiculo { get; set; }
        public string? placa { get; set; }
        public string? marca { get; set; }
        public int? anio { get; set; }
        public int? idCategoria { get; set; }
        public int activo { get; set; }
        public string? observacion { get; set; }
        public string? chasis { get; set; }
        public string? motor { get; set; }
        public string? modelo { get; set; }
    }

    public class CentralCursoDto
    {
        public int idNivel { get; set; }
        public int idCarrera { get; set; }
        public string? Nivel { get; set; }
        public int? jerarquia { get; set; }
        public int? orden { get; set; }
        public int? esRecuperacion { get; set; }
        public string? aliasCurso { get; set; }
    }

    public class CentralTipoLicenciaDto
    {
        public int id_tipo { get; set; }
        public string codigo { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public int activo { get; set; }
    }

    public class CentralCategoriaVehiculoDto
    {
        public int idCategoria { get; set; }
        public string categoria { get; set; } = string.Empty;
    }

    public class CentralCategoriaExamenDto
    {
        public int IdCategoria { get; set; }
        public string categoria { get; set; } = string.Empty;
        public int tieneNota { get; set; }
        public int activa { get; set; }
    }

    public class CentralMatriculaDto
    {
        public int idMatricula { get; set; }
        public string idAlumno { get; set; } = string.Empty;
        public int idNivel { get; set; }
        public int idSeccion { get; set; }
        public int idModalidad { get; set; }
        public string idPeriodo { get; set; } = string.Empty;
        public DateTime? fechaMatricula { get; set; }
        public string? paralelo { get; set; }
        public int? arrastres { get; set; }
        public int? folio { get; set; }
        public decimal? beca_matricula { get; set; }
        public int? retirado { get; set; }
        public int? esOyente { get; set; }
        public int valida { get; set; }
    }

    public class CentralAsignacionInstructorVehiculoDto
    {
        public int idAsignacion { get; set; }
        public int idVehiculo { get; set; }
        public string idProfesor { get; set; } = string.Empty;
        public DateTime? fecha_asignacion { get; set; }
        public DateTime? fecha_salida { get; set; }
        public int activo { get; set; }
        public string? usuario_asigna { get; set; }
        public string? usuario_desactiva { get; set; }
        public string? observacion { get; set; }
    }

    public class CentralAsignacionAlumnoVehiculoDto
    {
        public int idAsignacion { get; set; }
        public string idAlumno { get; set; } = string.Empty;
        public int idVehiculo { get; set; }
        public string idProfesor { get; set; } = string.Empty;
        public string? idPeriodo { get; set; }
        public DateTime? fechaAsignacion { get; set; }
        public DateTime? fechaInicio { get; set; }
        public DateTime? fechaFin { get; set; }
        public int activa { get; set; }
        public string? observacion { get; set; }
    }

    public class CentralPracticaHorarioDto
    {
        public int idPractica { get; set; }
        public int idAsignacionHorario { get; set; }
    }

    public class CentralAlumnoLiteDto
    {
        public string idAlumno { get; set; } = string.Empty;
        public string? primerNombre { get; set; }
        public string? segundoNombre { get; set; }
        public string? apellidoPaterno { get; set; }
        public string? apellidoMaterno { get; set; }
        public string? celular { get; set; }
        public string? email { get; set; }
    }

    public interface ICentralStudentProvider
    {
        Task<CentralStudentDto?> GetFromCentralAsync(string idAlumno);
        Task<CentralInstructorDto?> GetInstructorFromCentralAsync(string idProfesor);
        Task<IEnumerable<CentralInstructorDto>> GetAllInstructorsFromCentralAsync();
        Task<CentralInstructorDto?> GetAssignedTutorAsync(string idAlumno);
        Task<ScheduledPracticeDto?> GetScheduledPracticeAsync(string idAlumno);
        Task<CentralHorarioDto?> GetNextScheduleAsync(string idAlumno);
        Task<IEnumerable<ScheduledPracticeDto>> GetSchedulesForTodayAsync();
        Task<IEnumerable<CentralUserDto>> GetAllWebUsersAsync();
        Task<IEnumerable<CentralVehiculoDto>> GetAllVehiclesFromCentralAsync();
        Task<IEnumerable<CentralCursoDto>> GetAllCoursesFromCentralAsync();
        Task<IEnumerable<CentralTipoLicenciaDto>> GetAllLicenseTypesFromCentralAsync();
        Task<IEnumerable<CentralCategoriaVehiculoDto>> GetAllVehicleCategoriesFromCentralAsync();
        Task<IEnumerable<CentralCategoriaExamenDto>> GetAllExamCategoriesFromCentralAsync();
        Task<IEnumerable<CentralAlumnoLiteDto>> GetAllStudentsFromCentralAsync();
        Task<IEnumerable<CentralMatriculaDto>> GetActiveEnrollmentsFromCentralAsync();
        Task<IEnumerable<CentralAsignacionInstructorVehiculoDto>> GetInstructorVehicleAssignmentsFromCentralAsync();
        Task<IEnumerable<CentralAsignacionAlumnoVehiculoDto>> GetStudentVehicleAssignmentsFromCentralAsync();
        Task<IEnumerable<CentralHorarioDto>> GetAllSchedulesFromCentralAsync();
        Task<IEnumerable<CentralPracticaHorarioDto>> GetPracticeScheduleLinksFromCentralAsync();
        Task<bool> PingSigafiAsync();
    }
}
