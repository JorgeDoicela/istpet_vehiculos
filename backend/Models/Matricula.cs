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
        [MaxLength(15)]
        public string idAlumno { get; set; } = string.Empty;

        [ForeignKey("idAlumno")]
        public Estudiante? Estudiante { get; set; }

        public int idNivel { get; set; }

        [ForeignKey("idNivel")]
        public Curso? Curso { get; set; }

        public int idSeccion { get; set; }

        public int idModalidad { get; set; }

        [Required]
        [MaxLength(10)]
        public string idPeriodo { get; set; } = string.Empty;

        public DateTime? fechaMatricula { get; set; }

        [MaxLength(10)]
        public string? paralelo { get; set; }

        public bool? arrastres { get; set; }

        public int? folio { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? beca_matricula { get; set; }

        public bool? retirado { get; set; }
        
        public DateTime? fechaRetiro { get; set; }

        [MaxLength(100)]
        public string? observacion { get; set; }

        public bool? convalidacion { get; set; }

        [MaxLength(200)]
        public string? carrera_convalidada { get; set; }

        public int? numero_permiso { get; set; }

        [MaxLength(20)]
        public string? user_matricula { get; set; }

        public int valida { get; set; } = 1;

        public bool esOyente { get; set; } = false;

        [MaxLength(14)]
        public string? documentoFactura { get; set; }

        // --- Logistics Operational Status ---
        [Column(TypeName = "decimal(10,2)")]
        public decimal horas_completadas { get; set; } = 0;
        
        [MaxLength(20)]
        public string estado { get; set; } = "ACTIVO";
    }
}
