using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Enrollment Model: Absolute SIGAFI Parity 2026.
     * Aligned with SIGAFI 'matriculas' table schema.
     */
    public class Matricula
    {
        [Key]
        public int idMatricula { get; set; }

        [Required]
        [MaxLength(14)]
        public string idAlumno { get; set; } = string.Empty;

        [ForeignKey("idAlumno")]
        public Estudiante? Estudiante { get; set; }

        public int idNivel { get; set; }

        [ForeignKey("idNivel")]
        public Nivel? Nivel { get; set; }

        public int idSeccion { get; set; }

        public int idModalidad { get; set; }

        [Required]
        [MaxLength(7)]
        public string idPeriodo { get; set; } = string.Empty;

        public DateTime? fechaMatricula { get; set; }

        [MaxLength(10)]
        public string? paralelo { get; set; }

        public bool? arrastres { get; set; }

        public int? folio { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? beca_matricula { get; set; }

        public bool? retirado { get; set; }

        public int valida { get; set; } = 1;

        public bool esOyente { get; set; } = false;

        // --- Logistics Operational Status ---
        public int horas_completadas { get; set; } = 0;
        
        [MaxLength(20)]
        public string estado { get; set; } = "ACTIVO";
    }
}
