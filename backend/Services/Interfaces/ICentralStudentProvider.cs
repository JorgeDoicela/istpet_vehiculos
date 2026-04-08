using backend.DTOs;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /**
     * Interface for the ISTPET Centralized Student Database Bridge.
     * This contract defines how we retrieve student records from the main
     * institutional database to sync them into the logistics system.
     */
    public class CentralStudentDto
    {
        public string Cedula { get; set; } = string.Empty;
        
        // --- Formato Limpio (Estructurado) ---
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Paralelo { get; set; }
        public string? Jornada { get; set; }
        
        // --- Formato "Legacy" (Messy/Foto) ---
        public string? NombreCompleto { get; set; } 
        public string? DetalleRaw { get; set; }   
        public string? CursoDetalle { get; set; }
        public string Periodo { get; set; } = string.Empty;
        public string? FotoBase64 { get; set; }
    }

    public class CentralInstructorDto
    {
        public string Cedula { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class ScheduledPracticeDto
    {
        public int IdPractica { get; set; }
        public string CedulaAlumno { get; set; } = string.Empty;
        public int IdVehiculo { get; set; }
        public string AlumnoNombre { get; set; } = string.Empty;
        public string CedulaProfesor { get; set; } = string.Empty;
        public TimeSpan? HoraSalida { get; set; }
        public string VehiculoDetalle { get; set; } = string.Empty;
        public string ProfesorNombre { get; set; } = string.Empty;
    }

    public interface ICentralStudentProvider
    {
        Task<CentralStudentDto?> GetFromCentralAsync(string cedula);
        Task<CentralInstructorDto?> GetInstructorFromCentralAsync(string cedula);
        Task<IEnumerable<CentralInstructorDto>> GetAllInstructorsFromCentralAsync();
        Task<CentralInstructorDto?> GetAssignedTutorAsync(string cedula);
        Task<ScheduledPracticeDto?> GetScheduledPracticeAsync(string cedula);
        Task<IEnumerable<ScheduledPracticeDto>> GetSchedulesForTodayAsync();
        Task<IEnumerable<CentralUserDto>> GetAllWebUsersAsync();
    }
}
