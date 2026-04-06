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
        public int Id_Vehiculo { get; set; }
        public int Numero_Vehiculo { get; set; }
        public string Placa { get; set; } = string.Empty;
    }
}
