using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Estudiante Model: Absolute SIGAFI Parity 2026.
     * Comprehensive alignment with SIGAFI 'alumnos' table schema.
     */
    public class Estudiante
    {
        [Key]
        [MaxLength(15)]
        public string idAlumno { get; set; } = string.Empty;

        [MaxLength(1)]
        public string? tipoDocumento { get; set; } = "C";

        [Required]
        [MaxLength(80)]
        public string apellidoPaterno { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? apellidoMaterno { get; set; }

        [Required]
        [MaxLength(80)]
        public string primerNombre { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? segundoNombre { get; set; }

        public DateTime? fecha_Nacimiento { get; set; }

        [MaxLength(60)]
        public string? direccion { get; set; }

        [MaxLength(20)]
        public string? telefono { get; set; }

        [MaxLength(50)]
        public string? celular { get; set; }

        [MaxLength(100)]
        public string? email { get; set; }

        [MaxLength(1)]
        public string? sexo { get; set; }

        [MaxLength(50)]
        public string? nacionalidad { get; set; }

        public int? idNivel { get; set; }

        [MaxLength(7)]
        public string? idPeriodo { get; set; }

        public int? idSeccion { get; set; }
        
        public int? idModalidad { get; set; }

        public int? idInstitucion { get; set; }

        [MaxLength(200)]
        public string? tituloColegio { get; set; }

        public DateTime? fecha_Inscripcion { get; set; }

        [MaxLength(150)]
        public string? nombre_padre { get; set; }

        [MaxLength(100)]
        public string? ciudad_residencia { get; set; }

        [MaxLength(6)]
        public string? tipo_sangre { get; set; }

        // Security / Identity (SIGAFI Simulation)
        [MaxLength(20)]
        public string? user_alumno { get; set; }

        [MaxLength(20)]
        public string? password { get; set; }

        [MaxLength(100)]
        public string? email_institucional { get; set; }

        public bool activo { get; set; } = true;
    }
}
