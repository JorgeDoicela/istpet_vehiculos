using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Usuario Model: Absolute SIGAFI Parity 2026.
     * Aligned with SIGAFI 'usuario' table schema.
     */
    public class Usuario
    {
        [Key]
        [MaxLength(20)]
        public string usuario { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)] // Hashed
        public string password { get; set; } = string.Empty;

        public bool salida { get; set; } = false;

        public bool ingreso { get; set; } = false;

        public bool activo { get; set; } = true;

        public bool asistencia { get; set; } = false;

        public bool esRrhh { get; set; } = false;

        // Local Augmentation (Keep for display/JWT)
        public string? rol { get; set; } // Derived from salida/ingreso/esRrhh
        public string? nombre_completo { get; set; }
        public DateTime creado_en { get; set; } = DateTime.Now;
    }
}
