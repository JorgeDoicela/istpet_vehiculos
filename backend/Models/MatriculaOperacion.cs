using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class MatriculaOperacion
    {
        [Key]
        public int idMatricula { get; set; }

        public decimal horas_completadas { get; set; } = 0;

        [MaxLength(20)]
        public string estado { get; set; } = "ACTIVO";
    }
}
