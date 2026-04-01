using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class TipoLicencia
    {
        [Key]
        public int Id_Tipo { get; set; }

        [Required]
        [MaxLength(5)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Descripcion { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}
