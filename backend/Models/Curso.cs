using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Curso
    {
        [Key]
        public int Id_Curso { get; set; }

        public int IdTipoLicencia { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Nivel { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Paralelo { get; set; } = string.Empty;

        public string Jornada { get; set; } = "MATUTINA";

        [Required]
        [MaxLength(20)]
        public string Periodo { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public int CupoMaximo { get; set; } = 20;
        public int CuposDisponibles { get; set; } = 20;
        public int HorasPracticaTotal { get; set; } = 15;
        public string Estado { get; set; } = "ACTIVO";

        // Navigation Properties
        [ForeignKey("IdTipoLicencia")]
        public virtual TipoLicencia? TipoLicencia { get; set; }
    }
}
