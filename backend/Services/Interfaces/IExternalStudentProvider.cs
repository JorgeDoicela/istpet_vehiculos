namespace backend.Services.Interfaces
{
    /**
     * Interface for the External Student Retrieval System
     * This will be replaced by the real API/DB once available.
     */
    public interface IExternalStudentProvider
    {
        Task<ExternalStudentDto?> GetByCedulaAsync(string cedula);
    }

    public class ExternalStudentDto
    {
        public string Cedula { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public int IdTipoLicencia { get; set; }
        public string CursoSugerido { get; set; } = string.Empty;
    }
}
