using backend.DTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Reportes leídos directamente de la BD central SIGAFI (cond_alumnos_practicas y tablas relacionadas).
    /// </summary>
    public interface ISigafiReportService
    {
        Task<IReadOnlyList<ReportePracticasDTO>> GetReportePracticasAsync(
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? cedulaProfesor);
    }
}
