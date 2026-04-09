namespace backend.Services.Interfaces
{
    /**
     * Interface for specialized Logistics Management
     * Uses SQL Stored Procedures for business logic integrity.
     */
    public interface ILogisticaService
    {
        Task<string> RegistrarSalidaAsync(int idMatricula, int idVehiculo, string idInstructor, string observaciones, int registradoPor, int? idAsignacionHorario = null);
        Task<string> RegistrarLlegadaAsync(int idRegistro, string observaciones, int registradoPor);
    }
}
