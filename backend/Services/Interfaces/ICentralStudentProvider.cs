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
        public string Periodo { get; set; } = string.Empty;      
    }

    public interface ICentralStudentProvider
    {
        Task<CentralStudentDto?> GetFromCentralAsync(string cedula);
    }
}
