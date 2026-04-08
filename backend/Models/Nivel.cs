using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Nivel Model: Absolute SIGAFI Parity 2026.
     * Aligned with table 'niveles'.
     */
    public class Nivel
    {
        [Key]
        public int idNivel { get; set; }

        public int idCarrera { get; set; }

        [MaxLength(20)]
        public string? NivelNombre { get; set; } // Renamed to avoid collision with class name

        public int? jerarquia { get; set; }
        public int? orden { get; set; }
        public byte? esRecuperacion { get; set; }
        
        [MaxLength(10)]
        public string? aliasCurso { get; set; }
    }
}
