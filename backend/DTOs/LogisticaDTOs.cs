namespace backend.DTOs
{
    public class EstudianteLogisticaResponse
    {
        public string Cedula { get; set; } = string.Empty;
        public string EstudianteNombre { get; set; } = string.Empty;
        public string CursoDetalle { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public string Paralelo { get; set; } = string.Empty;
        public string Jornada { get; set; } = string.Empty;
        public string TipoLicencia { get; set; } = string.Empty; // C, D, E
        public int IdTipoLicencia { get; set; } // Identificador numérico para filtrado
        public int IdMatricula { get; set; }

        // --- Detección de Práctica Central (Proactivo) ---
        public int? IdPracticaCentral { get; set; }
        public string? PracticaVehiculo { get; set; }
        public string? PracticaInstructor { get; set; }
        public string? PracticaHora { get; set; }
        public bool TienePracticaHoy => IdPracticaCentral.HasValue;
        public string? FotoBase64 { get; set; }
        public bool IsBusy { get; set; }
    }

    public class VehiculoLogisticaResponse
    {
        public int IdVehiculo { get; set; }
        public int NumeroVehiculo { get; set; }
        public string VehiculoStr { get; set; } = string.Empty;
        public int IdInstructorFijo { get; set; }
        public string InstructorNombre { get; set; } = string.Empty;
        public int IdTipoLicencia { get; set; } // Identificador numérico para filtrado
    }

    public class InstructorLogisticaResponse
    {
        public int Id_Instructor { get; set; }
        public string FullName { get; set; } = string.Empty;
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
        public string? Observaciones { get; set; }
        public int RegistradoPor { get; set; } = 1; // Default admin
    }
}
