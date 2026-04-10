using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Vehicle Category Model: SIGAFI Parity.
     * Maps to sigafi_es.categoria_vehiculos
     */
    public class CategoriaVehiculo
    {
        [Key]
        public int idCategoria { get; set; }

        [MaxLength(100)]
        public string? categoria { get; set; }
    }
}
