using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Asignacion Model: Absolute SIGAFI Parity 2026.
     * Aligned with SIGAFI 'Asignaciones' table schema.
     */
    public class Asignacion
    {
        [Key]
        public int idAsignacion { get; set; }

        [Required]
        [MaxLength(14)]
        public string idAlumno { get; set; } = string.Empty;

        public int idVehiculo { get; set; }

        [Required]
        [MaxLength(10)]
        public string idPeriodo { get; set; } = string.Empty;

        [MaxLength(14)]
        public string? idProfesor { get; set; }

        public DateTime fechaAsignacion { get; set; } = DateTime.Now;

        public DateTime? fechaInicio { get; set; }

        public DateTime? fechaFin { get; set; }

        public byte activa { get; set; } = 1;

        [MaxLength(200)]
        public string? observacion { get; set; }

        // Navigation
        [ForeignKey("idAlumno")]
        public virtual Estudiante? Estudiante { get; set; }

        [ForeignKey("idVehiculo")]
        public virtual Vehiculo? Vehiculo { get; set; }

        [ForeignKey("idProfesor")]
        public virtual Instructor? Instructor { get; set; }
    }
}
