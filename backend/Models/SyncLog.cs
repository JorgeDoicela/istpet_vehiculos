using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Bitácora de Sincronización Profesional
     * Registra cada trago de datos externos para auditoría.
     */
    using System.ComponentModel.DataAnnotations.Schema;

    public class SyncLog
    {
        [Key]
        [Column("id_log")]
        public int Id_Log { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Column("modulo")]
        public string Modulo { get; set; } = string.Empty;

        [Column("origen")]
        public string Origen { get; set; } = "API_EXTERNA";

        [Column("estado")]
        public string Estado { get; set; } = "OK";

        [Column("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [Column("registros_procesados")]
        public int RegistrosProcesados { get; set; }

        [Column("registros_fallidos")]
        public int RegistrosFallidos { get; set; }
    }
}
