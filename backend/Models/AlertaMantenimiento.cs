using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    /**
     * Read-only model for SQL View: v_alerta_mantenimiento
     */
    [Keyless]
    public class AlertaMantenimiento
    {
        public int Numero_Vehiculo { get; set; }
        public string Placa { get; set; } = string.Empty;
        public int Km_Actual { get; set; }
        public int? Km_Proximo_Mantenimiento { get; set; }
        public int? Km_Para_Taller { get; set; }
    }
}
