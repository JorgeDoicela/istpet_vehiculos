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
            
            // 1. ESTUDIANTE DE PRUEBA REAL (DE LA FOTO)
            _mockData.Add(new ExternalStudentDto
            {
                Cedula = "1725555377",
                Nombres = "JORGE ISMAEL",
                Apellidos = "DOICELA MOLINA",
                IdTipoLicencia = 1, // Tipo C
                CursoSugerido = "DESARROLLO DE SOFTWARE CUARTO",
                Periodo = "OCT2025",
                Paralelo = "A",
                Jornada = "MATUTINA"
            });

            // 2. OTROS ESTUDIANTES REALISTAS
            _mockData.Add(new ExternalStudentDto
            {
                Cedula = "1700000001",
                Nombres = "MARCO ANTONIO",
                Apellidos = "PEREZ SALAZAR",
                IdTipoLicencia = 2, // Tipo D
                CursoSugerido = "CONDUCCION PROFESIONAL TIPO D",
                Periodo = "ABR2026",
                Paralelo = "B",
                Jornada = "VESPERTINA"
            });

            // Resto de la data mock generada para pruebas masivas
            string[] names = { "Juan", "Maria", "Carlos", "Luis", "Ana", "Jose", "Beatriz", "Pedro", "Elena", "Fernando" };
            string[] surnames = { "Perez", "Garcia", "Ruiz", "Vaca", "Moncayo", "Loor", "Doicela", "Molina", "Vera", "Mora" };
            
            for (int i = 1; i <= 10; i++)
            {
                _mockData.Add(new ExternalStudentDto
                {
                    Cedula = $"17255550{i:D2}",
                    Nombres = names[i % names.Length].ToUpper(),
                    Apellidos = $"{surnames[i % surnames.Length]} {surnames[(i+1) % surnames.Length]}".ToUpper(),
                    IdTipoLicencia = (i % 3) + 1,
                    CursoSugerido = $"MANEJO PROFESIONAL TIPO { (i % 3 == 0 ? "E" : (i % 2 == 0 ? "D" : "C")) }",
                    Periodo = "MAY2026",
                    Paralelo = "C",
                    Jornada = "NOCTURNA"
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
