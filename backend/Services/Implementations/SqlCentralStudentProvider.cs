using backend.Data;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using backend.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace backend.Services.Implementations
{
    /**
     * Bridge towards SIGAFI Remote Database (192.168.7.50).
     * Total Alignment 2026: Parity 1:1 with specific column naming.
     */
    public class SqlCentralStudentProvider : ICentralStudentProvider
    {
        private readonly AppDbContext _context;
        private readonly string TABLE_PREFIX;

        public SqlCentralStudentProvider(AppDbContext context, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _context = context;
            string? rawDb = configuration["CentralDbName"] ?? configuration.GetConnectionString("CentralDbName");
            string dbName = (rawDb ?? "sigafi_es").Replace("\"", "").Trim();
            TABLE_PREFIX = string.IsNullOrWhiteSpace(dbName) ? "sigafi_es." : dbName + ".";
        }

        public async Task<CentralStudentDto?> GetFromCentralAsync(string idAlumno)
        {
            try
            {
                // Note: idAlumno, primerNombre, apellidoPaterno match DTO properties exactly.
                string sql = $@"
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
                        a.foto
                    FROM {TABLE_PREFIX}alumnos a
                    JOIN {TABLE_PREFIX}matriculas m ON m.idAlumno = a.idAlumno
                    JOIN {TABLE_PREFIX}periodos p ON p.idPeriodo = m.idPeriodo
                    LEFT JOIN {TABLE_PREFIX}cursos c ON c.idNivel = m.idNivel
                    LEFT JOIN {TABLE_PREFIX}secciones s ON s.idSeccion = m.idSeccion
                    WHERE a.idAlumno = @p0 AND p.activo = 1
                    LIMIT 1";

                var result = await _context.Database.SqlQueryRaw<CentralStudentDto>(sql, idAlumno)
                    .FirstOrDefaultAsync();

                if (result?.foto != null)
                {
                    result.FotoBase64 = Convert.ToBase64String(result.foto);
                }

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<CentralInstructorDto?> GetInstructorFromCentralAsync(string idProfesor)
        {
            try
            {
                string sql = $@"
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
                    FROM {TABLE_PREFIX}profesores
                    WHERE idProfesor = @p0
                    LIMIT 1";

                return await _context.Database.SqlQueryRaw<CentralInstructorDto>(sql, idProfesor).FirstOrDefaultAsync();
            }
            catch (Exception) { return null; }
        }

        public async Task<IEnumerable<CentralInstructorDto>> GetAllInstructorsFromCentralAsync()
        {
            try
            {
                string sql = $@"
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
                    FROM {TABLE_PREFIX}profesores";

                return await _context.Database.SqlQueryRaw<CentralInstructorDto>(sql).ToListAsync();
            }
            catch (Exception) { return new List<CentralInstructorDto>(); }
        }

        public async Task<CentralInstructorDto?> GetAssignedTutorAsync(string idAlumno)
        {
            try
            {
                string sql = $@"
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
                    FROM {TABLE_PREFIX}cond_alumnos_vehiculos v
                    JOIN {TABLE_PREFIX}profesores p ON p.idProfesor = v.idProfesor
                    WHERE v.idAlumno = @p0 AND v.activa = 1
                    LIMIT 1";

                return await _context.Database.SqlQueryRaw<CentralInstructorDto>(sql, idAlumno).FirstOrDefaultAsync();
            }
            catch (Exception) { return null; }
        }

        public async Task<ScheduledPracticeDto?> GetScheduledPracticeAsync(string idAlumno)
        {
            try
            {
                // CRITICAL: lowercase idalumno and idvehiculo mapping to DTO.
                string sql = $@"
                    SELECT
                        p.idPractica,
                        p.idalumno,
                        p.idvehiculo,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS AlumnoNombre,
                        p.idProfesor,
                        p.fecha,
                        p.hora_salida,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoDetalle,
                        CONCAT_WS(' ', pr.apellidos, pr.nombres) AS ProfesorNombre
                    FROM {TABLE_PREFIX}cond_alumnos_practicas p
                    JOIN {TABLE_PREFIX}alumnos a ON a.idAlumno = p.idalumno
                    JOIN {TABLE_PREFIX}vehiculos v ON v.idVehiculo = p.idvehiculo
                    JOIN {TABLE_PREFIX}profesores pr ON pr.idProfesor = p.idProfesor
                    WHERE p.idalumno = @p0
                    AND p.fecha >= CURDATE()
                    ORDER BY p.fecha ASC, p.hora_salida ASC
                    LIMIT 1";

                return await _context.Database.SqlQueryRaw<ScheduledPracticeDto>(sql, idAlumno).FirstOrDefaultAsync();
            }
            catch (Exception) { return null; }
        }

        public async Task<IEnumerable<ScheduledPracticeDto>> GetSchedulesForTodayAsync()
        {
            try
            {
                string sql = $@"
                    SELECT
                        p.idPractica,
                        p.idalumno,
                        p.idvehiculo,
                        CONCAT_WS(' ', a.apellidoPaterno, a.apellidoMaterno, a.primerNombre, a.segundoNombre) AS AlumnoNombre,
                        p.idProfesor,
                        p.fecha,
                        p.hora_salida,
                        CONCAT('#', v.numero_vehiculo, ' (', v.placa, ')') AS VehiculoDetalle,
                        CONCAT_WS(' ', pr.apellidos, pr.nombres) AS ProfesorNombre
                    FROM {TABLE_PREFIX}cond_alumnos_practicas p
                    JOIN {TABLE_PREFIX}alumnos a ON a.idAlumno = p.idalumno
                    JOIN {TABLE_PREFIX}vehiculos v ON v.idVehiculo = p.idvehiculo
                    JOIN {TABLE_PREFIX}profesores pr ON pr.idProfesor = p.idProfesor
                    WHERE p.fecha >= CURDATE()
                    ORDER BY p.fecha ASC, p.hora_salida ASC
                    LIMIT 100";

                return await _context.Database.SqlQueryRaw<ScheduledPracticeDto>(sql).ToListAsync();
            }
            catch (Exception) { return new List<ScheduledPracticeDto>(); }
        }

        public async Task<IEnumerable<CentralUserDto>> GetAllWebUsersAsync()
        {
            try
            {
                // Note: lowercase properties in DTO match sigafi_es table exactly.
                string sql = $@"
                    SELECT
                        usuario,
                        password,
                        salida,
                        ingreso,
                        activo,
                        asistencia,
                        esRrhh
                    FROM {TABLE_PREFIX}usuarios_web";

                return await _context.Database.SqlQueryRaw<CentralUserDto>(sql).ToListAsync();
            }
            catch (Exception) { return new List<CentralUserDto>(); }
        }

        public async Task<CentralHorarioDto?> GetNextScheduleAsync(string idAlumno)
        {
            try
            {
                // Buscamos el horario activo más cercano en el calendario de planificación
                string sql = $@"
                    SELECT 
                        h.idAsignacionHorario,
                        h.idAsignacion,
                        CURDATE() as Fecha, -- Placeholder si no tenemos tabla de fechas externa
                        'PROGRAMADO' as Hora,
                        CAST(h.asiste AS SIGNED) as asiste
                    FROM {TABLE_PREFIX}cond_alumnos_horarios h
                    JOIN {TABLE_PREFIX}cond_alumnos_vehiculos a ON a.idAsignacion = h.idAsignacion
                    WHERE a.idAlumno = @p0 
                    AND h.activo = 1
                    ORDER BY h.idAsignacionHorario DESC
                    LIMIT 1";

                return await _context.Database.SqlQueryRaw<CentralHorarioDto>(sql, idAlumno).FirstOrDefaultAsync();
            }
            catch (Exception) { return null; }
        }
    }
}
