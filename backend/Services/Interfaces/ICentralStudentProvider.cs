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
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public int IdTipoLicencia { get; set; } // Proposed category based on previous records
        public string Paralelo { get; set; } = "A";
        public string Jornada { get; set; } = "MATUTINA";
        public string Periodo { get; set; } = "2026-I";
    }

    public interface ICentralStudentProvider
    {
        Task<CentralStudentDto?> GetFromCentralAsync(string cedula);
    }
}
