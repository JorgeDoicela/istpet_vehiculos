using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Curso Model: Absolute SIGAFI Parity 2026.
     * Aligned with table 'cursos'.
     */
    public class Curso
    {
        [Key]
        public int idNivel { get; set; }

        public int idCarrera { get; set; }

        [MaxLength(20)]
        [Column("Nivel")]
        public string? Nivel { get; set; }

        public int? jerarquia { get; set; }
        public int? orden { get; set; }
        public byte? esRecuperacion { get; set; }

        [MaxLength(10)]
        public string? aliasCurso { get; set; }
    }
}
