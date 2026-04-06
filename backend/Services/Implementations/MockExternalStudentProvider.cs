using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    /**
     * Simulation of an External Driving School Management System DB/API.
     * Contains 20 students for testing purposes.
     */
    public class MockExternalStudentProvider : IExternalStudentProvider
    {
        private readonly List<ExternalStudentDto> _mockData;

        public MockExternalStudentProvider()
        {
            _mockData = new List<ExternalStudentDto>();
            
            // Generate 20 mock students (Cedulas: 1725555001-1725555020)
            string[] names = { "Juan", "Maria", "Carlos", "Luis", "Ana", "Jose", "Beatriz", "Pedro", "Elena", "Fernando" };
            string[] surnames = { "Perez", "Garcia", "Ruiz", "Vaca", "Moncayo", "Loor", "Doicela", "Molina", "Vera", "Mora" };
            
            for (int i = 1; i <= 20; i++)
            {
                _mockData.Add(new ExternalStudentDto
                {
                    Cedula = $"17255550{i:D2}",
                    Nombres = names[i % names.Length],
                    Apellidos = $"{surnames[i % surnames.Length]} {surnames[(i+1) % surnames.Length]}",
                    IdTipoLicencia = (i % 3) + 1, // License Type C(1), D(2), E(3)
                    CursoSugerido = $"Curso { (i % 3 == 0 ? "Tipo E" : (i % 2 == 0 ? "Tipo D" : "Profesional C")) }"
                });
            }
        }

        public async Task<ExternalStudentDto?> GetByCedulaAsync(string cedula)
        {
            // Simulate network delay (500ms)
            await Task.Delay(500);
            return _mockData.FirstOrDefault(s => s.Cedula == cedula);
        }
    }
}
