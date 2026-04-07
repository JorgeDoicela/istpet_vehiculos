using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    /**
     * EF Core Implementation of ILogisticaService
     * Migrado desde Procedimientos Almacenados (Stored Procedures)
     * Toda la lógica de negocio y validación reside ahora en el servidor.
     */
    public class SqlLogisticaService : ILogisticaService
    {
        private readonly AppDbContext _context;

        public SqlLogisticaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> RegistrarSalidaAsync(int idMatricula, int idVehiculo, int idInstructor, string observaciones, int registradoPor)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validar que el vehículo exista y esté operativo
                var vehiculo = await _context.Vehiculos.FindAsync(idVehiculo);
                if (vehiculo == null || !vehiculo.Activo || vehiculo.EstadoMecanico != "OPERATIVO")
                    return "ERROR: Vehículo no disponible u operativo.";

                // 2. Validar que el vehículo no esté ya en uso (sin llegada registrada)
                var vehiculoOcupado = await _context.RegistrosSalida
                    .AnyAsync(s => s.IdVehiculo == idVehiculo && !_context.RegistrosLlegada.Any(l => l.IdRegistro == s.Id_Registro));

                if (vehiculoOcupado)
                    return "VEHICULO_EN_USO";

                // 3. Validar que el instructor no esté ocupado (sin llegada registrada)
                var instructorOcupado = await _context.RegistrosSalida
                    .AnyAsync(s => s.IdInstructor == idInstructor && !_context.RegistrosLlegada.Any(l => l.IdRegistro == s.Id_Registro));

                if (instructorOcupado)
                    return "INSTRUCTOR_OCUPADO";

                // 4. Validar que el estudiante no esté ya en pista
                // Buscamos si hay un registro de salida para esta matrícula sin su respectiva llegada
                var estudianteOcupado = await _context.RegistrosSalida
                    .Where(s => s.IdMatricula == idMatricula && !_context.RegistrosLlegada.Any(l => l.IdRegistro == s.Id_Registro))
                    .AnyAsync();

                if (estudianteOcupado)
                    return "ESTUDIANTE_EN_PISTA";

                // 5. Registrar salida
                var salida = new RegistroSalida
                {
                    IdMatricula = idMatricula,
                    IdVehiculo = idVehiculo,
                    IdInstructor = idInstructor,
                    FechaHoraSalida = DateTime.Now,
                    ObservacionesSalida = observaciones,
                    RegistradoPor = registradoPor
                };

                _context.RegistrosSalida.Add(salida);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return "EXITO";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"ERROR: {ex.Message}";
            }
        }

        public async Task<string> RegistrarLlegadaAsync(int idRegistro, string observaciones, int registradoPor)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Obtener registro de salida
                var salida = await _context.RegistrosSalida.FindAsync(idRegistro);
                if (salida == null)
                    return "ERROR: Registro de salida no encontrado.";

                // 2. Validar que no tenga llegada ya
                var tieneLlegada = await _context.RegistrosLlegada.AnyAsync(l => l.IdRegistro == idRegistro);
                if (tieneLlegada)
                    return "ERROR: Este registro ya fue cerrado.";

                // 3. Registrar Llegada
                var llegada = new RegistroLlegada
                {
                    IdRegistro = idRegistro,
                    FechaHoraLlegada = DateTime.Now,
                    ObservacionesLlegada = observaciones,
                    RegistradoPor = registradoPor
                };

                _context.RegistrosLlegada.Add(llegada);

                // 6. Actualizar Horas Completadas del Estudiante (Matrícula)
                var matricula = await _context.Matriculas.FindAsync(salida.IdMatricula);
                if (matricula != null)
                {
                    var duracionClase = llegada.FechaHoraLlegada - salida.FechaHoraSalida;
                    // Redondear a dos decimales las horas totales (eg. 1.5 horas = 1 hora 30 min)
                    decimal horasCalculadas = (decimal)Math.Round(duracionClase.TotalHours, 2);
                    matricula.HorasCompletadas += horasCalculadas;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return "EXITO";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"ERROR: {ex.Message}";
            }
        }
    }
}
