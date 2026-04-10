using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;


namespace backend.Services.Implementations
{
    /**
     * EF Core Implementation of ILogisticaService
     * Refactored 2026 for Absolute Parity: idAlumno, idProfesor, idVehiculo, idPractica.
     */
    public class SqlLogisticaService : ILogisticaService
    {
        private readonly AppDbContext _context;
        private readonly ICentralStudentProvider _central;

        public SqlLogisticaService(AppDbContext context, ICentralStudentProvider central)
        {
            _context = context;
            _central = central;
        }


        public async Task<string> RegistrarSalidaAsync(int idMatricula, int idVehiculo, string idInstructor, string observaciones, int registradoPor, int? idAsignacionHorario = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validar que el vehículo exista y esté operativo
                var vehiculo = await _context.Vehiculos.FindAsync(idVehiculo);
                if (vehiculo == null || !vehiculo.activo || vehiculo.estado_mecanico != "OPERATIVO")
                    return "ERROR: Vehículo no disponible u operativo.";

                // 2. Validar que el vehículo no esté ya en uso (ensalida = 1)
                var vehiculoOcupado = await _context.Practicas
                    .AnyAsync(p => p.idvehiculo == idVehiculo && p.ensalida == 1 && p.cancelado == 0);

                if (vehiculoOcupado)
                    return "VEHICULO_EN_USO";

                // 3. Obtener datos de la matrícula para idAlumno
                var matricula = await _context.Matriculas.FindAsync(idMatricula);
                if (matricula == null) return "ERROR: Matrícula no encontrada.";

                // 4. Validar que el estudiante no esté ya en pista
                var estudianteOcupado = await _context.Practicas
                    .AnyAsync(p => p.idalumno == matricula.idAlumno && p.ensalida == 1 && p.cancelado == 0);

                if (estudianteOcupado)
                    return "ESTUDIANTE_EN_PISTA";

                // 4.5 Asegurar que el instructor existe localmente para que el JOIN de la vista v_clases_activas no esconda el registro.
                var instructorLocal = await _context.Instructores.AnyAsync(i => i.idProfesor == idInstructor);
                if (!instructorLocal)
                {
                    try
                    {
                        var cp = await _central.GetInstructorFromCentralAsync(idInstructor);
                        if (cp != null)
                        {
                            _context.Instructores.Add(new Instructor
                            {
                                idProfesor = cp.idProfesor,
                                primerNombre = (cp.primerNombre ?? cp.nombres).ToUpper(),
                                primerApellido = (cp.primerApellido ?? cp.apellidos).ToUpper(),
                                nombres = (cp.nombres ?? "").ToUpper(),
                                apellidos = (cp.apellidos ?? "").ToUpper(),
                                activo = true
                            });
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch
                    {
                        // Si falla la central, se permite continuar para no bloquear la operación de salida,
                        // aunque no aparezca en la vista v_clases_activas (que usa INNER JOIN o LEFT JOIN sin datos).
                    }
                }

                // 5. Registrar salida (Usando modelo Practica que mapea a cond_alumnos_practicas)

                var practica = new Practica
                {
                    idalumno = matricula.idAlumno,
                    idvehiculo = idVehiculo,
                    idProfesor = idInstructor,
                    idPeriodo = matricula.idPeriodo ?? "S/P",
                    fecha = DateTime.Today,
                    hora_salida = DateTime.Now.TimeOfDay,
                    ensalida = 1,
                    user_asigna = registradoPor.ToString(),
                    cancelado = 0,
                    observaciones = observaciones
                };

                _context.Practicas.Add(practica);
                await _context.SaveChangesAsync();

                // 🚀 Vínculo con Agenda SIGAFI (Cierre de Ciclo)
                if (idAsignacionHorario.HasValue && idAsignacionHorario.Value > 0)
                {
                    var vinculacion = new PracticaHorarioAlumno
                    {
                        idPractica = practica.idPractica,
                        idAsignacionHorario = idAsignacionHorario.Value
                    };
                    _context.PracticasHorarios.Add(vinculacion);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return "EXITO";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"ERROR: {ex.Message}";
            }
        }

        public async Task<string> RegistrarLlegadaAsync(int idPractica, string observaciones, int registradoPor)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Obtener registro de la práctica
                var practica = await _context.Practicas.FindAsync(idPractica);
                if (practica == null)
                    return "ERROR: Registro de práctica no encontrado.";

                // 2. Validar que esté en salida
                if (practica.ensalida == 0)
                    return "ERROR: Esta práctica ya fue cerrada o no ha iniciado.";

                // 3. Registrar Llegada
                practica.hora_llegada = DateTime.Now.TimeOfDay;
                practica.ensalida = 0;
                practica.user_llegada = registradoPor.ToString();

                if (practica.hora_salida.HasValue)
                {
                    practica.tiempo = practica.hora_llegada - practica.hora_salida;
                }

                // 4. Actualizar Horas Completadas en Matrícula
                var matricula = await _context.Matriculas.FirstOrDefaultAsync(m => m.idAlumno == practica.idalumno && m.estado == "ACTIVO");
                if (matricula != null && practica.tiempo.HasValue)
                {
                    decimal horasCalculadas = (decimal)Math.Round(practica.tiempo.Value.TotalHours, 2);
                    matricula.horas_completadas += horasCalculadas;
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
