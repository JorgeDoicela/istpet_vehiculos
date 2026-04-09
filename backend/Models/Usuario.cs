using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Usuario Model: Absolute SIGAFI Parity 2026.
     * Aligned with SIGAFI 'usuarios_web' table schema.
     */
    public class Usuario
    {
        [Key]
        [MaxLength(50)]
        public string usuario { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)] 
        public string password { get; set; } = string.Empty;

        public bool salida { get; set; } = false;

        public bool ingreso { get; set; } = false;

        public bool activo { get; set; } = true;

        public bool asistencia { get; set; } = false;

        public bool esRrhh { get; set; } = false;

        // Local Auxiliary Fields (NOT in SIGAFI DB)
        [NotMapped]
        public string? rol { get; set; } 

        [NotMapped]
        public string? nombre_completo { get; set; }

        [NotMapped]
        public DateTime creado_en { get; set; } = DateTime.Now;
    }
}
