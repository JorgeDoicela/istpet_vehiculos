namespace backend.Services.Interfaces
{
    /**
     * Interface for specialized Logistics Management
     * Uses SQL Stored Procedures for business logic integrity.
     */
    public interface ILogisticaService
    {
        Task<string> RegistrarSalidaAsync(int idMatricula, int idVehiculo, int idInstructor, string observaciones, int registradoPor);
        Task<string> RegistrarLlegadaAsync(int idRegistro, string observaciones, int registradoPor);
    }
}
