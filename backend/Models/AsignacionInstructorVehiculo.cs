using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Instructor-Vehicle Assignment Model: Absolute SIGAFI Parity.
     * Maps to sigafi_es.asignacion_instructores_vehiculos
     */
    public class AsignacionInstructorVehiculo
    {
        [Key]
        public int idAsignacion { get; set; }

        public int idVehiculo { get; set; }

        [MaxLength(15)]
        public string idProfesor { get; set; } = string.Empty;

        public DateTime? fecha_asignacion { get; set; }

        public DateTime? fecha_salida { get; set; }

        public bool activo { get; set; }

        [MaxLength(20)]
        public string? usuario_asigna { get; set; }

        [MaxLength(20)]
        public string? usuario_desactiva { get; set; }

        [MaxLength(255)]
        public string? observacion { get; set; }

        // Navigation Properties
        [ForeignKey("idVehiculo")]
        public Vehiculo? Vehiculo { get; set; }

        [ForeignKey("idProfesor")]
        public Instructor? Profesor { get; set; }
    }
}
