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
    }
}
