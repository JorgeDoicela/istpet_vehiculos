using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Academic Period Model: SIGAFI Parity 2026.
     * Aligned with SIGAFI 'periodos' table schema.
     */
    public class Periodo
    {
        [Key]
        [MaxLength(7)]
        public string idPeriodo { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? detalle { get; set; }

        public DateTime? fecha_inicial { get; set; }
        public DateTime? fecha_final { get; set; }

        public bool cerrado { get; set; } = false;
        public bool activo { get; set; } = true;
        
        public bool permiteMatricula { get; set; } = false;
    }
}
