namespace backend.Services.Interfaces
{
    /**
     * Interface for specialized Logistics Management
     * Uses SQL Stored Procedures for business logic integrity.
     */
    public interface ILogisticaService
    {
        Task<string> RegistrarSalidaAsync(int idMatricula, int idVehiculo, string idInstructor, string usuarioLogin, IEnumerable<int>? idsAsignacionHorario = null, string? observaciones = null);
        Task<string> RegistrarLlegadaAsync(int idRegistro, string usuarioLogin);
        Task<string> EliminarSalidaAsync(int idPractica, string usuarioLogin);
    }
}
