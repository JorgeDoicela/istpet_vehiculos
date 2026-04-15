using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Globalization;
using System.Linq;

namespace backend.Services.Implementations
{
    /**
     * Reportes contra la BD SIGAFI del servidor configurado.
     * Refactored 2026 for Absolute Parity: idAlumno, idProfesor, numeroVehiculo, etc.
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
                Database = dbName.Trim(),
                ConnectionTimeout = 10, // Máximo 10 segundos esperando al servidor remoto
                DefaultCommandTimeout = 20
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
    p.idPractica AS id_practica,
    p.idProfesor AS id_profesor,
    TRIM(CONCAT(
        COALESCE(pr.primerApellido, ''), ' ',
        COALESCE(pr.segundoApellido, ''), ' ',
        COALESCE(pr.primerNombre, ''), ' ',
        COALESCE(pr.segundoNombre, '')
    )) AS profesor_nombre,
    v.numero_vehiculo AS numero_vehiculo,
    v.placa AS placa,
    v.Marca AS marca,
    v.Modelo AS modelo,
    cv.categoria AS tipo_licencia,
    p.idalumno AS id_alumno,
    TRIM(CONCAT(
        COALESCE(a.apellidoPaterno, ''), ' ',
        COALESCE(a.apellidoMaterno, ''), ' ',
        COALESCE(a.primerNombre, ''), ' ',
        COALESCE(a.segundoNombre, '')
    )) AS alumno_nombre,
    p.fecha AS fecha_practica,
    p.hora_salida AS hora_salida,
    p.hora_llegada AS hora_llegada,
    COALESCE(p.cancelado, 0) AS cancelado,
    p.user_asigna AS user_asigna
FROM cond_alumnos_practicas p
LEFT JOIN alumnos a ON a.idAlumno = p.idalumno
LEFT JOIN profesores pr ON pr.idProfesor = p.idProfesor
LEFT JOIN vehiculos v ON v.idVehiculo = p.idvehiculo
LEFT JOIN categoria_vehiculos cv ON cv.idCategoria = v.idCategoria
WHERE 1=1";

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
            Console.WriteLine($"[SIGAFI-REPORT] Conectando a SIGAFI... {connStr.Split(';').FirstOrDefault(x => x.StartsWith("Server"))}");
            await conn.OpenAsync();
            
            await using var cmd = new MySqlCommand(sql, conn)
            {
                CommandTimeout = 20 // Segundos de gracia para reportes pesados
            };
            foreach (var p in args)
                cmd.Parameters.Add(p);

            Console.WriteLine($"[SIGAFI-REPORT] Ejecutando query remota...");
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var idPractica = reader.GetInt32(reader.GetOrdinal("id_practica"));
                var idProf = reader.IsDBNull(reader.GetOrdinal("id_profesor")) ? "---" : reader.GetString(reader.GetOrdinal("id_profesor"));
                var profNombre = reader.IsDBNull(reader.GetOrdinal("profesor_nombre"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("profesor_nombre"));

                var numOrdinal = reader.GetOrdinal("numero_vehiculo");
                var numeroVeh = reader.IsDBNull(numOrdinal) ? "0" : reader.GetValue(numOrdinal).ToString() ?? "0";


                var placaOrd = reader.GetOrdinal("placa");
                var placa = reader.IsDBNull(placaOrd) ? "" : reader.GetString(placaOrd);
                var marca = reader.IsDBNull(reader.GetOrdinal("marca")) ? "" : reader.GetString(reader.GetOrdinal("marca")).Trim();
                var modelo = reader.IsDBNull(reader.GetOrdinal("modelo")) ? "" : reader.GetString(reader.GetOrdinal("modelo")).Trim();

                var idAlumno = reader.IsDBNull(reader.GetOrdinal("id_alumno")) ? "---" : reader.GetString(reader.GetOrdinal("id_alumno"));
                var alumnoNom = reader.IsDBNull(reader.GetOrdinal("alumno_nombre"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("alumno_nombre"));

                var fecha = reader.GetDateTime(reader.GetOrdinal("fecha_practica"));

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

                var tipoLicencia = reader.IsDBNull(reader.GetOrdinal("tipo_licencia")) ? "" : reader.GetString(reader.GetOrdinal("tipo_licencia")).Trim();
                var categoria = !string.IsNullOrWhiteSpace(tipoLicencia)
                    ? culture.TextInfo.ToUpper(tipoLicencia)
                    : culture.TextInfo.ToUpper("LICENCIA TIPO C");

                list.Add(new ReportePracticasDTO
                {
                    idPractica = idPractica,
                    idProfesor = idProf,
                    profesor = culture.TextInfo.ToUpper(profNombre.Trim()),
                    categoria = categoria,
                    numeroVehiculo = numeroVeh.ToString(),
                    idAlumno = idAlumno,
                    nomina = culture.TextInfo.ToUpper(alumnoNom.Trim()),
                    dia = culture.DateTimeFormat.GetDayName(fecha.DayOfWeek).ToLowerInvariant(),
                    fecha = fecha.ToString("dd/M/yyyy"),
                    horaSalida = horaSalidaStr,
                    horaLlegada = horaLlegadaStr,
                    tiempo = string.Format("{0:00}:{1:00}:{2:00}", (int)duracion.TotalHours, duracion.Minutes, duracion.Seconds),
                    observaciones = ReadOptionalString(reader, "user_asigna"),
                    cancelado = reader.IsDBNull(reader.GetOrdinal("cancelado")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("cancelado")))
                });
            }

            return list;
        }

        private static string? ReadOptionalString(MySqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            return reader.IsDBNull(ord) ? null : reader.GetString(ord);
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
