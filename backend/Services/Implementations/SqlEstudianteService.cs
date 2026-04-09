using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    /**
     * Student Search Service: Refactored for Absolute SIGAFI Parity 2026.
     * Si no está en el espejo local, consulta SIGAFI y materializa el alumno (sin depender del cron de sync).
     */
    public class SqlEstudianteService : IEstudianteService
    {
        private readonly AppDbContext _context;
        private readonly ICentralStudentProvider _central;
        private readonly ILogger<SqlEstudianteService> _logger;

        public SqlEstudianteService(
            AppDbContext context,
            ICentralStudentProvider central,
            ILogger<SqlEstudianteService> logger)
        {
            _context = context;
            _central = central;
            _logger = logger;
        }

        public async Task<Estudiante?> GetByIdAlumnoAsync(string idAlumno)
        {
            var local = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idAlumno == idAlumno);
            if (local != null)
                return local;

            CentralStudentDto? central;
            try
            {
                central = await _central.GetFromCentralAsync(idAlumno);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SIGAFI no disponible al resolver alumno {Id}", idAlumno);
                return null;
            }

            if (central == null)
                return null;

            try
            {
                local = new Estudiante
                {
                    idAlumno = central.idAlumno,
                    primerNombre = (central.primerNombre ?? "S/N").ToUpper(),
                    segundoNombre = (central.segundoNombre ?? "").ToUpper(),
                    apellidoPaterno = (central.apellidoPaterno ?? "S/N").ToUpper(),
                    apellidoMaterno = (central.apellidoMaterno ?? "").ToUpper()
                };
                _context.Estudiantes.Add(local);
                await _context.SaveChangesAsync();
                return local;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo persistir alumno {Id} desde SIGAFI; se devuelve null.", idAlumno);
                return null;
            }
        }

        public async Task<IEnumerable<Estudiante>> GetAllAsync()
        {
            var lites = await _central.GetAllStudentsFromCentralAsync();
            return lites
                .Select(x => new Estudiante
                {
                    idAlumno = x.idAlumno,
                    primerNombre = (x.primerNombre ?? "").ToUpper(),
                    segundoNombre = (x.segundoNombre ?? "").ToUpper(),
                    apellidoPaterno = (x.apellidoPaterno ?? "").ToUpper(),
                    apellidoMaterno = (x.apellidoMaterno ?? "").ToUpper(),
                    celular = x.celular,
                    email = x.email,
                    activo = true
                })
                .ToList();
        }
    }
}
