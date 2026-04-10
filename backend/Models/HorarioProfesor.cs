using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * HorarioProfesor Model: Absolute SIGAFI Parity 2026.
     * Aligned with SIGAFI 'horario_profesores' table.
     */
    [Table("horario_profesores")]
    public class HorarioProfesor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int idHorario { get; set; }

        public int idAsignacion { get; set; }

        public int idHora { get; set; }

        public int idFecha { get; set; }

        public byte asiste { get; set; } = 0;

        public byte activo { get; set; } = 1;
    }
}
