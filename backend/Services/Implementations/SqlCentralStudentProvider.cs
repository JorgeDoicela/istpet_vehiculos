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
        private readonly string TABLE_PREFIX;

        public SqlCentralStudentProvider(AppDbContext context, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _context = context;
            // Modo Plug & Play:
            // Si CentralDbName es "" (especialmente en Render/Vercel), usamos prefijo "ext_"
            // Si CentralDbName tiene valor, lo usamos como esquema: "dbName."
            string dbName = (configuration["CentralDbName"] ?? "sigafi_es").Replace("\"", "").Trim();
            
            if (string.IsNullOrWhiteSpace(dbName)) {
                TABLE_PREFIX = "ext_";
            } else {
                TABLE_PREFIX = dbName + ".";
            }
        }

        public async Task<CentralStudentDto?> GetFromCentralAsync(string cedula)
        {
            /*
             * USANDO PUENTE SQL REAL:
             * Realizamos un query cross-database (hacia otra BD en el mismo servidor).
             * En EF Core 8 SqlQueryRaw requiere que el resultado mapee todas las propiedades del DTO.
             */
            try
            {
            string sql = $@"
                SELECT
                    a.idAlumno AS Cedula,
                    a.primerNombre AS Nombres,
                    a.apellidoPaterno AS Apellidos,
                    m.paralelo AS Paralelo,
                    s.seccion AS Jornada,
                    CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS NombreCompleto,
                    CONCAT(c.Nivel, ', PARALELO:', m.paralelo, ' ', s.seccion) AS DetalleRaw,
                    c.Nivel AS CursoDetalle,
                    CAST(p.idPeriodo AS CHAR) AS Periodo,
                    TO_BASE64(a.foto) AS FotoBase64
                FROM {TABLE_PREFIX}alumnos a
                JOIN {TABLE_PREFIX}matriculas m ON m.idAlumno = a.idAlumno
                JOIN {TABLE_PREFIX}periodos p ON p.idPeriodo = m.idPeriodo
                LEFT JOIN {TABLE_PREFIX}cursos c ON c.idNivel = m.idNivel
                LEFT JOIN {TABLE_PREFIX}secciones s ON s.idSeccion = m.idSeccion
                WHERE a.idAlumno = @p0 AND p.activo = 1
                LIMIT 1";

                var result = await _context.Database.SqlQueryRaw<CentralStudentDto>(sql, cedula)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR GetFromCentralAsync ({cedula}): {ex.Message}");
                if(ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                return null;
            }
        }

        public async Task<CentralInstructorDto?> GetInstructorFromCentralAsync(string cedula)
        {
            try
            {
                string sql = $@"
                    SELECT
                        idProfesor AS Cedula,
                        CONCAT_WS(' ', primerNombre, segundoNombre) AS Nombres,
                        CONCAT_WS(' ', primerApellido, segundoApellido) AS Apellidos,
                        celular AS Telefono,
                        email AS Email,
                        COALESCE(activo, 1) AS Activo
                    FROM {TABLE_PREFIX}profesores
                    WHERE idProfesor = @p0
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

        public async Task<IEnumerable<CentralInstructorDto>> GetAllInstructorsFromCentralAsync()
        {
            try
            {
                string sql = $@"
                    SELECT
                        idProfesor AS Cedula,
                        CONCAT_WS(' ', primerNombre, segundoNombre) AS Nombres,
                        CONCAT_WS(' ', primerApellido, segundoApellido) AS Apellidos,
                        celular AS Telefono,
                        email AS Email,
                        COALESCE(activo, 1) AS Activo
                    FROM {TABLE_PREFIX}profesores";

                var list = await _context.Database.SqlQueryRaw<CentralInstructorDto>(sql).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR GetAllInstructorsFromCentralAsync: {ex.Message}");
                return new List<CentralInstructorDto>();
            }
        }
        public async Task<CentralInstructorDto?> GetAssignedTutorAsync(string cedula)
        {
            try
            {
                string sql = $@"
                    SELECT
                        p.idProfesor AS Cedula,
                        CONCAT_WS(' ', p.primerNombre, p.segundoNombre) AS Nombres,
                        CONCAT_WS(' ', p.primerApellido, p.segundoApellido) AS Apellidos,
                        p.celular AS Telefono,
                        p.email AS Email,
                        COALESCE(p.activo, 1) AS Activo
                    FROM {TABLE_PREFIX}cond_alumnos_vehiculos v
                    JOIN {TABLE_PREFIX}profesores p ON p.idProfesor = v.idProfesor
                    WHERE v.idAlumno = @p0 AND v.activa = 1
                    LIMIT 1";

                var result = await _context.Database.SqlQueryRaw<CentralInstructorDto>(sql, cedula)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR GetAssignedTutorAsync: {ex.Message}");
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
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS AlumnoNombre,
                        p.idProfesor AS CedulaProfesor,
                        p.hora_salida AS HoraSalida,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoDetalle,
                        CONCAT_WS(' ', pr.primerApellido, pr.segundoApellido, pr.primerNombre, pr.segundoNombre) AS ProfesorNombre
                    FROM {TABLE_PREFIX}cond_alumnos_practicas p
                    JOIN {TABLE_PREFIX}alumnos a ON a.idAlumno = p.idalumno
                    JOIN {TABLE_PREFIX}vehiculos v ON v.idVehiculo = p.idvehiculo
                    JOIN {TABLE_PREFIX}profesores pr ON pr.idProfesor = p.idProfesor
                    WHERE p.idalumno = @p0 
                    AND p.fecha = (SELECT MAX(fecha) FROM {TABLE_PREFIX}cond_alumnos_practicas)
                    ORDER BY p.hora_salida ASC
                    LIMIT 1";

                var result = await _context.Database.SqlQueryRaw<ScheduledPracticeDto>(sql, cedula)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR GetScheduledPracticeAsync: {ex.Message}");
                if(ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
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
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS AlumnoNombre,
                        p.idProfesor AS CedulaProfesor,
                        p.hora_salida AS HoraSalida,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoDetalle,
                        CONCAT_WS(' ', pr.primerApellido, pr.segundoApellido, pr.primerNombre, pr.segundoNombre) AS ProfesorNombre
                    FROM {TABLE_PREFIX}cond_alumnos_practicas p
                    JOIN {TABLE_PREFIX}alumnos a ON a.idAlumno = p.idalumno
                    JOIN {TABLE_PREFIX}vehiculos v ON v.idVehiculo = p.idvehiculo
                    JOIN {TABLE_PREFIX}profesores pr ON pr.idProfesor = p.idProfesor
                    WHERE p.fecha = (SELECT MAX(fecha) FROM {TABLE_PREFIX}cond_alumnos_practicas)
                    ORDER BY p.hora_salida ASC";

                var list = await _context.Database.SqlQueryRaw<ScheduledPracticeDto>(sql).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR GetSchedulesForTodayAsync: {ex.Message}");
                if(ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                return new List<ScheduledPracticeDto>();
            }
        }
    }
}
