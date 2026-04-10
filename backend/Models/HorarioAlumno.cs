using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Student Schedule Model: Absolute SIGAFI Parity.
     * Maps to sigafi_es.cond_alumnos_horarios
     */
    public class HorarioAlumno
    {
        [Key]
        public int idAsignacionHorario { get; set; }

        public int idAsignacion { get; set; }

        public int idFecha { get; set; }

        public int idHora { get; set; }

        public byte asiste { get; set; } = 0;

        public byte activo { get; set; } = 1;

        [MaxLength(100)]
        public string? observacion { get; set; }

        // Mapped helpers (Dynamic hydration from sigafi_es)
        [NotMapped]
        public string? DescripcionHora { get; set; }
        
        [NotMapped]
        public DateTime? FechaReal { get; set; }
    }
}
