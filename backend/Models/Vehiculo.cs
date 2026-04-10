using System.ComponentModel.DataAnnotations;
namespace backend.Models
{
    /**
     * Vehicle Model: Absolute SIGAFI Parity 2026.
     * Aligned with SIGAFI 'vehiculos' table schema.
     */
    public class Vehiculo
    {
        [Key]
        public int idVehiculo { get; set; }

        public int? idSubcategoria { get; set; }

        [MaxLength(3)]
        public string? numero_vehiculo { get; set; }

        [MaxLength(10)]
        public string? placa { get; set; }

        [MaxLength(100)]
        public string? marca { get; set; }

        public int? anio { get; set; }

        public int? idCategoria { get; set; }

        public int activo { get; set; } = 1;

        [MaxLength(200)]
        public string? observacion { get; set; }

        [MaxLength(50)]
        public string? chasis { get; set; }

        [MaxLength(50)]
        public string? motor { get; set; }

        [MaxLength(100)]
        public string? modelo { get; set; }

    }
}
