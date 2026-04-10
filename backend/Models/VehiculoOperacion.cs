using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class VehiculoOperacion
    {
        [Key]
        public int idVehiculo { get; set; }

        public int? id_tipo_licencia { get; set; }

        [MaxLength(14)]
        public string? id_instructor_fijo { get; set; }

        [MaxLength(30)]
        public string estado_mecanico { get; set; } = "OPERATIVO";
    }
}
