using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /// <summary>
    /// Registro inmutable de cada acción relevante realizada en el sistema.
    /// Solo lectura para auditoría; nunca se modifica un registro existente.
    /// </summary>
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        /// <summary>Cédula / login del operador que ejecutó la acción.</summary>
        [Required]
        [MaxLength(50)]
        [Column("usuario")]
        public string usuario { get; set; } = string.Empty;

        /// <summary>Código de la acción: LOGIN, LOGIN_FAIL, SALIDA, LLEGADA, SYNC, SYNC_FAIL.</summary>
        [Required]
        [MaxLength(50)]
        [Column("accion")]
        public string accion { get; set; } = string.Empty;

        /// <summary>ID de la entidad afectada (idPractica, idAlumno, etc.).</summary>
        [MaxLength(100)]
        [Column("entidad_id")]
        public string? entidad_id { get; set; }

        /// <summary>JSON o texto libre con el detalle de la operación.</summary>
        [Column("detalles", TypeName = "text")]
        public string? detalles { get; set; }

        /// <summary>IP del cliente que originó la solicitud.</summary>
        [MaxLength(45)]
        [Column("ip_origen")]
        public string? ip_origen { get; set; }

        [Column("fecha_hora")]
        public DateTime fecha_hora { get; set; } = DateTime.UtcNow;
    }
}
