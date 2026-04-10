using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Logistics / Operational Fields (Local Augmentation)
        public int id_tipo_licencia { get; set; } = 1; // Default to Type C

        [ForeignKey("id_tipo_licencia")]
        public TipoLicencia? TipoLicencia { get; set; }

        public string? id_instructor_fijo { get; set; } // FK to idProfesor

        [ForeignKey("id_instructor_fijo")]
        public Instructor? InstructorFijo { get; set; }

        [MaxLength(50)]
        public string estado_mecanico { get; set; } = "OPERATIVO";
    }
}
