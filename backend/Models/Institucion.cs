using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Column("Institucion")]
        public string? NombreInstitucion { get; set; }

        [MaxLength(100)]
        public string? ciudad { get; set; }

        [MaxLength(100)]
        public string? provincia { get; set; }
    }
}
