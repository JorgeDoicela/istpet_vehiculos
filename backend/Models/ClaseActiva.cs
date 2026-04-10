using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    /**
     * Read-only model for SQL View: v_clases_activas.
     * Absolute Parity 2026: Aligned with idPractica as the primary handle.
     */
    [Keyless]
    public class ClaseActiva
    {
        public int idPractica { get; set; }
        public int idVehiculo { get; set; }
        public string idAlumno { get; set; } = string.Empty;
        public string estudiante { get; set; } = string.Empty;
        public string placa { get; set; } = string.Empty;
        public string? numeroVehiculo { get; set; }
        public string instructor { get; set; } = string.Empty;
        public TimeSpan? salida { get; set; }
    }

}
