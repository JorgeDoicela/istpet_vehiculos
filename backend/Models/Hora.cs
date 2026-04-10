using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Hora Model: Absolute SIGAFI Parity 2026.
     * Aligned with SIGAFI 'horas' table.
     */
    [Table("horas")]
    public class Hora
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int idHora { get; set; }

        [MaxLength(100)]
        public string? detalle { get; set; }
    }
}
