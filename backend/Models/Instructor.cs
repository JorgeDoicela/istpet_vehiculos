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
        [MaxLength(14)]
        public string idProfesor { get; set; } = string.Empty;

        [MaxLength(1)]
        public string? tipodocumento { get; set; } = "C";

        [MaxLength(60)]
        public string? apellidos { get; set; }

        [MaxLength(60)]
        public string? nombres { get; set; }

        [MaxLength(60)]
        public string? primerApellido { get; set; }

        [MaxLength(60)]
        public string? segundoApellido { get; set; }

        [MaxLength(60)]
        public string? primerNombre { get; set; }

        [MaxLength(60)]
        public string? segundoNombre { get; set; }


        [MaxLength(100)]
        public string? direccion { get; set; }

        [MaxLength(125)]
        public string? callePrincipal { get; set; }

        [MaxLength(125)]
        public string? calleSecundaria { get; set; }

        [MaxLength(45)]
        public string? numeroCasa { get; set; }

        [MaxLength(30)]
        public string? telefono { get; set; }

        [MaxLength(20)]
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

        [MaxLength(5)]
        public string? abreviatura_post { get; set; }

        public int activo { get; set; } = 1;

        [MaxLength(255)]
        public string? emailInstitucional { get; set; }

        public DateTime? fecha_ingreso { get; set; }

        public DateTime? fechaIngresoIess { get; set; }

        public DateTime? fecha_retiro { get; set; }


        [MaxLength(5)]
        public string tipoSangre { get; set; } = "S/N";

        [MaxLength(255)]
        public string? foto { get; set; }

        public int esReal { get; set; } = 1;
    }
}
