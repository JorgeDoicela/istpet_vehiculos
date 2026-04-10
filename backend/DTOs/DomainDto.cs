namespace backend.DTOs
{
    /**
     * Domain DTOs: Absolute Parity with SIGAFI naming 2026.
     */
    public class EstudianteDto
    {
        public string idAlumno { get; set; } = string.Empty;
        public string primerNombre { get; set; } = string.Empty;
        public string segundoNombre { get; set; } = string.Empty;
        public string apellidoPaterno { get; set; } = string.Empty;
        public string apellidoMaterno { get; set; } = string.Empty;
        public string? email { get; set; }
        public string? celular { get; set; }
        
        public string nombreCompleto => $"{apellidoPaterno} {apellidoMaterno} {primerNombre} {segundoNombre}".Trim();
    }

    public class InstructorDto
    {
        public string idProfesor { get; set; } = string.Empty;
        public string primerNombre { get; set; } = string.Empty;
        public string segundoNombre { get; set; } = string.Empty;
        public string primerApellido { get; set; } = string.Empty;
        public string segundoApellido { get; set; } = string.Empty;
        public string? nombres { get; set; }
        public string? apellidos { get; set; }
        public string? email { get; set; }
        public string? celular { get; set; }
        public bool activo { get; set; }

        public string nombreCompleto => !string.IsNullOrWhiteSpace(nombres) && !string.IsNullOrWhiteSpace(apellidos) 
            ? $"{apellidos} {nombres}".Trim()
            : $"{primerApellido} {segundoApellido} {primerNombre} {segundoNombre}".Trim();
    }

    public class VehiculoDto
    {
        public int idVehiculo { get; set; }
        public string placa { get; set; } = string.Empty;
        public string numero_vehiculo { get; set; } = string.Empty;
        public string marca { get; set; } = string.Empty;
        public string modelo { get; set; } = string.Empty;
        public int? anio { get; set; }
        public bool activo { get; set; }
    }
}
