using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Bitácora de Sincronización Profesional
     * Registra cada trago de datos externos para auditoría.
     */
    public class SyncLog
    {
        [Key]
        public int Id_Log { get; set; }
        
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        
        public string Modulo { get; set; } = string.Empty; // Ej: "Estudiantes", "Vehiculos"
        
        public string Origen { get; set; } = "API_EXTERNA";
        
        public string Estado { get; set; } = "OK"; // OK, ERROR, ADVERTENCIA
        
        public string Mensaje { get; set; } = string.Empty; // Ej: "Sincronizados 5, Rechazados 2 por formato inválido"
        
        public int RegistrosProcesados { get; set; }
        
        public int RegistrosFallidos { get; set; }
    }
}
