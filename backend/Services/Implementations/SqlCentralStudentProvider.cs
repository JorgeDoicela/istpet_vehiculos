using backend.Data;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace backend.Services.Implementations
{
    /**
     * Real SQL Bridge implementation for the ISTPET Central Database.
     * Ready for when the DBA provides the final database name.
     */
    public class SqlCentralStudentProvider : ICentralStudentProvider
    {
        private readonly AppDbContext _context;
        // CONFIGURACIÓN: El nombre de la BD Central encontrado en el dump SQL.
        private const string CENTRAL_DB_NAME = "sigafi_es";

        public SqlCentralStudentProvider(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CentralStudentDto?> GetFromCentralAsync(string cedula)
        {
            /*
             * USANDO PUENTE SQL REAL:
             * Realizamos un query cross-database (hacia otra BD en el mismo servidor).
             * En EF Core 8 SqlQueryRaw requiere que el resultado mapee todas las propiedades del DTO.
             */
            string sql = $@"
                SELECT
                    a.idAlumno AS Cedula,
                    a.primerNombre AS Nombres,
                    a.apellidoPaterno AS Apellidos,
                    m.paralelo AS Paralelo,
                    'MATUTINA' AS Jornada,
                    CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS NombreCompleto,
                    CONCAT('MATRICULADO: ', p.detalle, ' (', m.paralelo, ')') AS DetalleRaw,
                    CAST(p.idPeriodo AS CHAR) AS Periodo,
                    TO_BASE64(a.foto) AS FotoBase64
                FROM {CENTRAL_DB_NAME}.alumnos a
                JOIN {CENTRAL_DB_NAME}.matriculas m ON m.idAlumno = a.idAlumno
                JOIN {CENTRAL_DB_NAME}.periodos p ON p.idPeriodo = m.idPeriodo
                WHERE a.idAlumno = @p0 AND p.activo = 1
                LIMIT 1";

            var result = await _context.Database.SqlQueryRaw<CentralStudentDto>(sql, cedula)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<CentralInstructorDto?> GetInstructorFromCentralAsync(string cedula)
        {
            try
            {
                string sql = $@"
                    SELECT
                        idProfesor AS Cedula,
                        CONCAT_WS(' ', primerNombre, segundoNombre) AS Nombres,
                        CONCAT_WS(' ', apellidoPaterno, apellidoMaterno) AS Apellidos
                    FROM {CENTRAL_DB_NAME}.profesores
                    WHERE idProfesor = @p0 AND activo = 1
                    LIMIT 1";

                var result = await _context.Database.SqlQueryRaw<CentralInstructorDto>(sql, cedula)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<ScheduledPracticeDto?> GetScheduledPracticeAsync(string cedula)
        {
            try
            {
                // Buscamos si tiene una práctica hoy (CURDATE)
                string sql = $@"
                    SELECT
                        p.idPractica AS IdPractica,
                        p.idalumno AS CedulaAlumno,
                        p.idvehiculo AS IdVehiculo,
                        p.idProfesor AS CedulaProfesor,
                        p.hora_salida AS HoraSalida,
                        CONCAT('#', v.NumeroVehiculo, ' (', v.Placa, ')') AS VehiculoDetalle,
                        CONCAT(pr.apellidos, ' ', pr.nombres) AS ProfesorNombre
                    FROM {CENTRAL_DB_NAME}.cond_alumnos_practicas p
                    JOIN {CENTRAL_DB_NAME}.vehiculo v ON v.IdVehiculo = p.idvehiculo
                    JOIN {CENTRAL_DB_NAME}.profesores pr ON pr.idProfesor = p.idProfesor
                    WHERE p.idalumno = @p0 AND p.fecha = CURDATE()
                    ORDER BY p.hora_salida ASC
                    LIMIT 1";

                var result = await _context.Database.SqlQueryRaw<ScheduledPracticeDto>(sql, cedula)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<IEnumerable<ScheduledPracticeDto>> GetSchedulesForTodayAsync()
        {
            try
            {
                // Buscamos todas las prácticas para hoy (CURDATE)
                string sql = $@"
                    SELECT
                        p.idPractica AS IdPractica,
                        p.idalumno AS CedulaAlumno,
                        p.idvehiculo AS IdVehiculo,
                        p.idProfesor AS CedulaProfesor,
                        p.hora_salida AS HoraSalida,
                        CONCAT('#', v.NumeroVehiculo, ' (', v.Placa, ')') AS VehiculoDetalle,
                        CONCAT_WS(' ', pr.apellidoPaterno, pr.apellidoMaterno, pr.primerNombre, pr.segundoNombre) AS ProfesorNombre
                    FROM {CENTRAL_DB_NAME}.cond_alumnos_practicas p
                    JOIN {CENTRAL_DB_NAME}.vehiculo v ON v.IdVehiculo = p.idvehiculo
                    JOIN {CENTRAL_DB_NAME}.profesores pr ON pr.idProfesor = p.idProfesor
                    WHERE p.fecha = CURDATE()
                    ORDER BY p.hora_salida ASC";

                return await _context.Database.SqlQueryRaw<ScheduledPracticeDto>(sql).ToListAsync();
            }
            catch (Exception)
            {
                return new List<ScheduledPracticeDto>();
            }
        }
    }
}
