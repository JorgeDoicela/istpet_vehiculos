using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Matricula
    {
        [Key]
        public int Id_Matricula { get; set; }

        [Required]
        [MaxLength(15)]
        public string CedulaEstudiante { get; set; } = string.Empty;

        public int IdCurso { get; set; }

        public DateTime FechaMatricula { get; set; } = DateTime.Now;

        public decimal HorasCompletadas { get; set; } = 0.00m;

        public string Estado { get; set; } = "ACTIVO";

        // Navigation Properties
        [ForeignKey("CedulaEstudiante")]
        public virtual Estudiante? Estudiante { get; set; }

        [ForeignKey("IdCurso")]
        public virtual Curso? Curso { get; set; }
    }
}
