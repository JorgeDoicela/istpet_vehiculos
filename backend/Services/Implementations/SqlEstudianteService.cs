using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    /**
     * Student Search Service: Refactored for Absolute SIGAFI Parity 2026.
     */
    public class SqlEstudianteService : IEstudianteService
    {
        private readonly AppDbContext _context;

        public SqlEstudianteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Estudiante?> GetByIdAlumnoAsync(string idAlumno)
        {
            return await _context.Estudiantes.FirstOrDefaultAsync(e => e.idAlumno == idAlumno);
        }

        public async Task<IEnumerable<Estudiante>> GetAllAsync()
        {
            return await _context.Estudiantes.ToListAsync();
        }
    }
}
