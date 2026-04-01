using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Vehiculo
    {
        [Key]
        public int Id_Vehiculo { get; set; }

        public int NumeroVehiculo { get; set; }

        [Required]
        [MaxLength(15)]
        public string Placa { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? Marca { get; set; }

        [MaxLength(80)]
        public string? Modelo { get; set; }

        public int IdTipoLicencia { get; set; }

        public int IdInstructorFijo { get; set; }

        public int KmActual { get; set; } = 0;

        public string EstadoMecanico { get; set; } = "OPERATIVO";

        public int? KmProximoMantenimiento { get; set; }

        public bool Activo { get; set; } = true;

        // Navigation Properties (Enterprise approach)
        [ForeignKey("IdTipoLicencia")]
        public virtual TipoLicencia? TipoLicencia { get; set; }

        [ForeignKey("IdInstructorFijo")]
        public virtual Instructor? InstructorFijo { get; set; }
    }
}
