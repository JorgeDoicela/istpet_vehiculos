using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Carrera Model: SIGAFI Parity 2026.
     * Aligned with SIGAFI 'carreras' table schema.
     */
    public class Carrera
    {
        [Key]
        public int idCarrera { get; set; }

        [MaxLength(100)]
        [Column("Carrera")]
        public string? NombreCarrera { get; set; }

        public DateTime? fechaCreacion { get; set; }
        public bool activa { get; set; } = true;

        [MaxLength(100)]
        public string? directorCarrera { get; set; }

        public int? numero_creditos { get; set; }
        public int ordenCarrera { get; set; } = 0;
        public int? numero_alumnos { get; set; }
        public bool revisaArrastres { get; set; } = true;

        [MaxLength(20)]
        public string? codigo_cases { get; set; }

        [MaxLength(5)]
        public string? aliasCarrera { get; set; }

        public bool esInstituto { get; set; } = false;
    }
}
