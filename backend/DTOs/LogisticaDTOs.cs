namespace backend.DTOs
{
    /**
     * Logistica DTOs: Refactored for Absolute SIGAFI Parity 2026.
     * Guaranteed 1:1 naming with central database for idAlumno, idProfesor, idVehiculo, idPractica.
     */
    public class EstudianteLogisticaResponse
    {
        public string idAlumno { get; set; } = string.Empty;
        public string nombreCompleto { get; set; } = string.Empty;
        public string nivel { get; set; } = string.Empty;
        public string idPeriodo { get; set; } = string.Empty;
        public string paralelo { get; set; } = string.Empty;
        public string jornada { get; set; } = string.Empty;
        public string tipoLicencia { get; set; } = string.Empty;
        public int idTipoLicencia { get; set; }
        public int idMatricula { get; set; }

        public int? idPracticaCentral { get; set; }
        public int? idPracticaInstructor { get; set; }
        public string? practicaVehiculo { get; set; }
        public string? practicaInstructor { get; set; }
        public string? practicaHora { get; set; }
        public string? horarioProximo { get; set; } // Ej: "14:00 - 15:00"
        public bool asistenciaHoy { get; set; }
        public bool tienePracticaHoy => idPracticaCentral.HasValue;
        public string? fotoBase64 { get; set; }
        public bool isBusy { get; set; }
    }

    public class VehiculoLogisticaResponse
    {
        public int idVehiculo { get; set; }
        public int numeroVehiculo { get; set; }
        public string vehiculoStr { get; set; } = string.Empty;
        public string? idInstructorFijo { get; set; }
        public string instructorNombre { get; set; } = string.Empty;
        public int idTipoLicencia { get; set; }
    }

    public class InstructorLogisticaResponse
    {
        public string idInstructor { get; set; } = string.Empty;
        public string fullName { get; set; } = string.Empty;
    }

    public class SalidaRequest
    {
        public int idMatricula { get; set; }
        public int idVehiculo { get; set; }
        public string idInstructor { get; set; } = string.Empty;
        public string? observaciones { get; set; }
        public int registradoPor { get; set; } = 1;
        public int? idAsignacionHorario { get; set; } // Opcional: Para vincular con agenda SIGAFI
    }

    public class LlegadaRequest
    {
        public int idPractica { get; set; }
        public string? observaciones { get; set; }
        public int registradoPor { get; set; } = 1;
    }

    public class ReportePracticasDTO
    {
        public int idPractica { get; set; }
        public string idProfesor { get; set; } = string.Empty;
        public string profesor { get; set; } = string.Empty;
        public string categoria { get; set; } = string.Empty;
        public string numeroVehiculo { get; set; } = string.Empty;
        public string idAlumno { get; set; } = string.Empty;
        public string nomina { get; set; } = string.Empty;
        public string dia { get; set; } = string.Empty;
        public string fecha { get; set; } = string.Empty;
        public string horaSalida { get; set; } = string.Empty;
        public string? horaLlegada { get; set; }
        public string tiempo { get; set; } = string.Empty;
        public string? observaciones { get; set; }
    }

    public class CentralUserDto
    {
        public string usuario { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public int salida { get; set; }
        public int ingreso { get; set; }
        public int activo { get; set; }
        public int asistencia { get; set; }
        public int esRrhh { get; set; }
    }
}
