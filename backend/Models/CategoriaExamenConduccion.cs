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

        [Required]
        [MaxLength(100)]
        public string categoria { get; set; } = string.Empty;

        public bool tieneNota { get; set; }

        public bool activa { get; set; }
    }
}
