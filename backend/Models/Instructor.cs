using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Instructor Model: Absolute SIGAFI Parity 2026.
     * Comprehensive alignment with SIGAFI 'profesores' table schema.
     */
    public class Instructor
    {
        [Key]
        [MaxLength(15)]
        public string idProfesor { get; set; } = string.Empty;

        [MaxLength(1)]
        public string? tipodocumento { get; set; } = "C";

        [Required]
        [MaxLength(80)]
        public string primerApellido { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? segundoApellido { get; set; }

        [Required]
        [MaxLength(80)]
        public string primerNombre { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? segundoNombre { get; set; }

        // Combined fields (Legacy / Search compatibility)
        [MaxLength(160)]
        public string nombres { get; set; } = string.Empty;

        [MaxLength(160)]
        public string apellidos { get; set; } = string.Empty;

        public int estadoCivil { get; set; } = 1;

        [MaxLength(100)]
        public string? direccion { get; set; }

        [MaxLength(30)]
        public string? telefono { get; set; }

        [MaxLength(50)]
        public string? celular { get; set; }

        [MaxLength(100)]
        public string? email { get; set; }

        public DateTime? fecha_nacimiento { get; set; }

        [MaxLength(1)]
        public string? sexo { get; set; }

        [MaxLength(20)]
        public string? clave { get; set; } = "321";

        public int practicas { get; set; } = 0;

        [MaxLength(1)]
        public string? tipo { get; set; } = "P";

        [MaxLength(200)]
        public string? titulo { get; set; }

        [MaxLength(5)]
        public string? abreviatura { get; set; }

        [MaxLength(255)]
        public string? emailInstitucional { get; set; }

        [MaxLength(5)]
        public string? tipoSangre { get; set; }

        [MaxLength(255)]
        public string? foto { get; set; }

        public bool activo { get; set; } = true;

        [MaxLength(50)]
        public string? nacionalidad { get; set; }
    }
}
