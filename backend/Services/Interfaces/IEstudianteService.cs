using backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /**
     * Student Search Service: Refactored for Absolute SIGAFI Parity 2026.
     */
    public interface IEstudianteService
    {
        Task<Estudiante?> GetByIdAlumnoAsync(string idAlumno);
        Task<IEnumerable<Estudiante>> GetAllAsync();
    }
}
