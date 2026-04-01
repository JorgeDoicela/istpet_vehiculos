using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IEstudianteService
    {
        Task<Estudiante?> GetByCedulaAsync(string cedula);
        Task<IEnumerable<Estudiante>> GetAllAsync();
    }
}
