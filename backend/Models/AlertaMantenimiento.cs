using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    /**
     * Read-only model for SQL View: v_alerta_mantenimiento
     * Aligned with SQL Healer (snake_case database)
     */
    [Keyless]
    public class AlertaMantenimiento
    {
        public int id_vehiculo { get; set; }
        public int numero_vehiculo { get; set; }
        public string placa { get; set; } = string.Empty;
    }
}
