using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    /**
     * Read-only model for SQL View: v_clases_activas
     */
    [Keyless]
    public class ClaseActiva
    {
        public int Id_Registro { get; set; }
        public int Id_Vehiculo { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string Estudiante { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public int NumeroVehiculo { get; set; }
        public string Instructor { get; set; } = string.Empty;
        public DateTime Salida { get; set; }
    }
}
