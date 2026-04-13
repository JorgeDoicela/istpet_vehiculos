using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IVehiculoService
    {
        Task<IEnumerable<Vehiculo>> GetVehiculosAsync();
        Task<Vehiculo?> GetVehiculoByPlacaAsync(string placa);
        Task<bool> UpdateOperacionAsync(VehiculoOperacion op);
    }
}
