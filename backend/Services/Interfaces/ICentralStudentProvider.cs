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
        public int idMatricula { get; set; }
        public string idAlumno { get; set; } = string.Empty;
        public string? tipoDocumento { get; set; }
        public string? primerNombre { get; set; }
        public string? segundoNombre { get; set; }
        public string? apellidoPaterno { get; set; }
        public string? apellidoMaterno { get; set; }
        public DateTime? fecha_Nacimiento { get; set; }
        public string? direccion { get; set; }
        public string? telefono { get; set; }
        public string? celular { get; set; }
        public string? email { get; set; }
        public int idNivel { get; set; }
        public string idPeriodo { get; set; } = string.Empty;
        public int idSeccion { get; set; }
        public int idModalidad { get; set; }
        public int? idInstitucion { get; set; }
        public string? tituloColegio { get; set; }
        public DateTime? fecha_Inscripcion { get; set; }
        public string? sexo { get; set; }
        public string? tipo_sangre { get; set; }
        public string? user_alumno { get; set; }
        public string? password { get; set; }
        public string? email_institucional { get; set; }
        public int? primerIngreso { get; set; }
        public byte[]? foto { get; set; }
        public string? paralelo { get; set; }
        public string? seccion { get; set; }
        public string? Nivel { get; set; }
        /// <summary>Nombre de carrera SIGAFI (<c>carreras.Carrera</c>), sin mezclar semestre.</summary>
        public string? CarreraNombre { get; set; }
        /// <summary>Semestre/nivel del curso (<c>cursos.Nivel</c>), p. ej. TERCERO.</summary>
        public string? NivelCurso { get; set; }
        public string? JornadaSigafi { get; set; }
        public string? NombreCompleto { get; set; } 
        public string? DetalleRaw { get; set; }   
        public string? FotoBase64 { get; set; }
    }

    public class CentralInstructorDto
    {
        public string idProfesor { get; set; } = string.Empty;
        public string? tipodocumento { get; set; }
        public string? apellidos { get; set; }
        public string? nombres { get; set; }
        public string? primerApellido { get; set; }
        public string? segundoApellido { get; set; }
        public string? primerNombre { get; set; }
        public string? segundoNombre { get; set; }
        public string? direccion { get; set; }
        public string? callePrincipal { get; set; }
        public string? calleSecundaria { get; set; }
        public string? numeroCasa { get; set; }
        public string? telefono { get; set; }
        public string? celular { get; set; }
        public string? email { get; set; }
        public DateTime? fecha_nacimiento { get; set; }
        public string? sexo { get; set; }
        public string? clave { get; set; }
        public int? practicas { get; set; }
        public string? tipo { get; set; }
        public string? titulo { get; set; }
        public string? abreviatura { get; set; }
        public string? abreviatura_post { get; set; }
        public int activo { get; set; }
        public string? emailInstitucional { get; set; }
        public DateTime? fecha_ingreso { get; set; }
        public DateTime? fechaIngresoIess { get; set; }
        public DateTime? fecha_retiro { get; set; }
        public string? tipoSangre { get; set; }
        public string? foto { get; set; }
        public int? esReal { get; set; }
    }

    public class ScheduledPracticeDto
    {
        public int idPractica { get; set; }
        public string idalumno { get; set; } = string.Empty;
        public int idvehiculo { get; set; }
        public string idProfesor { get; set; } = string.Empty;

        /// <summary>Desde <c>cond_alumnos_practicas.idPeriodo</c> (SIGAFI).</summary>
        public string? idPeriodo { get; set; }

        public DateTime fecha { get; set; }
        public TimeSpan? hora_salida { get; set; }

        /// <summary>Campos SIGAFI para persistir el espejo local (tras enriquecer pueden reflejar también estado operativo local).</summary>
        public int SigafiCancelado { get; set; }
        public int SigafiEnsalida { get; set; }
        public TimeSpan? SigafiHoraLlegada { get; set; }

        public string AlumnoNombre { get; set; } = string.Empty;
        public string VehiculoDetalle { get; set; } = string.Empty;
        public string ProfesorNombre { get; set; } = string.Empty;

        public string EstadoOperativo { get; set; } = "sin_sincronizar";

        // Campos de Planificación Extendida (Agenda Perfecta)
        public int? idAsignacionHorario { get; set; }
        public List<int>? idsAsignacionHorario { get; set; }
        public string? HoraPlanificadaInicio { get; set; }
        public string? HoraPlanificadaFin { get; set; }
        public bool EsPlanificado { get; set; }
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
        
        // Metadata extendida para Control Operativo
        public string? HoraInicio { get; set; }
        public string? HoraFin { get; set; }
        public DateTime? FechaReal { get; set; }
        public int? FinSemana { get; set; }
        public string? VehiculoPlanificado { get; set; }
        public string? InstructorPlanificado { get; set; }
        public List<int>? idsAsignacionHorario { get; set; }
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
        public int? nota { get; set; }
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
        public decimal? beca_colegiatura { get; set; }
        public int? retirado { get; set; }
        public DateTime? fechaRetiro { get; set; }
        public string? observacion { get; set; }
        public int? convalidacion { get; set; }
        public string? carrera_convalidada { get; set; }
        public int? numero_permiso { get; set; }
        public string? user_matricula { get; set; }
        public int valida { get; set; }
        public int? esOyente { get; set; }
        public string? documentoFactura { get; set; }
    }

    public class CentralAsignacionInstructorVehiculoDto
    {
        public int idAsignacion { get; set; }
        public int idVehiculo { get; set; }
        public string idProfesor { get; set; } = string.Empty;
        public DateTime? fecha_asignacion { get; set; }
        public DateTime? fecha_salidad { get; set; }
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
    }

    public class CentralAlumnoLiteDto
    {
        public string idAlumno { get; set; } = string.Empty;
        public string? tipoDocumento { get; set; }
        public string? apellidoPaterno { get; set; }
        public string? apellidoMaterno { get; set; }
        public string? primerNombre { get; set; }
        public string? segundoNombre { get; set; }
        public DateTime? fecha_Nacimiento { get; set; }
        public string? direccion { get; set; }
        public string? telefono { get; set; }
        public string? celular { get; set; }
        public string? email { get; set; }
        public int idNivel { get; set; } = 1;
        public string? idPeriodo { get; set; }
        public int? idSeccion { get; set; }
        public int? idModalidad { get; set; }
        public int? idInstitucion { get; set; }
        public string? tituloColegio { get; set; }
        public DateTime? fecha_Inscripcion { get; set; }
        public string? sexo { get; set; }
        public string? tipo_sangre { get; set; }
        public string? user_alumno { get; set; }
        public string? password { get; set; }
        public string? email_institucional { get; set; }
        public int? primerIngreso { get; set; }
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
        
        Task<IEnumerable<CentralPeriodoDto>> GetAllPeriodosFromCentralAsync();
        Task<IEnumerable<CentralCarreraDto>> GetAllCarrerasFromCentralAsync();
        Task<IEnumerable<CentralSeccionDto>> GetAllSeccionesFromCentralAsync();
        Task<IEnumerable<CentralModalidadDto>> GetAllModalidadesFromCentralAsync();
        Task<IEnumerable<CentralInstitucionDto>> GetAllInstitucionesFromCentralAsync();
        
        Task<IEnumerable<CentralFechaHorarioDto>> GetAllFechasHorariosFromCentralAsync();
        Task<IEnumerable<CentralHoraClaseDto>> GetAllHorasClasesFromCentralAsync();
        Task<IEnumerable<CentralHorarioProfesorDto>> GetAllHorariosProfesoresFromCentralAsync();

        Task<bool> PingSigafiAsync();

        /// <summary>Historial de retornos de hoy (completados).</summary>
        Task<IEnumerable<ScheduledPracticeDto>> GetTodayCompletedPracticesAsync(int limit = 50);

        /// <summary>
        /// Invalida todas las entradas de caché de catálogos SIGAFI.
        /// Llamar antes de un MasterSync para garantizar datos frescos.
        /// </summary>
        void InvalidateSigafiCatalogCache();
        Task<IDictionary<string, CentralHorarioDto>> GetNextSchedulesForAlumnosAsync(IEnumerable<string> ids);

        /// <summary>Libera un registro en la base central (cancelado=1, ensalida=0).</summary>
        Task<bool> CancelPracticeInCentralAsync(int idPractica);
    }
}

