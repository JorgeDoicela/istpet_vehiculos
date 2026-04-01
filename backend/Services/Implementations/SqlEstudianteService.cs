using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class SqlEstudianteService : IEstudianteService
    {
        private readonly AppDbContext _context;

        public SqlEstudianteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Estudiante?> GetByCedulaAsync(string cedula)
        {
            return await _context.Estudiantes.FirstOrDefaultAsync(e => e.Cedula == cedula);
        }

        public async Task<IEnumerable<Estudiante>> GetAllAsync()
        {
            return await _context.Estudiantes.ToListAsync();
        }
    }
}
