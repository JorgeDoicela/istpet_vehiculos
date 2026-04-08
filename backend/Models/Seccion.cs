using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Section Model: SIGAFI Parity 2026.
     * Aligned with SIGAFI 'secciones' table schema.
     */
    public class Seccion
    {
        [Key]
        public int idSeccion { get; set; }

        [MaxLength(30)]
        public string? seccion { get; set; }

        [MaxLength(1)]
        public string? sufijo { get; set; }
    }
}
