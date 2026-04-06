namespace backend.DTOs
{
    public class EstudianteLogisticaResponse
    {
        public string Cedula { get; set; } = string.Empty;
        public string EstudianteNombre { get; set; } = string.Empty;
        public string CursoDetalle { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public int IdMatricula { get; set; }
    }

    public class VehiculoLogisticaResponse
    {
        public int IdVehiculo { get; set; }
        public string VehiculoStr { get; set; } = string.Empty;
        public int IdInstructorFijo { get; set; }
        public string InstructorNombre { get; set; } = string.Empty;
        public int KmActual { get; set; }
    }

    public class SalidaRequest
    {
        public int IdMatricula { get; set; }
        public int IdVehiculo { get; set; }
        public int IdInstructor { get; set; }
        public string? Observaciones { get; set; }
        public int RegistradoPor { get; set; } = 1; // Default admin
    }

    public class LlegadaRequest
    {
        public int IdRegistro { get; set; }
        public int KmLlegada { get; set; }
        public string? Observaciones { get; set; }
        public int RegistradoPor { get; set; } = 1; // Default admin
    }
}
