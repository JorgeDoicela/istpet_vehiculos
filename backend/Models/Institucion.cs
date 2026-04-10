using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Institucion Model: SIGAFI Parity 2026.
     * Aligned with SIGAFI 'instituciones' table schema.
     */
    public class Institucion
    {
        [Key]
        public int idInstitucion { get; set; }

        [MaxLength(200)]
        public string? Institucion { get; set; }

        [MaxLength(100)]
        public string? ciudad { get; set; }

        [MaxLength(100)]
        public string? provincia { get; set; }
    }
}
