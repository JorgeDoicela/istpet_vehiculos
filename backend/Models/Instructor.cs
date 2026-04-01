using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Instructor
    {
        [Key]
        public int Id_Instructor { get; set; }

        [Required]
        [MaxLength(15)]
        public string Cedula { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? Telefono { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class InstructorLicencia
    {
        public int Id_Instructor { get; set; }
        public int Id_Tipo_Licencia { get; set; }
        public DateTime? FechaObtencion { get; set; }

        [ForeignKey("Id_Instructor")]
        public virtual Instructor? Instructor { get; set; }

        [ForeignKey("Id_Tipo_Licencia")]
        public virtual TipoLicencia? TipoLicencia { get; set; }
    }
}
