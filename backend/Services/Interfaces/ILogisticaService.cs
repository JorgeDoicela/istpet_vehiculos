namespace backend.Services.Interfaces
{
    /**
     * Interface for specialized Logistics Management
     * Uses SQL Stored Procedures for business logic integrity.
     */
    public interface ILogisticaService
    {
        Task<string> RegistrarSalidaAsync(int idMatricula, int idVehiculo, string idInstructor, int registradoPor, IEnumerable<int>? idsAsignacionHorario = null, string? observaciones = null);
        Task<string> RegistrarLlegadaAsync(int idRegistro, int registradoPor);
    }
}
