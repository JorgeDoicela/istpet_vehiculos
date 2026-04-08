using backend.DTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Reportes leídos de la instancia MySQL SIGAFI configurada en appsettings (servidor = fuente de verdad).
    /// </summary>
    public interface ISigafiReportService
    {
        Task<IReadOnlyList<ReportePracticasDTO>> GetReportePracticasAsync(
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? cedulaProfesor);
    }
}
