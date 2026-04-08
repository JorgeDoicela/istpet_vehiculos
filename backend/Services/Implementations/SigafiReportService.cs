using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Globalization;

namespace backend.Services.Implementations
{
    /**
     * Lectura de reportes desde MySQL SIGAFI (servidor dedicado o misma instancia con otra base).
     * No usa tablas de istpet_vehiculos salvo que el DBA las tenga en el mismo servidor.
     */
    public class SigafiReportService : ISigafiReportService
    {
        private readonly IConfiguration _configuration;

        public SigafiReportService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ResolveConnectionString()
        {
            var explicitConn = _configuration.GetConnectionString("SigafiConnection");
            if (!string.IsNullOrWhiteSpace(explicitConn))
                return explicitConn;

            var fallback = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(fallback))
                throw new InvalidOperationException("Falta ConnectionStrings:SigafiConnection o DefaultConnection.");

            var dbName = _configuration["ConnectionStrings:CentralDbName"]
                         ?? _configuration["CentralDbName"]
                         ?? "sigafi_es";

            var builder = new MySqlConnectionStringBuilder(fallback)
            {
                Database = dbName.Trim()
            };
            return builder.ConnectionString;
        }

        public async Task<IReadOnlyList<ReportePracticasDTO>> GetReportePracticasAsync(
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? cedulaProfesor)
        {
            var connStr = ResolveConnectionString();
            var culture = new CultureInfo("es-EC");

            var sql = @"
SELECT
    p.idPractica,
    pr.idProfesor,
    TRIM(CONCAT(
        COALESCE(pr.primerApellido, ''), ' ',
        COALESCE(pr.segundoApellido, ''), ' ',
        COALESCE(pr.primerNombre, ''), ' ',
        COALESCE(pr.segundoNombre, '')
    )) AS ProfesorNombre,
    v.numero_vehiculo,
    v.IdTipoVehiculo,
    a.idAlumno,
    TRIM(CONCAT(
        COALESCE(a.apellidoPaterno, ''), ' ',
        COALESCE(a.apellidoMaterno, ''), ' ',
        COALESCE(a.primerNombre, ''), ' ',
        COALESCE(a.segundoNombre, '')
    )) AS AlumnoNombre,
    p.fecha,
    p.hora_salida,
    p.hora_llegada,
    COALESCE(p.cancelado, 0) AS cancelado
FROM cond_alumnos_practicas p
INNER JOIN alumnos a ON a.idAlumno = p.idAlumno
INNER JOIN profesores pr ON pr.idProfesor = p.idProfesor
INNER JOIN vehiculos v ON v.idVehiculo = p.idVehiculo
WHERE COALESCE(p.cancelado, 0) = 0";

            var args = new List<MySqlParameter>();

            if (fechaInicio.HasValue)
            {
                sql += " AND p.fecha >= @d0";
                args.Add(new MySqlParameter("@d0", fechaInicio.Value.Date));
            }

            if (fechaFin.HasValue)
            {
                sql += " AND p.fecha <= @d1";
                args.Add(new MySqlParameter("@d1", fechaFin.Value.Date));
            }

            if (!string.IsNullOrWhiteSpace(cedulaProfesor))
            {
                sql += " AND p.idProfesor = @prof";
                args.Add(new MySqlParameter("@prof", cedulaProfesor.Trim()));
            }

            sql += " ORDER BY p.fecha DESC, p.hora_salida DESC";

            var list = new List<ReportePracticasDTO>();

            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var p in args)
                cmd.Parameters.Add(p);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var idPractica = reader.GetInt32(reader.GetOrdinal("idPractica"));
                var idProf = reader.GetString(reader.GetOrdinal("idProfesor"));
                var profNombre = reader.IsDBNull(reader.GetOrdinal("ProfesorNombre"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("ProfesorNombre"));

                var numOrdinal = reader.GetOrdinal("numero_vehiculo");
                var numeroVeh = reader.IsDBNull(numOrdinal) ? 0 : Convert.ToInt32(reader.GetValue(numOrdinal));

                int? idTipoVeh = null;
                var tipoOrd = reader.GetOrdinal("IdTipoVehiculo");
                if (!reader.IsDBNull(tipoOrd))
                    idTipoVeh = Convert.ToInt32(reader.GetValue(tipoOrd));

                var idAlumno = reader.GetString(reader.GetOrdinal("idAlumno"));
                var alumnoNom = reader.IsDBNull(reader.GetOrdinal("AlumnoNombre"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("AlumnoNombre"));

                var fecha = reader.GetDateTime(reader.GetOrdinal("fecha"));

                var horaSalidaStr = FormatTime(reader, "hora_salida");
                var horaLlegadaStr = reader.IsDBNull(reader.GetOrdinal("hora_llegada"))
                    ? null
                    : FormatTime(reader, "hora_llegada");

                TimeSpan? tsSalida = ReadTimeSpan(reader, "hora_salida");
                TimeSpan? tsLlegada = reader.IsDBNull(reader.GetOrdinal("hora_llegada"))
                    ? null
                    : ReadTimeSpan(reader, "hora_llegada");

                var duracion = TimeSpan.Zero;
                if (tsSalida.HasValue && tsLlegada.HasValue)
                    duracion = tsLlegada.Value - tsSalida.Value;
                if (duracion < TimeSpan.Zero)
                    duracion = TimeSpan.Zero;

                var categoria = idTipoVeh.HasValue
                    ? culture.TextInfo.ToUpper($"Licencia tipo {idTipoVeh.Value}")
                    : culture.TextInfo.ToUpper("Práctica SIGAFI");

                list.Add(new ReportePracticasDTO
                {
                    IdRegistro = idPractica,
                    IdProfesor = idProf,
                    Profesor = culture.TextInfo.ToUpper(profNombre.Trim()),
                    Categoria = categoria,
                    NumeroVehiculo = numeroVeh,
                    IdAlumno = idAlumno,
                    Nomina = culture.TextInfo.ToUpper(alumnoNom.Trim()),
                    Dia = culture.DateTimeFormat.GetDayName(fecha.DayOfWeek).ToLowerInvariant(),
                    Fecha = fecha.ToString("dd/M/yyyy"),
                    HoraSalida = horaSalidaStr,
                    HoraLlegada = horaLlegadaStr,
                    Tiempo = string.Format("{0:00}:{1:00}:{2:00}", (int)duracion.TotalHours, duracion.Minutes, duracion.Seconds),
                    Observaciones = null
                });
            }

            return list;
        }

        private static string FormatTime(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            if (reader.IsDBNull(ord)) return "--:--:--";
            var v = reader.GetValue(ord);
            if (v is TimeSpan ts)
                return string.Format("{0:D2}:{1:D2}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds);
            if (v is DateTime dt)
                return dt.ToString("HH:mm:ss");
            return v?.ToString() ?? "--:--:--";
        }

        private static TimeSpan? ReadTimeSpan(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            if (reader.IsDBNull(ord)) return null;
            var v = reader.GetValue(ord);
            if (v is TimeSpan ts) return ts;
            if (v is DateTime dt) return dt.TimeOfDay;
            return null;
        }
    }
}
