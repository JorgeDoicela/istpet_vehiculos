using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Practica Model: Absolute SIGAFI Parity 2026.
     * Aligned with table 'cond_alumnos_practicas'.
     */
    public class Practica
    {
        [Key]
        public int idPractica { get; set; }

        [Required]
        [MaxLength(14)]
        public string idalumno { get; set; } = string.Empty;

        public int idvehiculo { get; set; }

        [Required]
        [MaxLength(14)]
        public string idProfesor { get; set; } = string.Empty;

        [Required]
        [MaxLength(7)]
        public string idPeriodo { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? dia { get; set; }

        public DateTime fecha { get; set; }

        public TimeSpan? hora_salida { get; set; }

        public TimeSpan? hora_llegada { get; set; }

        public TimeSpan? tiempo { get; set; }

        public byte? ensalida { get; set; } = 0;

        public byte? verificada { get; set; } = 0;

        [MaxLength(20)]
        public string? user_asigna { get; set; }

        [MaxLength(20)]
        public string? user_llegada { get; set; }

        public byte? cancelado { get; set; } = 0;
        public string? observaciones { get; set; }

        // Navigation (Local Logic)
        [ForeignKey("idalumno")]
        public virtual Estudiante? Estudiante { get; set; }

        [ForeignKey("idvehiculo")]
        public virtual Vehiculo? Vehiculo { get; set; }

        [ForeignKey("idProfesor")]
        public virtual Instructor? Instructor { get; set; }
    }
}
