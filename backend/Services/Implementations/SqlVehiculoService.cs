using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class SqlVehiculoService : IVehiculoService
    {
        private readonly AppDbContext _context;

        public SqlVehiculoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Vehiculo>> GetVehiculosAsync()
        {
            return await _context.Vehiculos.ToListAsync();
        }

        public async Task<Vehiculo?> GetVehiculoByPlacaAsync(string placa)
        {
            return await _context.Vehiculos.FirstOrDefaultAsync(v => v.Placa == placa);
        }
    }
}
