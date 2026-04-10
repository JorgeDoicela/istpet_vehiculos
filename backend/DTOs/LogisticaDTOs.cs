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
        /// <summary>Línea tipo SIGAFI: carrera/nivel, paralelo, sección y jornada (cuadro blanco central).</summary>
        public string detalleMatriculaSigafi { get; set; } = string.Empty;
        public string idPeriodo { get; set; } = string.Empty;
        public string paralelo { get; set; } = string.Empty;
        public string jornada { get; set; } = string.Empty;
        public string tipoLicencia { get; set; } = string.Empty;
        public int idTipoLicencia { get; set; }
        public int idMatricula { get; set; }

        public int? idPracticaCentral { get; set; }
        public string? idPracticaInstructor { get; set; }
        public string? practicaVehiculo { get; set; }
        public string? practicaInstructor { get; set; }
        public string? practicaHora { get; set; }
        public string? horarioProximo { get; set; } // Ej: "14:00 - 15:00"
        public int? idAsignacionHorario { get; set; }
        public bool asistenciaHoy { get; set; }
        public bool tienePracticaHoy => idPracticaCentral.HasValue;
        public string? fotoBase64 { get; set; }
        public bool isBusy { get; set; }
    }

    public class VehiculoLogisticaResponse
    {
        public int idVehiculo { get; set; }
        public string? numeroVehiculo { get; set; }
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

    public class AlumnoSugerenciaLogisticaDto
    {
        public string idAlumno { get; set; } = string.Empty;
        public string nombreCompleto { get; set; } = string.Empty;
        public bool esAgendado { get; set; }
        public bool isBusy { get; set; }
        /// <summary>Próxima práctica abierta en SIGAFI (hoy o futura, sin llegada), para sugerencias fuera del top de agenda.</summary>
        public string? horaAgenda { get; set; }
        public string? vehiculoAgenda { get; set; }
        public string? instructorAgenda { get; set; }
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
        public int cancelado { get; set; }
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

    public class CentralPeriodoDto
    {
        public string idPeriodo { get; set; } = string.Empty;
        public string? detalle { get; set; }
        public DateTime? fecha_inicial { get; set; }
        public DateTime? fecha_final { get; set; }
        public int cerrado { get; set; }
        public DateTime? fecha_maxima_autocierre { get; set; }
        public int activo { get; set; }
        public int creditos { get; set; }
        public int numero_pagos { get; set; }
        public DateTime? fecha_matrucla_extraordinaria { get; set; }
        public int? foliop { get; set; }
        public int permiteMatricula { get; set; }
        public int ingresoCalificaciones { get; set; }
        public int permiteCalificacionesInstituto { get; set; }
        public int periodoactivoinstituto { get; set; }
        public int visualizaPowerBi { get; set; }
        public int esInstituto { get; set; }
        public int periodoPlanificacion { get; set; }
    }

    public class CentralCarreraDto
    {
        public int idCarrera { get; set; }
        public string? Carrera { get; set; }
        public DateTime? fechaCreacion { get; set; }
        public int activa { get; set; }
        public string? directorCarrera { get; set; }
        public int? numero_creditos { get; set; }
        public int ordenCarrera { get; set; }
        public int? numero_alumnos { get; set; }
        public int revisaArrastres { get; set; }
        public string? codigo_cases { get; set; }
        public string? aliasCarrera { get; set; }
        public int esInstituto { get; set; }
    }

    public class CentralSeccionDto
    {
        public int idSeccion { get; set; }
        public string? seccion { get; set; }
        public string? sufijo { get; set; }
    }

    public class CentralModalidadDto
    {
        public int idModalidad { get; set; }
        public string? modalidad { get; set; }
        public string? sufijo { get; set; }
    }

    public class CentralInstitucionDto
    {
        public int idInstitucion { get; set; }
        public string? Institucion { get; set; }
        public string? ciudad { get; set; }
        public string? provincia { get; set; }
    }

    public class CentralFechaHorarioDto
    {
        public int idFecha { get; set; }
        public DateTime fecha { get; set; }
        public int finsemana { get; set; }
        public string? dia { get; set; }
    }

    public class CentralHorarioProfesorDto
    {
        public int idHorario { get; set; }
        public int idAsignacion { get; set; }
        public int idHora { get; set; }
        public int idFecha { get; set; }
        public int asiste { get; set; }
        public int activo { get; set; }
    }

}
