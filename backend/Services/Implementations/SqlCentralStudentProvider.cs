using backend.Data;
using backend.Models;
using backend.Services.Helpers;
using backend.Services.Interfaces;
using backend.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using MySqlConnector;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend.Services.Implementations
{
    /**
     * Puente de lectura hacia la BD remota SIGAFI (sigafi_es) vía SigafiConnection.
     * Aquí vive la extracción primaria; DataSyncService y otros consumidores copian hacia la BD local.
     * Paridad de nombres de columnas con SIGAFI 2026.
     */
    public class SqlCentralStudentProvider : ICentralStudentProvider
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;
        private readonly ISigafiResiliencePipeline _pipeline;
        private readonly ILogger<SqlCentralStudentProvider> _logger;

        // Tiempos de caché para catálogos que no cambian en tiempo real
        private static readonly TimeSpan CacheInstructores = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CacheVehiculos    = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CacheCursos       = TimeSpan.FromMinutes(15);
        
        private const string AlumnoLiteCols = @"idAlumno, tipoDocumento, primerNombre, segundoNombre, apellidoPaterno, apellidoMaterno,
                                  fecha_Nacimiento, direccion, telefono, celular, email, 
                                  sexo, idPeriodo, idNivel, idSeccion, idModalidad, idInstitucion,
                                  tituloColegio, fecha_Inscripcion, tipo_sangre, user_alumno, password,
                                  email_institucional, primerIngreso";

        public SqlCentralStudentProvider(
            AppDbContext context,
            IConfiguration configuration,
            IMemoryCache cache,
            ISigafiResiliencePipeline pipeline,
            ILogger<SqlCentralStudentProvider> logger)
        {
            _cache = cache;
            _pipeline = pipeline;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("SigafiConnection")
                ?? throw new InvalidOperationException("Falta SigafiConnection para leer SIGAFI.");
        }

        public async Task<CentralStudentDto?> GetFromCentralAsync(string idAlumno)
        {
            try
            {
                // cursos.Nivel = solo semestre (CUARTO). El nombre de carrera está en carreras (idCarrera), como en la ficha SIGAFI.
                const string selectBase = @"
                    SELECT
                        a.idAlumno, a.tipoDocumento, a.primerNombre, a.segundoNombre, a.apellidoPaterno, a.apellidoMaterno,
                        a.fecha_Nacimiento, a.direccion, a.telefono, a.celular, a.email,
                        a.sexo, m.idNivel, COALESCE(p.idPeriodo, m.idPeriodo) AS idPeriodo, 
                        m.idMatricula,
                        COALESCE(m.idSeccion, 0) AS idSeccion, COALESCE(m.idModalidad, 0) AS idModalidad, m.paralelo,
                        a.idInstitucion, a.tituloColegio, a.fecha_Inscripcion,
                        a.tipo_sangre, a.user_alumno, a.password,
                        a.email_institucional,
                        a.primerIngreso,
                        s.seccion,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS NombreCompleto,
                        CONCAT(
                            TRIM(CONCAT_WS(' ', NULLIF(TRIM(car.Carrera), ''), NULLIF(TRIM(c.Nivel), ''))),
                            ', PARALELO:',
                            COALESCE(m.paralelo, ''),
                            ' ',
                            IFNULL(s.seccion, '')
                        ) AS DetalleRaw,
                        NULLIF(TRIM(car.Carrera), '') AS CarreraNombre,
                        NULLIF(TRIM(c.Nivel), '') AS NivelCurso,
                        TRIM(CONCAT_WS(' ', NULLIF(TRIM(car.Carrera), ''), NULLIF(TRIM(c.Nivel), ''))) AS Nivel";

                // 0) Última matrícula válida (no usar alumnos.idPeriodo: en tu caso sigue ABR2024/50 mientras matriculas ya va por ABR2025/52).
                //    Orden: fechaMatricula más reciente; empate → idMatricula más alto. Sin fecha → solo idMatricula.
                const string ordenMatriculaMasReciente = @"
                    ORDER BY (m.fechaMatricula IS NULL) ASC, m.fechaMatricula DESC, m.idMatricula DESC
                    LIMIT 1";
                var sqlUltimaMatricula = selectBase + @"
                    FROM alumnos a
                    INNER JOIN matriculas m ON m.idAlumno = a.idAlumno AND COALESCE(m.valida, 1) = 1
                    INNER JOIN periodos p ON p.idPeriodo = m.idPeriodo
                    LEFT JOIN cursos c ON c.idNivel = m.idNivel
                    LEFT JOIN carreras car ON car.idCarrera = c.idCarrera
                    LEFT JOIN secciones s ON s.idSeccion = m.idSeccion
                    WHERE a.idAlumno = @p0
                    " + ordenMatriculaMasReciente;
                var result = await QuerySingleAsync(sqlUltimaMatricula, idAlumno, MapCentralStudent);

                // 1) Igual criterio, pero si falta fila en periodos (p. ej. ABR2025 aún no en tabla periodos).
                if (result == null)
                {
                    var sqlUltimaMatriculaSinPeriodo = selectBase + @"
                    FROM alumnos a
                    INNER JOIN matriculas m ON m.idAlumno = a.idAlumno AND COALESCE(m.valida, 1) = 1
                    LEFT JOIN periodos p ON p.idPeriodo = m.idPeriodo
                    LEFT JOIN cursos c ON c.idNivel = m.idNivel
                    LEFT JOIN carreras car ON car.idCarrera = c.idCarrera
                    LEFT JOIN secciones s ON s.idSeccion = m.idSeccion
                    WHERE a.idAlumno = @p0
                    " + ordenMatriculaMasReciente;
                    result = await QuerySingleAsync(sqlUltimaMatriculaSinPeriodo, idAlumno, MapCentralStudent);
                }

                // 2) Respaldo: periodo académico activo (varias matrículas vigentes).
                if (result == null)
                {
                    var sqlActivo = selectBase + @"
                    FROM alumnos a
                    JOIN matriculas m ON m.idAlumno = a.idAlumno AND COALESCE(m.valida, 1) = 1
                    JOIN periodos p ON p.idPeriodo = m.idPeriodo
                    LEFT JOIN cursos c ON c.idNivel = m.idNivel
                    LEFT JOIN carreras car ON car.idCarrera = c.idCarrera
                    LEFT JOIN secciones s ON s.idSeccion = m.idSeccion
                    WHERE a.idAlumno = @p0 AND p.activo = 1
                    " + ordenMatriculaMasReciente;
                    result = await QuerySingleAsync(sqlActivo, idAlumno, MapCentralStudent);
                }

                // 3) Solo ficha en alumnos (evita “no existe” si aún no cargaron matrícula/periodo).
                if (result == null)
                {
                    const string sqlSoloAlumno = @"
                    SELECT
                        a.idAlumno, a.tipoDocumento, a.primerNombre, a.segundoNombre, 
                        a.apellidoPaterno, a.apellidoMaterno, a.fecha_Nacimiento, a.direccion, 
                        a.telefono, a.celular, a.email,
                        a.sexo,
                        0 AS idMatricula,
                        0 AS idNivel, 0 AS idSeccion, 0 AS idModalidad, NULL AS idInstitucion,
                        a.tituloColegio, a.fecha_Inscripcion,
                        a.tipo_sangre, a.user_alumno, a.password,
                        a.email_institucional,
                        a.primerIngreso,
                        NULL AS paralelo, NULL AS seccion,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS NombreCompleto,
                        'Alumno en SIGAFI sin matrícula registrada' AS DetalleRaw,
                        NULL AS CarreraNombre,
                        NULL AS NivelCurso,
                        NULL AS Nivel,
                        'SIN_MAT' AS idPeriodo
                    FROM alumnos a
                    WHERE a.idAlumno = @p0
                    LIMIT 1";
                    result = await QuerySingleAsync(sqlSoloAlumno, idAlumno, MapCentralStudent);
                }

                if (result != null && string.IsNullOrWhiteSpace(result.seccion) && result.idModalidad > 0)
                    result.JornadaSigafi = await TryGetModalidadDescripcionAsync(result.idModalidad);

                if (result?.foto != null)
                    result.FotoBase64 = Convert.ToBase64String(result.foto);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando alumno SIGAFI {idAlumno}", idAlumno);
                throw;
            }
        }

        public async Task<CentralInstructorDto?> GetInstructorFromCentralAsync(string idProfesor)
        {
            try
            {
                const string sql = @"
                    SELECT
                        idProfesor, tipodocumento, apellidos, nombres, primerApellido, segundoApellido,
                        primerNombre, segundoNombre, direccion, callePrincipal, calleSecundaria,
                        numeroCasa, telefono, celular, email, fecha_nacimiento, sexo, clave, practicas,
                        tipo, titulo, abreviatura, abreviatura_post, CAST(activo AS SIGNED) AS activo,
                        emailInstitucional, fecha_ingreso,
                        fechaIngresoIess, fecha_retiro, tipoSangre,
                        foto, esReal
                    FROM profesores
                    WHERE idProfesor = @p0
                    LIMIT 1";
                return await QuerySingleAsync(sql, idProfesor, MapCentralInstructor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando instructor SIGAFI {idProfesor}", idProfesor);
                return null;
            }
        }

        public async Task<IEnumerable<CentralInstructorDto>> GetAllInstructorsFromCentralAsync()
        {
            if (_cache.TryGetValue("sigafi:instructores", out IEnumerable<CentralInstructorDto>? cached) && cached != null)
                return cached;
            try
            {
                const string sql = @"
                    SELECT idProfesor, tipodocumento, apellidos, nombres, primerApellido, segundoApellido,
                           primerNombre, segundoNombre, direccion, callePrincipal, calleSecundaria,
                           numeroCasa, telefono, celular, email, fecha_nacimiento, sexo, clave, practicas,
                           tipo, titulo, abreviatura, abreviatura_post, CAST(activo AS SIGNED) AS activo,
                           emailInstitucional, fecha_ingreso,
                           fechaIngresoIess, fecha_retiro, tipoSangre, esReal
                    FROM profesores";
                var result = (await QueryListAsync(sql, MapCentralInstructor)).ToList();
                _cache.Set("sigafi:instructores", (IEnumerable<CentralInstructorDto>)result, CacheInstructores);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando instructores SIGAFI.");
                return new List<CentralInstructorDto>();
            }
        }

        public async Task<CentralInstructorDto?> GetAssignedTutorAsync(string idAlumno)
        {
            try
            {
                const string sql = @"
                    SELECT
                        p.idProfesor,
                        p.tipodocumento,
                        p.nombres,
                        p.apellidos,
                        p.primerApellido,
                        p.segundoApellido,
                        p.primerNombre,
                        p.segundoNombre,
                        p.celular,
                        p.email,
                        p.direccion,
                        p.callePrincipal,
                        p.calleSecundaria,
                        p.numeroCasa,
                        p.telefono,
                        p.sexo,
                        p.clave,
                        p.tipo,
                        p.titulo,
                        p.abreviatura,
                        p.abreviatura_post,
                        CAST(p.activo AS SIGNED) AS activo
                    FROM cond_alumnos_vehiculos v
                    JOIN profesores p ON p.idProfesor = v.idProfesor
                    WHERE v.idAlumno = @p0 AND v.activa = 1
                    LIMIT 1";
                return await QuerySingleAsync(sql, idAlumno, MapCentralInstructor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando tutor SIGAFI de alumno {idAlumno}", idAlumno);
                return null;
            }
        }

        public async Task<ScheduledPracticeDto?> GetScheduledPracticeAsync(string idAlumno)
        {
            try
            {
                const string sql = @"
                    SELECT
                        p.idPractica,
                        p.idalumno,
                        p.idvehiculo,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS AlumnoNombre,
                        p.idProfesor,
                        p.idPeriodo AS idPeriodo,
                        p.fecha,
                        p.hora_salida,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoDetalle,
                        CONCAT_WS(' ', pr.apellidos, pr.nombres) AS ProfesorNombre,
                        CAST(COALESCE(p.cancelado, 0) AS SIGNED) AS SigafiCancelado,
                        CAST(COALESCE(p.ensalida, 0) AS SIGNED) AS SigafiEnsalida,
                        p.hora_llegada AS SigafiHoraLlegada
                    FROM cond_alumnos_practicas p
                    JOIN alumnos a ON a.idAlumno = p.idalumno
                    JOIN vehiculos v ON v.idVehiculo = p.idvehiculo
                    JOIN profesores pr ON pr.idProfesor = p.idProfesor
                    WHERE p.idalumno = @p0
                    AND p.fecha >= CURDATE()
                    ORDER BY p.fecha ASC, p.hora_salida ASC
                    LIMIT 1";
                return await QuerySingleAsync(sql, idAlumno, MapScheduledPractice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando práctica SIGAFI de alumno {idAlumno}", idAlumno);
                return null;
            }
        }

        public async Task<IEnumerable<ScheduledPracticeDto>> GetRecentSchedulesAsync(int limit = 100)
        {
            try
            {
                var take = Math.Clamp(limit, 1, 200);
                var sql = $@"
                    SELECT
                        COALESCE(p.idPractica, 0) AS idPractica,
                        av.idAlumno AS idalumno,
                        av.idVehiculo AS idvehiculo,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS AlumnoNombre,
                        av.idProfesor AS idProfesor,
                        av.idPeriodo AS idPeriodo,
                        fh.fecha AS fecha,
                        p.hora_salida AS hora_salida,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoDetalle,
                        CONCAT_WS(' ', pr.apellidos, pr.nombres) AS ProfesorNombre,
                        CAST(COALESCE(p.cancelado, 0) AS SIGNED) AS SigafiCancelado,
                        CAST(COALESCE(p.ensalida, 0) AS SIGNED) AS SigafiEnsalida,
                        p.hora_llegada AS SigafiHoraLlegada,
                        h.idAsignacionHorario AS idAsignacionHorario,
                        hc.hora_inicio AS HoraPlanificadaInicio,
                        hc.hora_fin AS HoraPlanificadaFin,
                        1 AS EsPlanificado
                    FROM cond_alumnos_horarios h
                    JOIN cond_alumnos_vehiculos av ON av.idAsignacion = h.idAsignacion
                    JOIN fechas_horarios fh ON fh.idFecha = h.idFecha
                    JOIN horas_clases hc ON hc.idhora = h.idHora
                    LEFT JOIN cond_practicas_horarios_alumnos pha ON pha.idAsignacionHorario = h.idAsignacionHorario
                    LEFT JOIN cond_alumnos_practicas p ON p.idPractica = pha.idPractica
                    JOIN alumnos a ON a.idAlumno = av.idAlumno
                    JOIN vehiculos v ON v.idVehiculo = av.idVehiculo
                    JOIN profesores pr ON pr.idProfesor = av.idProfesor
                    WHERE fh.fecha = CURDATE() AND COALESCE(h.activo, 1) = 1 AND COALESCE(av.activa, 1) = 1

                    UNION

                    SELECT
                        p.idPractica,
                        p.idalumno,
                        p.idvehiculo,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS AlumnoNombre,
                        p.idProfesor,
                        p.idPeriodo AS idPeriodo,
                        p.fecha,
                        p.hora_salida,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoDetalle,
                        CONCAT_WS(' ', pr.apellidos, pr.nombres) AS ProfesorNombre,
                        CAST(COALESCE(p.cancelado, 0) AS SIGNED) AS SigafiCancelado,
                        CAST(COALESCE(p.ensalida, 0) AS SIGNED) AS SigafiEnsalida,
                        p.hora_llegada AS SigafiHoraLlegada,
                        NULL AS idAsignacionHorario,
                        NULL AS HoraPlanificadaInicio,
                        NULL AS HoraPlanificadaFin,
                        0 AS EsPlanificado
                    FROM cond_alumnos_practicas p
                    JOIN alumnos a ON a.idAlumno = p.idalumno
                    JOIN vehiculos v ON v.idVehiculo = p.idvehiculo
                    JOIN profesores pr ON pr.idProfesor = p.idProfesor
                    WHERE (p.fecha >= CURDATE() - INTERVAL 1 DAY)
                    AND (
                        -- Solo filtrar por cancelado si la columna existe o asumir 0
                        NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = 'sigafi_es' AND TABLE_NAME = 'cond_alumnos_practicas' AND COLUMN_NAME = 'cancelado')
                        OR p.cancelado = 0
                    )
                      AND p.fecha >= CURDATE() - INTERVAL 1 DAY
                      AND NOT EXISTS (
                          SELECT 1 
                          FROM cond_practicas_horarios_alumnos pha 
                          JOIN cond_alumnos_horarios h ON h.idAsignacionHorario = pha.idAsignacionHorario
                          JOIN fechas_horarios fh ON fh.idFecha = h.idFecha
                          WHERE pha.idPractica = p.idPractica AND fh.fecha = CURDATE()
                      )
                    ORDER BY fecha DESC, COALESCE(HoraPlanificadaInicio, '23:59:00') ASC, hora_salida DESC
                    LIMIT {take}";
                return await QueryListAsync(sql, MapScheduledPractice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando prácticas recientes SIGAFI.");
                return new List<ScheduledPracticeDto>();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, ScheduledPracticeDto>> GetNextOpenPracticesForAlumnosAsync(
            IEnumerable<string> idAlumnos)
        {
            var ids = idAlumnos?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.Ordinal)
                .Take(25)
                .ToList() ?? new List<string>();
            if (ids.Count == 0)
                return new Dictionary<string, ScheduledPracticeDto>(StringComparer.Ordinal);

            try
            {
                var inClause = string.Join(",", ids.Select((_, i) => $"@p{i}"));
                var sql = $@"
                    SELECT
                        p.idPractica,
                        p.idalumno,
                        p.idvehiculo,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS AlumnoNombre,
                        p.idProfesor,
                        p.idPeriodo AS idPeriodo,
                        p.fecha,
                        p.hora_salida,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoDetalle,
                        CONCAT_WS(' ', pr.apellidos, pr.nombres) AS ProfesorNombre,
                        CAST(COALESCE(p.cancelado, 0) AS SIGNED) AS SigafiCancelado,
                        CAST(COALESCE(p.ensalida, 0) AS SIGNED) AS SigafiEnsalida,
                        p.hora_llegada AS SigafiHoraLlegada
                    FROM cond_alumnos_practicas p
                    JOIN alumnos a ON a.idAlumno = p.idalumno
                    JOIN vehiculos v ON v.idVehiculo = p.idvehiculo
                    JOIN profesores pr ON pr.idProfesor = p.idProfesor
                    WHERE p.hora_llegada IS NULL
                    AND p.fecha >= CURDATE()
                    AND (
                        NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = 'sigafi_es' AND TABLE_NAME = 'cond_alumnos_practicas' AND COLUMN_NAME = 'cancelado')
                        OR p.cancelado = 0
                    )
                    AND TRIM(p.idalumno) IN ({inClause})
                    ORDER BY p.idalumno ASC, p.fecha ASC, p.hora_salida ASC";


                var rows = (await QueryListAsync(sql, ids, MapScheduledPractice)).ToList();
                var dict = new Dictionary<string, ScheduledPracticeDto>(StringComparer.Ordinal);
                foreach (var row in rows)
                {
                    var tid = row.idalumno?.Trim();
                    if (tid != null && !dict.ContainsKey(tid))
                        dict[tid] = row;
                }


                return dict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando prácticas abiertas SIGAFI por lote de alumnos.");
                return new Dictionary<string, ScheduledPracticeDto>(StringComparer.Ordinal);
            }
        }

        public async Task<IEnumerable<CentralUserDto>> GetAllWebUsersAsync()
        {
            try
            {
                const string sql = @"
                    SELECT
                        usuario,
                        password,
                        salida,
                        ingreso,
                        activo,
                        asistencia,
                        esRrhh
                    FROM usuarios_web";
                return await QueryListAsync(sql, reader => new CentralUserDto
                {
                    usuario = ReadString(reader, "usuario"),
                    password = ReadString(reader, "password"),
                    salida = ReadInt(reader, "salida"),
                    ingreso = ReadInt(reader, "ingreso"),
                    activo = ReadInt(reader, "activo"),
                    asistencia = ReadInt(reader, "asistencia"),
                    esRrhh = ReadInt(reader, "esRrhh")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando usuarios_web SIGAFI.");
                return new List<CentralUserDto>();
            }
        }

        public Task<CentralUserDto?> GetWebUserFromSigafiAsync(string usuario) =>
            QuerySingleAsync(
                @"SELECT usuario, password, salida, ingreso, activo, asistencia, esRrhh
                  FROM usuarios_web WHERE usuario = @p0 LIMIT 1",
                usuario,
                reader => new CentralUserDto
                {
                    usuario = ReadString(reader, "usuario"),
                    password = ReadString(reader, "password"),
                    salida = ReadInt(reader, "salida"),
                    ingreso = ReadInt(reader, "ingreso"),
                    activo = ReadInt(reader, "activo"),
                    asistencia = ReadInt(reader, "asistencia"),
                    esRrhh = ReadInt(reader, "esRrhh")
                });

        public async Task<CentralHorarioDto?> GetNextScheduleAsync(string idAlumno)
        {
            try
            {
                const string sql = @"
                    SELECT 
                        h.idAsignacionHorario, 
                        h.idAsignacion, 
                        h.idFecha, 
                        h.idHora,
                        CAST(h.asiste AS SIGNED) AS asiste,
                        CAST(COALESCE(h.activo, 1) AS SIGNED) AS activo,
                        h.observacion,
                        hc.hora_inicio AS HoraInicio,
                        hc.hora_fin AS HoraFin,
                        fh.fecha AS FechaReal,
                        fh.finsemana AS FinSemana,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoPlanificado,
                        TRIM(CONCAT_WS(' ', pr.apellidos, pr.nombres)) AS InstructorPlanificado
                    FROM cond_alumnos_horarios h
                    JOIN cond_alumnos_vehiculos a ON a.idAsignacion = h.idAsignacion
                    JOIN fechas_horarios fh ON fh.idFecha = h.idFecha
                    LEFT JOIN horas_clases hc ON hc.idhora = h.idHora
                    LEFT JOIN vehiculos v ON v.idVehiculo = a.idVehiculo
                    LEFT JOIN profesores pr ON pr.idProfesor = a.idProfesor
                    WHERE TRIM(a.idAlumno) = TRIM(@p0) 
                    AND COALESCE(h.activo, 1) = 1

                    ORDER BY 
                        (CASE 
                            WHEN fh.fecha = CURDATE() THEN 0 
                            WHEN fh.fecha > CURDATE() THEN 1 
                            ELSE 2 
                        END) ASC,
                        ABS(DATEDIFF(fh.fecha, CURDATE())) ASC,
                        hc.hora_inicio ASC
                    LIMIT 1";



                return await QuerySingleAsync(sql, idAlumno, MapCentralHorario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando próximo horario SIGAFI de alumno {idAlumno}", idAlumno);
                return null;
            }
        }

        private const string SqlVehiculosSelect = @"
                SELECT idVehiculo, idSubcategoria, numero_vehiculo, placa, marca, anio, idCategoria,
                       CAST(activo AS SIGNED) AS activo, observacion, chasis, motor, modelo
                FROM vehiculos";

        private static CentralVehiculoDto MapCentralVehiculo(MySqlDataReader reader) => new()
        {
            idVehiculo = ReadInt(reader, "idVehiculo"),
            idSubcategoria = ReadNullableInt(reader, "idSubcategoria"),
            numero_vehiculo = ReadNullableString(reader, "numero_vehiculo"),
            placa = ReadNullableString(reader, "placa"),
            marca = ReadNullableString(reader, "marca"),
            anio = ReadNullableInt(reader, "anio"),
            idCategoria = ReadNullableInt(reader, "idCategoria"),
            activo = ReadInt(reader, "activo"),
            observacion = ReadNullableString(reader, "observacion"),
            chasis = ReadNullableString(reader, "chasis"),
            motor = ReadNullableString(reader, "motor"),
            modelo = ReadNullableString(reader, "modelo")
        };

        public async Task<IEnumerable<CentralVehiculoDto>> GetAllVehiclesFromCentralAsync()
        {
            if (_cache.TryGetValue("sigafi:vehiculos", out IEnumerable<CentralVehiculoDto>? cached) && cached != null)
                return cached;
            var result = (await QueryListAsync(SqlVehiculosSelect, MapCentralVehiculo)).ToList();
            _cache.Set("sigafi:vehiculos", (IEnumerable<CentralVehiculoDto>)result, CacheVehiculos);
            return result;
        }

        public Task<CentralVehiculoDto?> GetVehicleByPlacaFromCentralAsync(string placa) =>
            QuerySingleAsync(
                SqlVehiculosSelect + " WHERE placa = @p0 LIMIT 1",
                placa.Trim(),
                MapCentralVehiculo);

        public async Task<IReadOnlyList<ClaseActiva>> GetClasesActivasEnRutaFromCentralAsync()
        {
            var list = new List<ClaseActiva>();
            try
            {
                const string sql = @"
SELECT
    p.idPractica,
    p.idvehiculo AS idVehiculo,
    p.idalumno,
    TRIM(CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre)) AS estudiante,
    COALESCE(v.placa, '') AS placa,
    v.numero_vehiculo AS numero_vehiculo_raw,
    TRIM(CONCAT_WS(' ', pr.apellidos, pr.nombres)) AS instructor,
    p.fecha,
    p.hora_salida
FROM cond_alumnos_practicas p
INNER JOIN alumnos a ON a.idAlumno = p.idalumno
INNER JOIN vehiculos v ON v.idVehiculo = p.idvehiculo
INNER JOIN profesores pr ON pr.idProfesor = p.idProfesor
WHERE COALESCE(p.cancelado, 0) = 0 AND COALESCE(p.ensalida, 0) = 1";

                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var fecha = ReadDate(reader, "fecha");
                    var hs = ReadNullableTime(reader, "hora_salida");
                    var salidaDt = hs.HasValue ? fecha.Date.Add(hs.Value) : fecha.Date;

                    var num = ReadNumeroVehiculoFlexible(reader, "numero_vehiculo_raw");

                    list.Add(new ClaseActiva
                    {
                        idPractica = ReadInt(reader, "idPractica"),
                        idVehiculo = ReadInt(reader, "idVehiculo"),
                        idAlumno = ReadString(reader, "idalumno"),
                        estudiante = ReadNullableString(reader, "estudiante") ?? "",
                        placa = ReadNullableString(reader, "placa") ?? "",
                        numeroVehiculo = num,
                        instructor = ReadNullableString(reader, "instructor") ?? "",
                        salida = hs
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leyendo clases en ruta desde SIGAFI.");
            }

            return list;
        }

        public async Task<IReadOnlyList<AlertaMantenimiento>> GetAlertasVehiculoDesdeCentralAsync()
        {
            var list = new List<AlertaMantenimiento>();
            try
            {
                const string sql = @"
SELECT idVehiculo, numero_vehiculo, placa
FROM vehiculos
WHERE COALESCE(activo, 1) = 0";

                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var num = ReadNumeroVehiculoFlexible(reader, "numero_vehiculo");

                    list.Add(new AlertaMantenimiento
                    {
                        id_vehiculo = ReadInt(reader, "idVehiculo"),
                        numero_vehiculo = num,
                        placa = ReadNullableString(reader, "placa") ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leyendo vehículos inactivos (alerta) desde SIGAFI.");
            }

            return list;
        }

        public async Task<IEnumerable<CentralCursoDto>> GetAllCoursesFromCentralAsync()
        {
            if (_cache.TryGetValue("sigafi:cursos", out IEnumerable<CentralCursoDto>? cached) && cached != null)
                return cached;
            var result = (await QueryListAsync(
                @"SELECT idNivel, idCarrera, Nivel, jerarquia, orden,
                         CAST(esRecuperacion AS SIGNED) AS esRecuperacion,
                         aliasCurso
                  FROM cursos",
                reader => new CentralCursoDto
                {
                    idNivel = ReadInt(reader, "idNivel"),
                    idCarrera = ReadInt(reader, "idCarrera"),
                    Nivel = ReadNullableString(reader, "Nivel"),
                    jerarquia = ReadNullableInt(reader, "jerarquia"),
                    orden = ReadNullableInt(reader, "orden"),
                    esRecuperacion = ReadNullableInt(reader, "esRecuperacion"),
                    aliasCurso = ReadNullableString(reader, "aliasCurso")
                })).ToList();
            _cache.Set("sigafi:cursos", (IEnumerable<CentralCursoDto>)result, CacheCursos);
            return result;
        }

        /// <summary>
        /// SIGAFI no expone tipo_licencia; se deriva de categoria_vehiculos para poblar tipo_licencia local.
        /// Para categorías "LICENCIA TIPO X" se normaliza el código como X (C/D/E...).
        /// </summary>
        public async Task<IEnumerable<CentralTipoLicenciaDto>> GetAllLicenseTypesFromCentralAsync()
        {
            var result = (await QueryListAsync(
                @"SELECT idCategoria, categoria FROM categoria_vehiculos",
                reader =>
                {
                    var idCat = ReadInt(reader, "idCategoria");
                    var descripcion = ReadString(reader, "categoria").Trim();
                    var upper = descripcion.ToUpperInvariant();

                    var codigo = $"V{idCat}";
                    var marker = "TIPO ";
                    var markerIndex = upper.LastIndexOf(marker, StringComparison.Ordinal);
                    if (markerIndex >= 0)
                    {
                        var letterStart = markerIndex + marker.Length;
                        if (letterStart < upper.Length)
                        {
                            var letter = upper[letterStart];
                            if (char.IsLetter(letter))
                                codigo = letter.ToString();
                        }
                    }

                    return new CentralTipoLicenciaDto
                    {
                        id_categoria_sigafi = idCat,
                        id_tipo = 0,
                        codigo = codigo,
                        descripcion = descripcion,
                        activo = 1
                    };
                })).ToList();
            return result;
        }

        public Task<IEnumerable<CentralCategoriaVehiculoDto>> GetAllVehicleCategoriesFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idCategoria, categoria FROM categoria_vehiculos",
                reader => new CentralCategoriaVehiculoDto
                {
                    idCategoria = ReadInt(reader, "idCategoria"),
                    categoria = ReadString(reader, "categoria")
                });

        public Task<IEnumerable<CentralCategoriaExamenDto>> GetAllExamCategoriesFromCentralAsync() =>
            QueryListAsync(
                @"SELECT IdCategoria, categoria, CAST(tieneNota AS SIGNED) AS tieneNota, CAST(activa AS SIGNED) AS activa FROM categorias_examenes_conduccion",
                reader => new CentralCategoriaExamenDto
                {
                    IdCategoria = ReadInt(reader, "IdCategoria"),
                    categoria = ReadString(reader, "categoria"),
                    tieneNota = ReadInt(reader, "tieneNota"),
                    activa = ReadInt(reader, "activa")
                });

        public async Task<IEnumerable<CentralAlumnoLiteDto>> SearchStudentsFromCentralAsync(string query)
        {
            // Si parece cédula (solo dígitos) busca por prefijo de idAlumno (PK indexada → muy rápido).
            // Si parece nombre busca por prefijo en apellidoPaterno y primerNombre (sin LIKE '%x' para usar índices).
            var q = query.Trim();
            var isCedula = q.All(char.IsDigit);
            try
            {
                if (isCedula)
                {
                    return await _pipeline.ExecuteAsync(async () =>
                    {
                        var list = new List<CentralAlumnoLiteDto>();
                        await using var conn = new MySqlConnection(_connectionString);
                        await conn.OpenAsync();
                        var sql = $"SELECT {AlumnoLiteCols} FROM alumnos WHERE idAlumno LIKE CONCAT(@p0,'%') LIMIT 15";
                        await using var cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@p0", q);
                        await using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                            list.Add(MapAlumnoLite(reader));
                        return (IEnumerable<CentralAlumnoLiteDto>)list;
                    });
                }
                else
                {
                    // Búsqueda por apellido o primer nombre (prefijo).
                    return await _pipeline.ExecuteAsync(async () =>
                    {
                        var list = new List<CentralAlumnoLiteDto>();
                        await using var conn = new MySqlConnection(_connectionString);
                        await conn.OpenAsync();
                        var sql = $@"SELECT {AlumnoLiteCols} FROM alumnos
                                     WHERE apellidoPaterno LIKE CONCAT(@p0,'%')
                                        OR apellidoMaterno LIKE CONCAT(@p0,'%')
                                        OR primerNombre    LIKE CONCAT(@p0,'%')
                                     LIMIT 15";
                        await using var cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@p0", q.ToUpperInvariant());
                        await using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                            list.Add(MapAlumnoLite(reader));
                        return (IEnumerable<CentralAlumnoLiteDto>)list;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Búsqueda de alumnos en SIGAFI no disponible para query '{q}'.", q);
                return Enumerable.Empty<CentralAlumnoLiteDto>();
            }
        }

        private static CentralAlumnoLiteDto MapAlumnoLite(MySqlDataReader reader) => new()
        {
            idAlumno = ReadString(reader, "idAlumno"),
            tipoDocumento = ReadNullableString(reader, "tipoDocumento"),
            primerNombre = ReadNullableString(reader, "primerNombre"),
            segundoNombre = ReadNullableString(reader, "segundoNombre"),
            apellidoPaterno = ReadNullableString(reader, "apellidoPaterno"),
            apellidoMaterno = ReadNullableString(reader, "apellidoMaterno"),
            fecha_Nacimiento = ReadNullableDate(reader, "fecha_Nacimiento"),
            direccion = ReadNullableString(reader, "direccion"),
            telefono = ReadNullableString(reader, "telefono"),
            celular = ReadNullableString(reader, "celular"),
            email = ReadNullableString(reader, "email"),
            idNivel = ReadInt(reader, "idNivel"),
            idPeriodo = ReadNullableString(reader, "idPeriodo"),
            idSeccion = ReadNullableInt(reader, "idSeccion"),
            idModalidad = ReadNullableInt(reader, "idModalidad"),
            idInstitucion = ReadNullableInt(reader, "idInstitucion"),
            tituloColegio = ReadNullableString(reader, "tituloColegio"),
            fecha_Inscripcion = ReadNullableDate(reader, "fecha_Inscripcion"),
            sexo = ReadNullableString(reader, "sexo"),
            tipo_sangre = ReadNullableString(reader, "tipo_sangre"),
            user_alumno = ReadNullableString(reader, "user_alumno"),
            password = ReadNullableString(reader, "password"),
            email_institucional = ReadNullableString(reader, "email_institucional"),
            primerIngreso = ReadNullableInt(reader, "primerIngreso")
        };

        public Task<IEnumerable<CentralAlumnoLiteDto>> GetAllStudentsFromCentralAsync()
        {
            return QueryListAsync("SELECT " + AlumnoLiteCols + " FROM alumnos", MapAlumnoLite);
        }

        public Task<IEnumerable<CentralMatriculaDto>> GetActiveEnrollmentsFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idMatricula, idAlumno, idNivel, COALESCE(idSeccion, 1) AS idSeccion, COALESCE(idModalidad, 1) AS idModalidad, idPeriodo, fechaMatricula, paralelo,
                         CAST(arrastres AS SIGNED) AS arrastres, folio, beca_matricula, beca_colegiatura, CAST(retirado AS SIGNED) AS retirado, fechaRetiro, observacion,
                         CAST(convalidacion AS SIGNED) AS convalidacion, carrera_convalidada, numero_permiso, user_matricula, 
                         COALESCE(valida, 1) AS valida, CAST(esOyente AS SIGNED) AS esOyente, documentoFactura
                  FROM matriculas",
                reader => new CentralMatriculaDto
                {
                    idMatricula = ReadInt(reader, "idMatricula"),
                    idAlumno = ReadString(reader, "idAlumno"),
                    idNivel = ReadInt(reader, "idNivel"),
                    idSeccion = ReadInt(reader, "idSeccion"),
                    idModalidad = ReadInt(reader, "idModalidad"),
                    idPeriodo = ReadString(reader, "idPeriodo"),
                    fechaMatricula = ReadNullableDate(reader, "fechaMatricula"),
                    paralelo = ReadNullableString(reader, "paralelo"),
                    arrastres = ReadNullableInt(reader, "arrastres"),
                    folio = ReadNullableInt(reader, "folio"),
                    beca_matricula = ReadNullableDecimal(reader, "beca_matricula"),
                    beca_colegiatura = ReadNullableDecimal(reader, "beca_colegiatura"),
                    retirado = ReadNullableInt(reader, "retirado"),
                    fechaRetiro = ReadNullableDate(reader, "fechaRetiro"),
                    observacion = ReadNullableString(reader, "observacion"),
                    convalidacion = ReadNullableInt(reader, "convalidacion"),
                    carrera_convalidada = ReadNullableString(reader, "carrera_convalidada"),
                    numero_permiso = ReadNullableInt(reader, "numero_permiso"),
                    user_matricula = ReadNullableString(reader, "user_matricula"),
                    valida = ReadInt(reader, "valida"),
                    esOyente = ReadNullableInt(reader, "esOyente"),
                    documentoFactura = ReadNullableString(reader, "documentoFactura")
                });

        public Task<IEnumerable<CentralAsignacionInstructorVehiculoDto>> GetInstructorVehicleAssignmentsFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idAsignacion, idVehiculo, idProfesor, fecha_asignacion, fecha_salidad, CAST(activo AS SIGNED) AS activo, usuario_asigna, usuario_desactiva, observacion
                  FROM asignacion_instructores_vehiculos",
                reader => new CentralAsignacionInstructorVehiculoDto
                {
                    idAsignacion = ReadInt(reader, "idAsignacion"),
                    idVehiculo = ReadInt(reader, "idVehiculo"),
                    idProfesor = ReadString(reader, "idProfesor"),
                    fecha_asignacion = ReadNullableDate(reader, "fecha_asignacion"),
                    fecha_salidad = ReadNullableDate(reader, "fecha_salidad"),
                    activo = ReadInt(reader, "activo"),
                    usuario_asigna = ReadNullableString(reader, "usuario_asigna"),
                    usuario_desactiva = ReadNullableString(reader, "usuario_desactiva"),
                    observacion = ReadNullableString(reader, "observacion")
                });

        public Task<IEnumerable<CentralAsignacionAlumnoVehiculoDto>> GetStudentVehicleAssignmentsFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idAsignacion, idAlumno, idVehiculo, idProfesor, idPeriodo, fechaAsignacion, fechaInicio, fechaFin, observacion, CAST(activa AS SIGNED) AS activa
                  FROM cond_alumnos_vehiculos",
                reader => new CentralAsignacionAlumnoVehiculoDto
                {
                    idAsignacion = ReadInt(reader, "idAsignacion"),
                    idAlumno = ReadString(reader, "idAlumno"),
                    idVehiculo = ReadInt(reader, "idVehiculo"),
                    idProfesor = ReadString(reader, "idProfesor"),
                    idPeriodo = ReadNullableString(reader, "idPeriodo"),
                    fechaAsignacion = ReadNullableDate(reader, "fechaAsignacion"),
                    fechaInicio = ReadNullableDate(reader, "fechaInicio"),
                    fechaFin = ReadNullableDate(reader, "fechaFin"),
                    observacion = ReadNullableString(reader, "observacion"),
                    activa = ReadInt(reader, "activa")
                });

        public Task<IEnumerable<CentralHorarioDto>> GetAllSchedulesFromCentralAsync() =>
            QueryListAsync(
                @"SELECT 
                    h.idAsignacionHorario, h.idAsignacion, h.idFecha, h.idHora,
                    CAST(h.asiste AS SIGNED) AS asiste,
                    CAST(COALESCE(h.activo, 1) AS SIGNED) AS activo,
                    h.observacion,
                    hc.hora_inicio AS HoraInicio, hc.hora_fin AS HoraFin,
                    fh.fecha AS FechaReal, fh.finsemana AS FinSemana
                  FROM cond_alumnos_horarios h
                  JOIN horas_clases hc ON hc.idhora = h.idHora
                  JOIN fechas_horarios fh ON fh.idFecha = h.idFecha",
                MapCentralHorario);

        public Task<IEnumerable<CentralPracticaHorarioDto>> GetPracticeScheduleLinksFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idPractica, idAsignacionHorario FROM cond_practicas_horarios_alumnos",
                reader => new CentralPracticaHorarioDto
                {
                    idPractica = ReadInt(reader, "idPractica"),
                    idAsignacionHorario = ReadInt(reader, "idAsignacionHorario")
                });

        public Task<IEnumerable<CentralMatriculaExamenDto>> GetMatriculaExamLinksFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idMatricula, idCategoria, CAST(nota AS SIGNED) AS nota, observacion, usuario, fechaExamen, fechaIngreso, instructor
                  FROM matriculas_examen_conduccion",
                reader => new CentralMatriculaExamenDto
                {
                    idMatricula = ReadInt(reader, "idMatricula"),
                    IdCategoria = ReadInt(reader, "idCategoria"),
                    nota = ReadNullableInt(reader, "nota"),
                    observacion = ReadNullableString(reader, "observacion"),
                    usuario = ReadNullableString(reader, "usuario"),
                    fechaExamen = ReadNullableDate(reader, "fechaExamen"),
                    fechaIngreso = ReadNullableDate(reader, "fechaIngreso"),
                    instructor = ReadNullableString(reader, "instructor")
                });

        public Task<IEnumerable<CentralInstitucionDto>> GetAllInstitucionesFromCentralAsync()
            => QueryListAsync("SELECT * FROM instituciones", MapCentralInstitucion);

        public Task<IEnumerable<CentralPracticaSyncDto>> GetAllPracticesFromCentralAsync()
            => QueryListAsync("SELECT * FROM cond_alumnos_practicas", MapCentralPracticaSync);

        public Task<IEnumerable<CentralPeriodoDto>> GetAllPeriodosFromCentralAsync()
            => QueryListAsync("SELECT * FROM periodos", MapCentralPeriodo);

        public Task<IEnumerable<CentralCarreraDto>> GetAllCarrerasFromCentralAsync()
            => QueryListAsync("SELECT * FROM carreras", MapCentralCarrera);

        public Task<IEnumerable<CentralSeccionDto>> GetAllSeccionesFromCentralAsync()
            => QueryListAsync("SELECT * FROM secciones", MapCentralSeccion);

        public Task<IEnumerable<CentralModalidadDto>> GetAllModalidadesFromCentralAsync()
            => QueryListAsync("SELECT * FROM modalidades", MapCentralModalidad);

        public Task<IEnumerable<CentralFechaHorarioDto>> GetAllFechasHorariosFromCentralAsync()
            => QueryListAsync("SELECT * FROM fechas_horarios", MapCentralFechaHorario);

        public Task<IEnumerable<CentralHoraClaseDto>> GetAllHorasClasesFromCentralAsync()
            => QueryListAsync("SELECT * FROM horas_clases", MapCentralHoraClase);

        public Task<IEnumerable<CentralHorarioProfesorDto>> GetAllHorariosProfesoresFromCentralAsync()
            => QueryListAsync("SELECT * FROM horario_profesores", MapCentralHorarioProfesor);

        private static CentralPracticaSyncDto MapCentralPracticaSync(MySqlDataReader reader) => new()
        {
            idPractica = ReadInt(reader, "idPractica"),
            idalumno = ReadString(reader, "idalumno"),
            idvehiculo = ReadInt(reader, "idvehiculo"),
            idProfesor = ReadString(reader, "idProfesor"),
            idPeriodo = ReadNullableString(reader, "idPeriodo"),
            dia = ReadNullableString(reader, "dia"),
            fecha = ReadDate(reader, "fecha"),
            hora_salida = ReadNullableTime(reader, "hora_salida"),
            hora_llegada = ReadNullableTime(reader, "hora_llegada"),
            tiempo = ReadNullableTime(reader, "tiempo"),
            ensalida = ReadInt(reader, "ensalida"),
            verificada = ReadInt(reader, "verificada"),
            user_asigna = ReadNullableString(reader, "user_asigna"),
            user_llegada = ReadNullableString(reader, "user_llegada"),
            cancelado = ReadInt(reader, "cancelado")
        };

        public async Task<bool> PingSigafiAsync()
        {
            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand("SELECT 1", conn);
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result) == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo conectar a SIGAFI con la conexión dedicada.");
                return false;
            }
        }

        public void InvalidateSigafiCatalogCache()
        {
            _cache.Remove("sigafi:instructores");
            _cache.Remove("sigafi:vehiculos");
            _cache.Remove("sigafi:cursos");
            _cache.Remove("sigafi:licencias");
            _logger.LogInformation("Caché de catálogos SIGAFI invalidado (pre-sync).");
        }

        public async Task<IDictionary<string, CentralHorarioDto>> GetNextSchedulesForAlumnosAsync(IEnumerable<string> ids)
        {
            // [TRIM RESILIENCE] Aseguramos que los IDs no tengan espacios para que el IN clause no falle
            var studentIds = ids?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList() ?? new List<string>();
                
            if (studentIds.Count == 0)
                return new Dictionary<string, CentralHorarioDto>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                var inClause = string.Join(",", studentIds.Select((_, i) => $"@p{i}"));
                var sql = $@"
                    SELECT 
                        h.idAsignacionHorario, 
                        h.idAsignacion,
                        h.idFecha, h.idHora, CAST(h.asiste AS SIGNED) AS asiste, CAST(COALESCE(h.activo, 1) AS SIGNED) AS activo, h.observacion,
                        av.idAlumno AS AlumnoId, fh.fecha AS FechaReal, hc.hora_inicio AS HoraInicio,
                        hc.hora_fin AS HoraFin, fh.finsemana AS FinSemana,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoPlanificado,
                        CONCAT_WS(' ', pr.apellidos, pr.nombres) AS InstructorPlanificado
                    FROM cond_alumnos_horarios h
                    JOIN cond_alumnos_vehiculos av ON av.idAsignacion = h.idAsignacion
                    JOIN fechas_horarios fh ON fh.idFecha = h.idFecha
                    JOIN horas_clases hc ON hc.idhora = h.idHora
                    LEFT JOIN vehiculos v ON v.idVehiculo = av.idVehiculo
                    LEFT JOIN profesores pr ON pr.idProfesor = av.idProfesor
                    WHERE TRIM(av.idAlumno) IN ({inClause})
                    AND fh.fecha >= DATE_SUB(CURDATE(), INTERVAL 1 DAY)
                    ORDER BY fh.fecha ASC, hc.hora_inicio ASC";

                        

                var rows = await QueryListAsync(sql, studentIds, reader => {
                    var dto = MapCentralHorario(reader);
                    return new { AlumnoId = ReadString(reader, "AlumnoId").Trim(), Dto = dto };
                });

                var dict = new Dictionary<string, CentralHorarioDto>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in rows)
                {
                    if (!dict.ContainsKey(item.AlumnoId))
                        dict[item.AlumnoId] = item.Dto;
                }
                return dict;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando próximos horarios por lote.");
                return new Dictionary<string, CentralHorarioDto>(StringComparer.Ordinal);
            }
        }

        private Task<IEnumerable<T>> QueryListAsync<T>(string sql, Func<MySqlDataReader, T> mapper)
            => _pipeline.ExecuteAsync(async () =>
            {
                var list = new List<T>();
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) list.Add(mapper(reader));
                return (IEnumerable<T>)list;
            });

        private Task<IEnumerable<T>> QueryListAsync<T>(string sql, IReadOnlyList<string> paramValues, Func<MySqlDataReader, T> mapper)
            => _pipeline.ExecuteAsync(async () =>
            {
                var list = new List<T>();
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand(sql, conn);
                for (var i = 0; i < paramValues.Count; i++)
                    cmd.Parameters.AddWithValue($"@p{i}", paramValues[i]);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) list.Add(mapper(reader));
                return (IEnumerable<T>)list;
            });

        private Task<T?> QuerySingleAsync<T>(string sql, string parameterValue, Func<MySqlDataReader, T> mapper) where T : class
            => _pipeline.ExecuteAsync(async () =>
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p0", parameterValue);
                await using var reader = await cmd.ExecuteReaderAsync();
                return await reader.ReadAsync() ? mapper(reader) : (T?)null;
            });

        private static string? ReadNumeroVehiculoFlexible(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            if (reader.IsDBNull(ord)) return null;
            return reader.GetValue(ord)?.ToString();
        }


        private static CentralStudentDto MapCentralStudent(MySqlDataReader reader) => new()
        {
            idMatricula = ReadInt(reader, "idMatricula"),
            idAlumno = ReadString(reader, "idAlumno"),
            tipoDocumento = ReadNullableString(reader, "tipoDocumento"),
            primerNombre = ReadNullableString(reader, "primerNombre"),
            segundoNombre = ReadNullableString(reader, "segundoNombre"),
            apellidoPaterno = ReadNullableString(reader, "apellidoPaterno"),
            apellidoMaterno = ReadNullableString(reader, "apellidoMaterno"),
            fecha_Nacimiento = ReadNullableDate(reader, "fecha_Nacimiento"),
            direccion = ReadNullableString(reader, "direccion"),
            telefono = ReadNullableString(reader, "telefono"),
            celular = ReadNullableString(reader, "celular"),
            email = ReadNullableString(reader, "email"),
            sexo = ReadNullableString(reader, "sexo"),
            idNivel = ReadInt(reader, "idNivel"),
            idPeriodo = ReadString(reader, "idPeriodo"),
            idSeccion = ReadInt(reader, "idSeccion"),
            idModalidad = ReadInt(reader, "idModalidad"),
            idInstitucion = ReadNullableInt(reader, "idInstitucion"),
            tituloColegio = ReadNullableString(reader, "tituloColegio"),
            fecha_Inscripcion = ReadNullableDate(reader, "fecha_Inscripcion"),
            email_institucional = ReadNullableString(reader, "email_institucional"),
            primerIngreso = ReadNullableInt(reader, "primerIngreso"),
            
            paralelo = ReadNullableString(reader, "paralelo"),
            seccion = ReadNullableString(reader, "seccion"),
            NombreCompleto = ReadNullableString(reader, "NombreCompleto"),
            DetalleRaw = ReadNullableString(reader, "DetalleRaw"),
            Nivel = ReadNullableString(reader, "Nivel"),
            CarreraNombre = ReadNullableString(reader, "CarreraNombre"),
            NivelCurso = ReadNullableString(reader, "NivelCurso")
        };

        private static CentralPeriodoDto MapCentralPeriodo(MySqlDataReader reader) => new()
        {
            idPeriodo = ReadString(reader, "idPeriodo"),
            detalle = ReadNullableString(reader, "detalle"),
            fecha_inicial = ReadNullableDate(reader, "fecha_inicial"),
            fecha_final = ReadNullableDate(reader, "fecha_final"),
            cerrado = ReadInt(reader, "cerrado"),
            fecha_maxima_autocierre = ReadNullableDate(reader, "fecha_maxima_autocierre"),
            activo = ReadInt(reader, "activo"),
            creditos = ReadInt(reader, "creditos"),
            numero_pagos = ReadInt(reader, "numero_pagos"),
            fecha_matrucla_extraordinaria = ReadNullableDate(reader, "fecha_matrucla_extraordinaria"),
            foliop = ReadNullableInt(reader, "foliop"),
            permiteMatricula = ReadInt(reader, "permiteMatricula"),
            ingresoCalificaciones = ReadInt(reader, "ingresoCalificaciones"),
            permiteCalificacionesInstituto = ReadInt(reader, "permiteCalificacionesInstituto"),
            periodoactivoinstituto = ReadInt(reader, "periodoactivoinstituto"),
            visualizaPowerBi = ReadInt(reader, "visualizaPowerBi"),
            esInstituto = ReadInt(reader, "esInstituto"),
            periodoPlanificacion = ReadInt(reader, "periodoPlanificacion")
        };

        private static CentralCarreraDto MapCentralCarrera(MySqlDataReader reader) => new()
        {
            idCarrera = ReadInt(reader, "idCarrera"),
            Carrera = ReadNullableString(reader, "Carrera"),
            fechaCreacion = ReadNullableDate(reader, "fechaCreacion"),
            activa = ReadInt(reader, "activa"),
            directorCarrera = ReadNullableString(reader, "directorCarrera"),
            numero_creditos = ReadNullableInt(reader, "numero_creditos"),
            ordenCarrera = ReadInt(reader, "ordenCarrera"),
            numero_alumnos = ReadNullableInt(reader, "numero_alumnos"),
            revisaArrastres = ReadInt(reader, "revisaArrastres"),
            codigo_cases = ReadNullableString(reader, "codigo_cases"),
            aliasCarrera = ReadNullableString(reader, "aliasCarrera"),
            esInstituto = ReadInt(reader, "esInstituto")
        };

        private static CentralSeccionDto MapCentralSeccion(MySqlDataReader reader) => new()
        {
            idSeccion = ReadInt(reader, "idSeccion"),
            seccion = ReadNullableString(reader, "seccion"),
            sufijo = ReadNullableString(reader, "sufijo")
        };

        private static CentralModalidadDto MapCentralModalidad(MySqlDataReader reader) => new()
        {
            idModalidad = ReadInt(reader, "idModalidad"),
            modalidad = ReadNullableString(reader, "modalidad"),
            sufijo = ReadNullableString(reader, "sufijo")
        };

        private static CentralInstitucionDto MapCentralInstitucion(MySqlDataReader reader) => new()
        {
            idInstitucion = ReadInt(reader, "idInstitucion"),
            Institucion = ReadNullableString(reader, "Institucion"),
            ciudad = ReadNullableString(reader, "ciudad"),
            provincia = ReadNullableString(reader, "provincia")
        };

        private static CentralFechaHorarioDto MapCentralFechaHorario(MySqlDataReader reader) => new()
        {
            idFecha = ReadInt(reader, "idFecha"),
            fecha = ReadDate(reader, "fecha"),
            finsemana = ReadInt(reader, "finsemana"),
            dia = ReadNullableString(reader, "dia")
        };

        private static CentralHorarioProfesorDto MapCentralHorarioProfesor(MySqlDataReader reader) => new()
        {
            idHorario = ReadInt(reader, "idHorario"),
            idAsignacion = ReadInt(reader, "idAsignacion"),
            idHora = ReadInt(reader, "idHora"),
            idFecha = ReadInt(reader, "idFecha"),
            asiste = ReadInt(reader, "asiste"),
            activo = ReadInt(reader, "activo")
        };

        private static CentralHoraClaseDto MapCentralHoraClase(MySqlDataReader reader) => new()
        {
            idhora = ReadInt(reader, "idhora"),
            idSeccion = ReadInt(reader, "idSeccion"),
            idCarrera = ReadInt(reader, "idCarrera"),
            hora_inicio = ReadNullableTime(reader, "hora_inicio"),
            hora_fin = ReadNullableTime(reader, "hora_fin"),
            minutos = ReadInt(reader, "minutos"),
            numero_hora = ReadInt(reader, "numero_hora"),
            tipo = ReadNullableString(reader, "tipo"),
            activo = ReadInt(reader, "activo")
        };



        /// <summary>
        /// Intenta leer la etiqueta de modalidad/jornada en SIGAFI (nombres de columna varían entre instalaciones).
        /// </summary>
        private async Task<string?> TryGetModalidadDescripcionAsync(int idModalidad)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            foreach (var sql in new[]
                     {
                         "SELECT TRIM(modalidad) AS t FROM modalidades WHERE idModalidad = @p0 LIMIT 1",
                         "SELECT TRIM(descripcion) AS t FROM modalidades WHERE idModalidad = @p0 LIMIT 1"
                     })
            {
                try
                {
                    await using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p0", idModalidad);
                    var o = await cmd.ExecuteScalarAsync();
                    var s = o?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(s))
                        return s;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Lectura modalidad SIGAFI omitida (sql incompatible con esta BD).");
                }
            }

            return null;
        }

        private static CentralInstructorDto MapCentralInstructor(MySqlDataReader reader) => new()
        {
            idProfesor = ReadString(reader, "idProfesor"),
            tipodocumento = ReadNullableString(reader, "tipodocumento"),
            apellidos = ReadNullableString(reader, "apellidos"),
            nombres = ReadNullableString(reader, "nombres"),
            primerApellido = ReadNullableString(reader, "primerApellido"),
            segundoApellido = ReadNullableString(reader, "segundoApellido"),
            primerNombre = ReadNullableString(reader, "primerNombre"),
            segundoNombre = ReadNullableString(reader, "segundoNombre"),
            direccion = ReadNullableString(reader, "direccion"),
            callePrincipal = ReadNullableString(reader, "callePrincipal"),
            calleSecundaria = ReadNullableString(reader, "calleSecundaria"),
            numeroCasa = ReadNullableString(reader, "numeroCasa"),
            telefono = ReadNullableString(reader, "telefono"),
            celular = ReadNullableString(reader, "celular"),
            email = ReadNullableString(reader, "email"),
            fecha_nacimiento = ReadNullableDate(reader, "fecha_nacimiento"),
            sexo = ReadNullableString(reader, "sexo"),
            clave = ReadNullableString(reader, "clave"),
            practicas = ReadNullableInt(reader, "practicas"),
            tipo = ReadNullableString(reader, "tipo"),
            titulo = ReadNullableString(reader, "titulo"),
            abreviatura = ReadNullableString(reader, "abreviatura"),
            abreviatura_post = ReadNullableString(reader, "abreviatura_post"),
            activo = ReadInt(reader, "activo"),
            emailInstitucional = ReadNullableString(reader, "emailInstitucional"),
            fecha_ingreso = ReadNullableDate(reader, "fecha_ingreso"),
            fechaIngresoIess = ReadNullableDate(reader, "fechaIngresoIess"),
            fecha_retiro = ReadNullableDate(reader, "fecha_retiro"),
            tipoSangre = ReadNullableString(reader, "tipoSangre"),
            esReal = ReadNullableInt(reader, "esReal")
        };

        private static ScheduledPracticeDto MapScheduledPractice(MySqlDataReader reader)
        {
            var cancelado = ReadInt(reader, "SigafiCancelado");
            var ensalida = ReadInt(reader, "SigafiEnsalida");
            var llegada = ReadNullableTime(reader, "SigafiHoraLlegada");
            var idPer = ReadNullableString(reader, "idPeriodo")?.Trim();
            
            int? idAsigHorario = null;
            try { idAsigHorario = ReadNullableInt(reader, "idAsignacionHorario"); } catch { }

            string? hpInicio = null;
            try { hpInicio = ReadNullableString(reader, "HoraPlanificadaInicio"); } catch { }

            string? hpFin = null;
            try { hpFin = ReadNullableString(reader, "HoraPlanificadaFin"); } catch { }

            bool esPlanificado = false;
            try { esPlanificado = ReadInt(reader, "EsPlanificado") == 1; } catch { }

            return new ScheduledPracticeDto
            {
                idPractica = ReadInt(reader, "idPractica"),
                idalumno = ReadString(reader, "idalumno"),
                idvehiculo = ReadInt(reader, "idvehiculo"),
                idProfesor = ReadString(reader, "idProfesor"),
                idPeriodo = string.IsNullOrEmpty(idPer) ? null : idPer,
                fecha = ReadDate(reader, "fecha"),
                hora_salida = ReadNullableTime(reader, "hora_salida"),
                SigafiCancelado = cancelado,
                SigafiEnsalida = ensalida,
                SigafiHoraLlegada = llegada,
                AlumnoNombre = ReadString(reader, "AlumnoNombre"),
                VehiculoDetalle = ReadString(reader, "VehiculoDetalle"),
                ProfesorNombre = ReadString(reader, "ProfesorNombre"),
                EstadoOperativo = EstadoOperativoDesdeCamposPractica(cancelado, ensalida, llegada),
                idAsignacionHorario = idAsigHorario,
                HoraPlanificadaInicio = hpInicio,
                HoraPlanificadaFin = hpFin,
                EsPlanificado = esPlanificado
            };
        }

        private static string EstadoOperativoDesdeCamposPractica(int cancelado, int ensalida, TimeSpan? horaLlegada)
        {
            if (cancelado != 0)
                return "cancelada";
            if (horaLlegada != null)
                return "completada";
            if (ensalida == 1)
                return "en_pista";
            return "pendiente";
        }

        private static CentralHorarioDto MapCentralHorario(MySqlDataReader reader) => new()
        {
            idAsignacionHorario = ReadInt(reader, "idAsignacionHorario"),
            idAsignacion = ReadInt(reader, "idAsignacion"),
            idFecha = ReadNullableInt(reader, "idFecha"),
            idHora = ReadNullableInt(reader, "idHora"),
            asiste = ReadInt(reader, "asiste"),
            activo = ReadInt(reader, "activo"),
            observacion = ReadNullableString(reader, "observacion"),
            HoraInicio = ReadNullableString(reader, "HoraInicio"),
            HoraFin = ReadNullableString(reader, "HoraFin"),
            FechaReal = ReadNullableDate(reader, "FechaReal"),

            FinSemana = ReadNullableInt(reader, "FinSemana"),
            VehiculoPlanificado = ReadNullableString(reader, "VehiculoPlanificado"),
            InstructorPlanificado = ReadNullableString(reader, "InstructorPlanificado")
        };


        private static int GetSafeOrdinal(MySqlDataReader reader, string column)
        {
            try { return reader.GetOrdinal(column); }
            catch { return -1; }
        }

        private static string ReadString(MySqlDataReader reader, string column)
        {
            var ord = GetSafeOrdinal(reader, column);
            if (ord == -1) return string.Empty;
            return reader.IsDBNull(ord) ? string.Empty : reader.GetValue(ord)?.ToString() ?? string.Empty;
        }

        private static string? ReadNullableString(MySqlDataReader reader, string column)
        {
            var ord = GetSafeOrdinal(reader, column);
            if (ord == -1) return null;
            return reader.IsDBNull(ord) ? null : reader.GetValue(ord)?.ToString();
        }

        private static int ReadInt(MySqlDataReader reader, string column)
        {
            var ord = GetSafeOrdinal(reader, column);
            if (ord == -1) return 0;
            return reader.IsDBNull(ord) ? 0 : Convert.ToInt32(reader.GetValue(ord));
        }

        private static int? ReadNullableInt(MySqlDataReader reader, string column)
        {
            var ord = GetSafeOrdinal(reader, column);
            if (ord == -1) return null;
            return reader.IsDBNull(ord) ? null : Convert.ToInt32(reader.GetValue(ord));
        }

        private static decimal? ReadNullableDecimal(MySqlDataReader reader, string column)
        {
            var ord = GetSafeOrdinal(reader, column);
            if (ord == -1) return null;
            return reader.IsDBNull(ord) ? null : Convert.ToDecimal(reader.GetValue(ord));
        }

        private static DateTime ReadDate(MySqlDataReader reader, string column)
        {
            var ord = GetSafeOrdinal(reader, column);
            if (ord == -1) return DateTime.MinValue;
            if (reader.IsDBNull(ord)) return DateTime.MinValue;
            var value = reader.GetValue(ord);
            return value is DateTime dt ? dt : DateTime.Parse(value.ToString() ?? DateTime.MinValue.ToString("O"));
        }

        private static DateTime? ReadNullableDate(MySqlDataReader reader, string column)
        {
            var ord = GetSafeOrdinal(reader, column);
            if (ord == -1) return null;
            if (reader.IsDBNull(ord)) return null;
            var value = reader.GetValue(ord);
            if (value is DateTime dt) return dt;
            if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static TimeSpan? ReadNullableTime(MySqlDataReader reader, string column)
        {
            var ord = GetSafeOrdinal(reader, column);
            if (ord == -1) return null;
            if (reader.IsDBNull(ord)) return null;
            var value = reader.GetValue(ord);
            if (value is TimeSpan ts) return ts;
            if (value is DateTime dt) return dt.TimeOfDay;
            return TimeSpan.TryParse(value.ToString(), out var parsed) ? parsed : null;
        }

        private static byte[]? ReadNullableBytes(MySqlDataReader reader, string column)
        {
            var ord = GetSafeOrdinal(reader, column);
            if (ord == -1) return null;
            return reader.IsDBNull(ord) ? null : (byte[])reader.GetValue(ord);
        }
    }
}

