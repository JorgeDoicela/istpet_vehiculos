using backend.Data;
using backend.Models;
using backend.Services.Helpers;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class SqlVehiculoService : IVehiculoService
    {
        private readonly AppDbContext _context;
        private readonly ICentralStudentProvider _central;

        public SqlVehiculoService(AppDbContext context, ICentralStudentProvider central)
        {
            _context = context;
            _central = central;
        }

        public async Task<IEnumerable<Vehiculo>> GetVehiculosAsync()
        {
            var central = await _central.GetAllVehiclesFromCentralAsync();
            await SigafiVehicleUpsert.MergeFromCentralAsync(_context, central);
            return await _context.Vehiculos
                .Include(v => v.TipoLicencia)
                .Include(v => v.InstructorFijo)
                .ToListAsync();
        }

        public async Task<Vehiculo?> GetVehiculoByPlacaAsync(string placa)
        {
            var key = placa.Trim();
            var local = await _context.Vehiculos
                .Include(v => v.TipoLicencia)
                .Include(v => v.InstructorFijo)
                .FirstOrDefaultAsync(v => v.placa == key);
            if (local != null)
                return local;

            var cv = await _central.GetVehicleByPlacaFromCentralAsync(key);
            if (cv == null)
                return null;

            await SigafiVehicleUpsert.MergeFromCentralAsync(_context, new[] { cv });
            return await _context.Vehiculos
                .Include(v => v.TipoLicencia)
                .Include(v => v.InstructorFijo)
                .FirstOrDefaultAsync(v => v.placa == key);
        }
    }
}
