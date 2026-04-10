using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Modalidad Model: SIGAFI Parity 2026.
     * Aligned with SIGAFI 'modalidades' table schema.
     */
    public class Modalidad
    {
        [Key]
        public int idModalidad { get; set; }

        [MaxLength(100)]
        public string? modalidad { get; set; }

        [MaxLength(1)]
        public string? sufijo { get; set; }
    }
}
