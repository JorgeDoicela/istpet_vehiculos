using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using backend.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using MySqlConnector;
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
        private readonly ILogger<SqlCentralStudentProvider> _logger;

        public SqlCentralStudentProvider(AppDbContext context, IConfiguration configuration, ILogger<SqlCentralStudentProvider> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("SigafiConnection")
                ?? throw new InvalidOperationException("Falta ConnectionStrings:SigafiConnection para leer SIGAFI.");
        }

        public async Task<CentralStudentDto?> GetFromCentralAsync(string idAlumno)
        {
            try
            {
                const string selectBase = @"
                    SELECT
                        a.idAlumno,
                        a.primerNombre,
                        a.apellidoPaterno,
                        a.apellidoMaterno,
                        a.segundoNombre,
                        m.paralelo,
                        s.seccion,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS NombreCompleto,
                        CONCAT(c.Nivel, ', PARALELO:', m.paralelo, ' ', s.seccion) AS DetalleRaw,
                        c.Nivel,
                        p.idPeriodo,
                        a.foto";

                // 1) Preferido: matrícula válida en periodo académico marcado activo en SIGAFI.
                var sqlActivo = selectBase + @"
                    FROM alumnos a
                    JOIN matriculas m ON m.idAlumno = a.idAlumno AND COALESCE(m.valida, 1) = 1
                    JOIN periodos p ON p.idPeriodo = m.idPeriodo
                    LEFT JOIN cursos c ON c.idNivel = m.idNivel
                    LEFT JOIN secciones s ON s.idSeccion = m.idSeccion
                    WHERE a.idAlumno = @p0 AND p.activo = 1
                    LIMIT 1";
                var result = await QuerySingleAsync(sqlActivo, idAlumno, MapCentralStudent);

                // 2) Cualquier matrícula válida (p. ej. alta reciente aún no ligada al periodo activo).
                if (result == null)
                {
                    var sqlCualquierPeriodo = selectBase + @"
                    FROM alumnos a
                    JOIN matriculas m ON m.idAlumno = a.idAlumno AND COALESCE(m.valida, 1) = 1
                    JOIN periodos p ON p.idPeriodo = m.idPeriodo
                    LEFT JOIN cursos c ON c.idNivel = m.idNivel
                    LEFT JOIN secciones s ON s.idSeccion = m.idSeccion
                    WHERE a.idAlumno = @p0
                    ORDER BY (p.activo = 1) DESC, m.idMatricula DESC
                    LIMIT 1";
                    result = await QuerySingleAsync(sqlCualquierPeriodo, idAlumno, MapCentralStudent);
                }

                // 3) Solo ficha en alumnos (evita “no existe” si aún no cargaron matrícula/periodo).
                if (result == null)
                {
                    const string sqlSoloAlumno = @"
                    SELECT
                        a.idAlumno,
                        a.primerNombre,
                        a.apellidoPaterno,
                        a.apellidoMaterno,
                        a.segundoNombre,
                        NULL AS paralelo,
                        NULL AS seccion,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS NombreCompleto,
                        'Alumno en SIGAFI sin matrícula registrada' AS DetalleRaw,
                        NULL AS Nivel,
                        'SIN_MAT' AS idPeriodo,
                        a.foto
                    FROM alumnos a
                    WHERE a.idAlumno = @p0
                    LIMIT 1";
                    result = await QuerySingleAsync(sqlSoloAlumno, idAlumno, MapCentralStudent);
                }

                if (result?.foto != null)
                    result.FotoBase64 = Convert.ToBase64String(result.foto);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando alumno SIGAFI {idAlumno}", idAlumno);
                return null;
            }
        }

        public async Task<CentralInstructorDto?> GetInstructorFromCentralAsync(string idProfesor)
        {
            try
            {
                const string sql = @"
                    SELECT
                        idProfesor,
                        nombres,
                        apellidos,
                        primerApellido,
                        segundoApellido,
                        primerNombre,
                        segundoNombre,
                        celular,
                        email,
                        CAST(activo AS SIGNED) AS activo
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
            try
            {
                const string sql = @"
                    SELECT
                        idProfesor,
                        nombres,
                        apellidos,
                        primerApellido,
                        segundoApellido,
                        primerNombre,
                        segundoNombre,
                        celular,
                        email,
                        CAST(activo AS SIGNED) AS activo
                    FROM profesores";
                return await QueryListAsync(sql, MapCentralInstructor);
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
                        p.nombres,
                        p.apellidos,
                        p.primerApellido,
                        p.segundoApellido,
                        p.primerNombre,
                        p.segundoNombre,
                        p.celular,
                        p.email,
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
                        p.idPractica,
                        p.idalumno,
                        p.idvehiculo,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS AlumnoNombre,
                        p.idProfesor,
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
                    WHERE COALESCE(p.cancelado, 0) = 0
                    ORDER BY p.fecha DESC, p.hora_salida DESC
                    LIMIT {take}";
                return await QueryListAsync(sql, MapScheduledPractice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando prácticas recientes SIGAFI.");
                return new List<ScheduledPracticeDto>();
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
                        h.observacion
                    FROM cond_alumnos_horarios h
                    JOIN cond_alumnos_vehiculos a ON a.idAsignacion = h.idAsignacion
                    WHERE a.idAlumno = @p0 
                    AND COALESCE(h.activo, 1) = 1
                    ORDER BY h.idAsignacionHorario DESC
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

        public Task<IEnumerable<CentralVehiculoDto>> GetAllVehiclesFromCentralAsync() =>
            QueryListAsync(SqlVehiculosSelect, MapCentralVehiculo);

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
                        salida = salidaDt
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

        public Task<IEnumerable<CentralCursoDto>> GetAllCoursesFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idNivel, idCarrera, Nivel, jerarquia, orden, CAST(esRecuperacion AS SIGNED) AS esRecuperacion, aliasCurso FROM cursos",
                reader => new CentralCursoDto
                {
                    idNivel = ReadInt(reader, "idNivel"),
                    idCarrera = ReadInt(reader, "idCarrera"),
                    Nivel = ReadNullableString(reader, "Nivel"),
                    jerarquia = ReadNullableInt(reader, "jerarquia"),
                    orden = ReadNullableInt(reader, "orden"),
                    esRecuperacion = ReadNullableInt(reader, "esRecuperacion"),
                    aliasCurso = ReadNullableString(reader, "aliasCurso")
                });

        /// <summary>
        /// SIGAFI no expone tipo_licencia; se deriva de categoria_vehiculos para poblar tipo_licencia local.
        /// Para categorías "LICENCIA TIPO X" se normaliza el código como X (C/D/E...).
        /// </summary>
        public Task<IEnumerable<CentralTipoLicenciaDto>> GetAllLicenseTypesFromCentralAsync() =>
            QueryListAsync(
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
                });

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
                @"SELECT idCategoria AS IdCategoria, categoria, CAST(tieneNota AS SIGNED) AS tieneNota, CAST(activa AS SIGNED) AS activa FROM categorias_examenes_conduccion",
                reader => new CentralCategoriaExamenDto
                {
                    IdCategoria = ReadInt(reader, "IdCategoria"),
                    categoria = ReadString(reader, "categoria"),
                    tieneNota = ReadInt(reader, "tieneNota"),
                    activa = ReadInt(reader, "activa")
                });

        public Task<IEnumerable<CentralAlumnoLiteDto>> GetAllStudentsFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idAlumno, primerNombre, segundoNombre, apellidoPaterno, apellidoMaterno, celular, email FROM alumnos",
                reader => new CentralAlumnoLiteDto
                {
                    idAlumno = ReadString(reader, "idAlumno"),
                    primerNombre = ReadNullableString(reader, "primerNombre"),
                    segundoNombre = ReadNullableString(reader, "segundoNombre"),
                    apellidoPaterno = ReadNullableString(reader, "apellidoPaterno"),
                    apellidoMaterno = ReadNullableString(reader, "apellidoMaterno"),
                    celular = ReadNullableString(reader, "celular"),
                    email = ReadNullableString(reader, "email")
                });

        public Task<IEnumerable<CentralMatriculaDto>> GetActiveEnrollmentsFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idMatricula, idAlumno, idNivel, COALESCE(idSeccion, 1) AS idSeccion, COALESCE(idModalidad, 1) AS idModalidad, idPeriodo, fechaMatricula, paralelo,
                         CAST(arrastres AS SIGNED) AS arrastres, folio, beca_matricula, CAST(retirado AS SIGNED) AS retirado, CAST(esOyente AS SIGNED) AS esOyente,
                         COALESCE(valida, 1) AS valida
                  FROM matriculas
                  WHERE COALESCE(valida, 1) = 1",
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
                    retirado = ReadNullableInt(reader, "retirado"),
                    esOyente = ReadNullableInt(reader, "esOyente"),
                    valida = ReadInt(reader, "valida")
                });

        public Task<IEnumerable<CentralAsignacionInstructorVehiculoDto>> GetInstructorVehicleAssignmentsFromCentralAsync() =>
            QueryListAsync(
                @"SELECT idAsignacion, idVehiculo, idProfesor, fecha_asignacion, fecha_salidad AS fecha_salida, CAST(activo AS SIGNED) AS activo, usuario_asigna, usuario_desactiva, observacion
                  FROM asignacion_instructores_vehiculos",
                reader => new CentralAsignacionInstructorVehiculoDto
                {
                    idAsignacion = ReadInt(reader, "idAsignacion"),
                    idVehiculo = ReadInt(reader, "idVehiculo"),
                    idProfesor = ReadString(reader, "idProfesor"),
                    fecha_asignacion = ReadNullableDate(reader, "fecha_asignacion"),
                    fecha_salida = ReadNullableDate(reader, "fecha_salida"),
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
                @"SELECT idAsignacionHorario, idAsignacion, idFecha, idHora,
                         CAST(asiste AS SIGNED) AS asiste,
                         CAST(COALESCE(activo, 1) AS SIGNED) AS activo,
                         observacion
                  FROM cond_alumnos_horarios
                  WHERE COALESCE(activo, 1) = 1",
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
                @"SELECT idMatricula, idCategoria, nota, observacion, usuario, fechaExamen, fechaIngreso, instructor
                  FROM matriculas_examen_conduccion",
                reader => new CentralMatriculaExamenDto
                {
                    idMatricula = ReadInt(reader, "idMatricula"),
                    IdCategoria = ReadInt(reader, "idCategoria"),
                    nota = ReadNullableDecimal(reader, "nota"),
                    observacion = ReadNullableString(reader, "observacion"),
                    usuario = ReadNullableString(reader, "usuario"),
                    fechaExamen = ReadNullableDate(reader, "fechaExamen"),
                    fechaIngreso = ReadNullableDate(reader, "fechaIngreso"),
                    instructor = ReadNullableString(reader, "instructor")
                });

        public Task<IEnumerable<CentralPracticaSyncDto>> GetAllPracticesFromCentralAsync() =>
            QueryListAsync(
                // SIGAFI: cond_alumnos_practicas no incluye observaciones; NULL mantiene paridad con origen.
                @"SELECT idPractica, idalumno, idvehiculo, idProfesor, idPeriodo, dia, fecha,
                         hora_salida, hora_llegada, tiempo,
                         CAST(ensalida AS SIGNED) AS ensalida, CAST(verificada AS SIGNED) AS verificada,
                         user_asigna, user_llegada, CAST(cancelado AS SIGNED) AS cancelado,
                         NULL AS observaciones
                  FROM cond_alumnos_practicas",
                reader => new CentralPracticaSyncDto
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
                    cancelado = ReadInt(reader, "cancelado"),
                    observaciones = ReadNullableString(reader, "observaciones")
                });

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

        private async Task<IEnumerable<T>> QueryListAsync<T>(string sql, Func<MySqlDataReader, T> mapper)
        {
            var list = new List<T>();
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(mapper(reader));
            }
            return list;
        }

        private async Task<T?> QuerySingleAsync<T>(string sql, string parameterValue, Func<MySqlDataReader, T> mapper) where T : class
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@p0", parameterValue);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return mapper(reader);
            }
            return null;
        }

        private static int ReadNumeroVehiculoFlexible(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            if (reader.IsDBNull(ord))
                return 0;
            var v = reader.GetValue(ord);
            if (v is int i)
                return i;
            if (v is long l)
                return (int)l;
            return int.TryParse(Convert.ToString(v)?.Trim(), out var n) ? n : 0;
        }

        private static CentralStudentDto MapCentralStudent(MySqlDataReader reader) => new()
        {
            idAlumno = ReadString(reader, "idAlumno"),
            primerNombre = ReadNullableString(reader, "primerNombre"),
            apellidoPaterno = ReadNullableString(reader, "apellidoPaterno"),
            apellidoMaterno = ReadNullableString(reader, "apellidoMaterno"),
            segundoNombre = ReadNullableString(reader, "segundoNombre"),
            paralelo = ReadNullableString(reader, "paralelo"),
            seccion = ReadNullableString(reader, "seccion"),
            NombreCompleto = ReadNullableString(reader, "NombreCompleto"),
            DetalleRaw = ReadNullableString(reader, "DetalleRaw"),
            Nivel = ReadNullableString(reader, "Nivel"),
            idPeriodo = ReadString(reader, "idPeriodo"),
            foto = ReadNullableBytes(reader, "foto")
        };

        private static CentralInstructorDto MapCentralInstructor(MySqlDataReader reader) => new()
        {
            idProfesor = ReadString(reader, "idProfesor"),
            nombres = ReadString(reader, "nombres"),
            apellidos = ReadString(reader, "apellidos"),
            primerApellido = ReadNullableString(reader, "primerApellido"),
            segundoApellido = ReadNullableString(reader, "segundoApellido"),
            primerNombre = ReadNullableString(reader, "primerNombre"),
            segundoNombre = ReadNullableString(reader, "segundoNombre"),
            celular = ReadNullableString(reader, "celular"),
            email = ReadNullableString(reader, "email"),
            activo = ReadInt(reader, "activo")
        };

        private static ScheduledPracticeDto MapScheduledPractice(MySqlDataReader reader)
        {
            var cancelado = ReadInt(reader, "SigafiCancelado");
            var ensalida = ReadInt(reader, "SigafiEnsalida");
            var llegada = ReadNullableTime(reader, "SigafiHoraLlegada");
            return new ScheduledPracticeDto
            {
                idPractica = ReadInt(reader, "idPractica"),
                idalumno = ReadString(reader, "idalumno"),
                idvehiculo = ReadInt(reader, "idvehiculo"),
                idProfesor = ReadString(reader, "idProfesor"),
                fecha = ReadDate(reader, "fecha"),
                hora_salida = ReadNullableTime(reader, "hora_salida"),
                AlumnoNombre = ReadString(reader, "AlumnoNombre"),
                VehiculoDetalle = ReadString(reader, "VehiculoDetalle"),
                ProfesorNombre = ReadString(reader, "ProfesorNombre"),
                EstadoOperativo = EstadoOperativoDesdeCamposPractica(cancelado, ensalida, llegada)
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
            observacion = ReadNullableString(reader, "observacion")
        };

        private static string ReadString(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            return reader.IsDBNull(ord) ? string.Empty : reader.GetValue(ord)?.ToString() ?? string.Empty;
        }

        private static string? ReadNullableString(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            return reader.IsDBNull(ord) ? null : reader.GetValue(ord)?.ToString();
        }

        private static int ReadInt(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            return reader.IsDBNull(ord) ? 0 : Convert.ToInt32(reader.GetValue(ord));
        }

        private static int? ReadNullableInt(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            return reader.IsDBNull(ord) ? null : Convert.ToInt32(reader.GetValue(ord));
        }

        private static decimal? ReadNullableDecimal(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            return reader.IsDBNull(ord) ? null : Convert.ToDecimal(reader.GetValue(ord));
        }

        private static DateTime ReadDate(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            if (reader.IsDBNull(ord)) return DateTime.MinValue;
            var value = reader.GetValue(ord);
            return value is DateTime dt ? dt : DateTime.Parse(value.ToString() ?? DateTime.MinValue.ToString("O"));
        }

        private static DateTime? ReadNullableDate(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            if (reader.IsDBNull(ord)) return null;
            var value = reader.GetValue(ord);
            if (value is DateTime dt) return dt;
            if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static TimeSpan? ReadNullableTime(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            if (reader.IsDBNull(ord)) return null;
            var value = reader.GetValue(ord);
            if (value is TimeSpan ts) return ts;
            if (value is DateTime dt) return dt.TimeOfDay;
            return TimeSpan.TryParse(value.ToString(), out var parsed) ? parsed : null;
        }

        private static byte[]? ReadNullableBytes(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            return reader.IsDBNull(ord) ? null : (byte[])reader.GetValue(ord);
        }
    }
}
