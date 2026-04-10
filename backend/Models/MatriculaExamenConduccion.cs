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

        public int? nota { get; set; }

        [MaxLength(100)]
        public string? observacion { get; set; }

        [MaxLength(20)]
        public string? usuario { get; set; }

        [Column(TypeName = "date")]
        public DateTime? fechaExamen { get; set; }

        public DateTime? fechaIngreso { get; set; }

        [MaxLength(100)]
        public string? instructor { get; set; }
    }
}
