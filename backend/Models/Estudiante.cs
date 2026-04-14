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
        [MaxLength(14)]
        public string idAlumno { get; set; } = string.Empty;

        [MaxLength(1)]
        public string? tipoDocumento { get; set; } = "C";

        [MaxLength(30)]
        public string? apellidoPaterno { get; set; }

        [MaxLength(30)]
        public string? apellidoMaterno { get; set; }

        [MaxLength(30)]
        public string? primerNombre { get; set; }

        [MaxLength(30)]
        public string? segundoNombre { get; set; }

        public DateTime? fecha_Nacimiento { get; set; }

        [MaxLength(30)]
        public string? ciudad_Nacimiento { get; set; }

        [MaxLength(40)]
        public string? provincia_Nacimiento { get; set; }

        public byte[]? foto { get; set; }

        [MaxLength(50)]
        public string? nacionalidad { get; set; }

        [MaxLength(100)]
        public string? parroquia_nacimiento { get; set; }

        [MaxLength(150)]
        public string? nombre_padre { get; set; }

        [MaxLength(150)]
        public string? ocupacion_padre { get; set; }

        [MaxLength(30)]
        public string? nacionalidad_padre { get; set; }

        [MaxLength(150)]
        public string? nombre_madre { get; set; }

        [MaxLength(150)]
        public string? ocupacion_madre { get; set; }

        [MaxLength(150)]
        public string? nacionalidad_madre { get; set; }

        [MaxLength(150)]
        public string? barrio_residencia { get; set; }

        [MaxLength(150)]
        public string? parroquia_residencia { get; set; }

        [MaxLength(100)]
        public string? ciudad_residencia { get; set; }

        [MaxLength(60)]
        public string? direccion { get; set; }

        [MaxLength(20)]
        public string? telefono { get; set; }

        [MaxLength(20)]
        public string? celular { get; set; }

        [MaxLength(40)]
        public string? email { get; set; }

        [MaxLength(1)]
        public string? sexo { get; set; }

        public int? idNivel { get; set; }

        [MaxLength(7)]
        public string? idPeriodo { get; set; }

        public int? idSeccion { get; set; }

        public int? idModalidad { get; set; }

        public int? idInstitucion { get; set; }

        [MaxLength(200)]
        public string? tituloColegio { get; set; }

        public DateTime? fecha_Inscripcion { get; set; }

        [MaxLength(6)]
        public string? tipo_sangre { get; set; }

        [MaxLength(20)]
        public string? user_alumno { get; set; }

        [MaxLength(20)]
        public string? password { get; set; }

        public int? idDiscapacidad { get; set; }

        public int? idEtnia { get; set; }

        public int? idNacionalidad { get; set; }

        public int? porcentaje_discapacidad { get; set; }

        [MaxLength(20)]
        public string? carnet_conadis { get; set; }

        [MaxLength(100)]
        public string? email_institucional { get; set; }

        public int primerIngreso { get; set; } = 1;

    }
}
