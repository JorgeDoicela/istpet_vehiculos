using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * HoraClase Model: Absolute SIGAFI Parity 2026.
     * Aligned with SIGAFI 'horas_clases' table.
     */
    [Table("horas_clases")]
    public class HoraClase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int idhora { get; set; }

        public int idSeccion { get; set; }

        public int idCarrera { get; set; }

        public TimeSpan? hora_inicio { get; set; }

        public TimeSpan? hora_fin { get; set; }

        public int minutos { get; set; }

        public int numero_hora { get; set; }

        [MaxLength(1)]
        public string? tipo { get; set; }

        public byte activo { get; set; } = 1;
    }
}
