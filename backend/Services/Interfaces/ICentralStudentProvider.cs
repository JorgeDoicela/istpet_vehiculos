using backend.DTOs;
using backend.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /**
     * Interface for the ISTPET Centralized Student Database Bridge.
     * Updated 2026: Absolute SIGAFI Naming Parity.
     */
    public class CentralStudentDto
    {
        public string idAlumno { get; set; } = string.Empty;
        public string? primerNombre { get; set; }
        public string? apellidoPaterno { get; set; }
        public string? apellidoMaterno { get; set; }
        public string? segundoNombre { get; set; }
        public string? paralelo { get; set; }

        /// <summary>Texto de jornada desde SIGAFI: <c>secciones.seccion</c> (MATUTINA, VESPERTINA, etc.).</summary>
        public string? seccion { get; set; }

        public string? Nivel { get; set; }
        public string idPeriodo { get; set; } = string.Empty;
        public byte[]? foto { get; set; }

        /// <summary>idNivel de la matrícula SIGAFI (cursos.idNivel).</summary>
        public int idNivel { get; set; }

        /// <summary>idModalidad en <c>matriculas</c>; respaldo si no hay texto en <see cref="seccion"/>.</summary>
        public int idModalidad { get; set; }

        /// <summary>Etiqueta desde tabla modalidades solo si <see cref="seccion"/> viene vacía (respaldo).</summary>
        public string? JornadaSigafi { get; set; }

        /// <summary><c>matriculas.idSeccion</c> → <c>secciones</c>.</summary>
        public int idSeccion { get; set; }

        // --- Helper for UI Compatibility ---
        public string? NombreCompleto { get; set; } 
        public string? DetalleRaw { get; set; }   
        public string? FotoBase64 { get; set; }
    }

    public class CentralInstructorDto
    {
        public string idProfesor { get; set; } = string.Empty;
        public string nombres { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        
        public string? primerApellido { get; set; }
        public string? segundoApellido { get; set; }
        public string? primerNombre { get; set; }
        public string? segundoNombre { get; set; }

        public string? celular { get; set; }
        public string? email { get; set; }
        public int activo { get; set; } 
    }

    public class ScheduledPracticeDto
    {
        public int idPractica { get; set; }
        public string idalumno { get; set; } = string.Empty; 
        public int idvehiculo { get; set; } 
        public string idProfesor { get; set; } = string.Empty;
        public DateTime fecha { get; set; }
        public TimeSpan? hora_salida { get; set; }
        
        public string AlumnoNombre { get; set; } = string.Empty;
        public string VehiculoDetalle { get; set; } = string.Empty;
        public string ProfesorNombre { get; set; } = string.Empty;

        public string EstadoOperativo { get; set; } = "sin_sincronizar";
    }

    public class AgendaLogisticaResponseDto
    {
        public List<ScheduledPracticeDto> Practicas { get; set; } = new();
        public string FuenteDatos { get; set; } = "sigafi";
        public DateTime ObtenidoEn { get; set; }
    }

    /// <summary>Filas de sigafi_es.cond_alumnos_horarios (sin columnas inventadas).</summary>
    public class CentralHorarioDto
    {
        public int idAsignacionHorario { get; set; }
        public int idAsignacion { get; set; }
        public int? idFecha { get; set; }
        public int? idHora { get; set; }
        public int asiste { get; set; }
        public int activo { get; set; }
        public string? observacion { get; set; }
    }

    public class CentralVehiculoDto
    {
        public int idVehiculo { get; set; }
        public int? idSubcategoria { get; set; }
        public string? numero_vehiculo { get; set; }
        public string? placa { get; set; }
        public string? marca { get; set; }
        public int? anio { get; set; }
        public int? idCategoria { get; set; }
        public int activo { get; set; }
        public string? observacion { get; set; }
        public string? chasis { get; set; }
        public string? motor { get; set; }
        public string? modelo { get; set; }
    }

    public class CentralCursoDto
    {
        public int idNivel { get; set; }
        public int idCarrera { get; set; }
        public string? Nivel { get; set; }
        public int? jerarquia { get; set; }
        public int? orden { get; set; }
        public int? esRecuperacion { get; set; }
        public string? aliasCurso { get; set; }
    }

    public class CentralTipoLicenciaDto
    {
        /// <summary>Origen SIGAFI categoria_vehiculos.idCategoria cuando aplica.</summary>
        public int? id_categoria_sigafi { get; set; }

        public int id_tipo { get; set; }
        public string codigo { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public int activo { get; set; }
    }

    public class CentralMatriculaExamenDto
    {
        public int idMatricula { get; set; }
        public int IdCategoria { get; set; }
        public decimal? nota { get; set; }
        public string? observacion { get; set; }
        public string? usuario { get; set; }
        public DateTime? fechaExamen { get; set; }
        public DateTime? fechaIngreso { get; set; }
        public string? instructor { get; set; }
    }

    public class CentralCategoriaVehiculoDto
    {
        public int idCategoria { get; set; }
        public string categoria { get; set; } = string.Empty;
    }

    public class CentralCategoriaExamenDto
    {
        public int IdCategoria { get; set; }
        public string categoria { get; set; } = string.Empty;
        public int tieneNota { get; set; }
        public int activa { get; set; }
    }

    public class CentralMatriculaDto
    {
        public int idMatricula { get; set; }
        public string idAlumno { get; set; } = string.Empty;
        public int idNivel { get; set; }
        public int idSeccion { get; set; }
        public int idModalidad { get; set; }
        public string idPeriodo { get; set; } = string.Empty;
        public DateTime? fechaMatricula { get; set; }
        public string? paralelo { get; set; }
        public int? arrastres { get; set; }
        public int? folio { get; set; }
        public decimal? beca_matricula { get; set; }
        public int? retirado { get; set; }
        public int? esOyente { get; set; }
        public int valida { get; set; }
    }

    public class CentralAsignacionInstructorVehiculoDto
    {
        public int idAsignacion { get; set; }
        public int idVehiculo { get; set; }
        public string idProfesor { get; set; } = string.Empty;
        public DateTime? fecha_asignacion { get; set; }
        public DateTime? fecha_salida { get; set; }
        public int activo { get; set; }
        public string? usuario_asigna { get; set; }
        public string? usuario_desactiva { get; set; }
        public string? observacion { get; set; }
    }

    public class CentralAsignacionAlumnoVehiculoDto
    {
        public int idAsignacion { get; set; }
        public string idAlumno { get; set; } = string.Empty;
        public int idVehiculo { get; set; }
        public string idProfesor { get; set; } = string.Empty;
        public string? idPeriodo { get; set; }
        public DateTime? fechaAsignacion { get; set; }
        public DateTime? fechaInicio { get; set; }
        public DateTime? fechaFin { get; set; }
        public int activa { get; set; }
        public string? observacion { get; set; }
    }

    public class CentralPracticaHorarioDto
    {
        public int idPractica { get; set; }
        public int idAsignacionHorario { get; set; }
    }

    public class CentralPracticaSyncDto
    {
        public int idPractica { get; set; }
        public string idalumno { get; set; } = string.Empty;
        public int idvehiculo { get; set; }
        public string idProfesor { get; set; } = string.Empty;
        public string? idPeriodo { get; set; }
        public string? dia { get; set; }
        public DateTime fecha { get; set; }
        public TimeSpan? hora_salida { get; set; }
        public TimeSpan? hora_llegada { get; set; }
        public TimeSpan? tiempo { get; set; }
        public int ensalida { get; set; }
        public int verificada { get; set; }
        public string? user_asigna { get; set; }
        public string? user_llegada { get; set; }
        public int cancelado { get; set; }
        public string? observaciones { get; set; }
    }

    public class CentralAlumnoLiteDto
    {
        public string idAlumno { get; set; } = string.Empty;
        public string? primerNombre { get; set; }
        public string? segundoNombre { get; set; }
        public string? apellidoPaterno { get; set; }
        public string? apellidoMaterno { get; set; }
        public string? celular { get; set; }
        public string? email { get; set; }

        /// <summary>Campos de ficha en <c>alumnos</c> (SIGAFI); alinean espejo local con matrícula vigente.</summary>
        public string? idPeriodo { get; set; }
        public int? idNivel { get; set; }
        public int? idSeccion { get; set; }
        public int? idModalidad { get; set; }
    }

    public interface ICentralStudentProvider
    {
        Task<CentralStudentDto?> GetFromCentralAsync(string idAlumno);
        Task<CentralInstructorDto?> GetInstructorFromCentralAsync(string idProfesor);
        Task<IEnumerable<CentralInstructorDto>> GetAllInstructorsFromCentralAsync();
        Task<CentralInstructorDto?> GetAssignedTutorAsync(string idAlumno);
        Task<ScheduledPracticeDto?> GetScheduledPracticeAsync(string idAlumno);
        /// <summary>Para cada cédula, la próxima práctica en SIGAFI con fecha ≥ hoy, no cancelada y sin hora de llegada.</summary>
        Task<IReadOnlyDictionary<string, ScheduledPracticeDto>> GetNextOpenPracticesForAlumnosAsync(IEnumerable<string> idAlumnos);
        Task<CentralHorarioDto?> GetNextScheduleAsync(string idAlumno);
        Task<IEnumerable<ScheduledPracticeDto>> GetRecentSchedulesAsync(int limit = 100);
        Task<IEnumerable<CentralUserDto>> GetAllWebUsersAsync();
        /// <summary>Lectura directa de un usuario en SIGAFI (usuarios_web).</summary>
        Task<CentralUserDto?> GetWebUserFromSigafiAsync(string usuario);
        Task<IEnumerable<CentralVehiculoDto>> GetAllVehiclesFromCentralAsync();
        /// <summary>Lectura directa en SIGAFI por placa (fuente de verdad).</summary>
        Task<CentralVehiculoDto?> GetVehicleByPlacaFromCentralAsync(string placa);
        Task<IEnumerable<CentralCursoDto>> GetAllCoursesFromCentralAsync();
        Task<IEnumerable<CentralTipoLicenciaDto>> GetAllLicenseTypesFromCentralAsync();
        Task<IEnumerable<CentralCategoriaVehiculoDto>> GetAllVehicleCategoriesFromCentralAsync();
        Task<IEnumerable<CentralCategoriaExamenDto>> GetAllExamCategoriesFromCentralAsync();
        Task<IEnumerable<CentralAlumnoLiteDto>> GetAllStudentsFromCentralAsync();

        /// <summary>
        /// Busca directamente en SIGAFI por cédula (prefijo) o por apellido/nombre (prefijo).
        /// Usado como fuente primaria en el autocomplete del Control Operativo para encontrar
        /// estudiantes que aún no han sido sincronizados al espejo local.
        /// </summary>
        Task<IEnumerable<CentralAlumnoLiteDto>> SearchStudentsFromCentralAsync(string query);
        Task<IEnumerable<CentralMatriculaDto>> GetActiveEnrollmentsFromCentralAsync();
        Task<IEnumerable<CentralAsignacionInstructorVehiculoDto>> GetInstructorVehicleAssignmentsFromCentralAsync();
        Task<IEnumerable<CentralAsignacionAlumnoVehiculoDto>> GetStudentVehicleAssignmentsFromCentralAsync();
        Task<IEnumerable<CentralHorarioDto>> GetAllSchedulesFromCentralAsync();
        Task<IEnumerable<CentralPracticaHorarioDto>> GetPracticeScheduleLinksFromCentralAsync();
        Task<IEnumerable<CentralMatriculaExamenDto>> GetMatriculaExamLinksFromCentralAsync();
        Task<IEnumerable<CentralPracticaSyncDto>> GetAllPracticesFromCentralAsync();
        /// <summary>Prácticas en ruta en SIGAFI (ensalida=1, no canceladas).</summary>
        Task<IReadOnlyList<ClaseActiva>> GetClasesActivasEnRutaFromCentralAsync();
        /// <summary>Vehículos inactivos en SIGAFI (p. ej. alerta operativa).</summary>
        Task<IReadOnlyList<AlertaMantenimiento>> GetAlertasVehiculoDesdeCentralAsync();
        Task<bool> PingSigafiAsync();

        /// <summary>
        /// Invalida todas las entradas de caché de catálogos SIGAFI.
        /// Llamar antes de un MasterSync para garantizar datos frescos.
        /// </summary>
        void InvalidateSigafiCatalogCache();
    }
}
