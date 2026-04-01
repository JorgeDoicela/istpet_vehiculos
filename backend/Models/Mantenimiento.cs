using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Mantenimiento
    {
        [Key]
        public int Id_Mantenimiento { get; set; }

        [Required]
        public int Id_Vehiculo { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public int KmRealizado { get; set; }

        public string? Descripcion { get; set; }

        public decimal Costo { get; set; } = 0;

        // Navigation Property
        [ForeignKey("Id_Vehiculo")]
        public virtual Vehiculo? Vehiculo { get; set; }
    }
}
