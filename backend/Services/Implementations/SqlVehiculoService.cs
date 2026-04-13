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
            
            // Unimos con la tabla de operación para traer el estado mecánico e instructor fijo
            return await _context.Vehiculos
                .Include(v => v.Operacion)
                .ToListAsync();
        }

        public async Task<Vehiculo?> GetVehiculoByPlacaAsync(string placa)
        {
            var key = placa.Trim();

            try
            {
                var cv = await _central.GetVehicleByPlacaFromCentralAsync(key);
                if (cv != null)
                {
                    await SigafiVehicleUpsert.MergeFromCentralAsync(_context, new[] { cv });
                }
            }
            catch (Exception) { /* Fallback a espejo local */ }

            return await _context.Vehiculos
                .Include(v => v.Operacion)
                .FirstOrDefaultAsync(v => v.placa == key);
        }

        public async Task<bool> UpdateOperacionAsync(VehiculoOperacion op)
        {
            var existing = await _context.VehiculosOperacion.FindAsync(op.idVehiculo);
            if (existing == null)
            {
                _context.VehiculosOperacion.Add(op);
            }
            else
            {
                existing.estado_mecanico = op.estado_mecanico;
                existing.id_instructor_fijo = op.id_instructor_fijo;
                existing.id_tipo_licencia = op.id_tipo_licencia;
            }

            return await _context.SaveChangesAsync() > 0;
        }
    }
}
