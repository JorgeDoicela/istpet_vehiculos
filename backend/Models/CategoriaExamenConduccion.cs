using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Conduct Exam Category Model: SIGAFI Parity.
     * Maps to sigafi_es.categorias_examenes_conduccion
     */
    public class CategoriaExamenConduccion
    {
        [Key]
        public int IdCategoria { get; set; }

        [MaxLength(100)]
        public string? categoria { get; set; }

        public byte tieneNota { get; set; } = 0;

        public byte activa { get; set; } = 1;
    }
}
