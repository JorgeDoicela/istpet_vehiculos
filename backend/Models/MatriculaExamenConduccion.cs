using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /// <summary>
    /// Espejo SIGAFI <c>matriculas_examen_conduccion</c> (camelCase en BD).
    /// </summary>
    public class MatriculaExamenConduccion
    {
        public int idMatricula { get; set; }

        [Column("idCategoria")]
        public int IdCategoria { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal? nota { get; set; }

        public string? observacion { get; set; }

        [MaxLength(50)]
        public string? usuario { get; set; }

        [Column(TypeName = "date")]
        public DateTime? fechaExamen { get; set; }

        public DateTime? fechaIngreso { get; set; }

        [MaxLength(80)]
        public string? instructor { get; set; }
    }
}
