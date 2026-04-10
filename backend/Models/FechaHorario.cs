using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * FechaHorario Model: Absolute SIGAFI Parity 2026.
     * Aligned with SIGAFI 'fechas_horarios' table.
     */
    [Table("fechas_horarios")]
    public class FechaHorario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int idFecha { get; set; }

        public DateTime fecha { get; set; }

        public byte finsemana { get; set; }

        [MaxLength(10)]
        public string? dia { get; set; }
    }
}
