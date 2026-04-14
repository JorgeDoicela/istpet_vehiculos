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


        public async Task<string> RegistrarSalidaAsync(int idMatricula, int idVehiculo, string idInstructor, int registradoPor, IEnumerable<int>? idsAsignacionHorario = null, string? observaciones = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ... (existing logging and matricula fetch logic assumed to be above)
                // Need to see where the actual code starts to replace correctly.
                // 1. Validar que el vehículo exista y esté operativo (con JIT SYNC)
                var vehiculo = await _context.Vehiculos.FindAsync(idVehiculo);
                if (vehiculo == null)
                {
                    // [JIT RESILIENCE] Si el vehículo no está en el espejo local, lo traemos de SIGAFI al instante.
                    var centralVehicles = await _central.GetAllVehiclesFromCentralAsync();
                    await Helpers.SigafiVehicleUpsert.MergeFromCentralAsync(_context, centralVehicles);
                    vehiculo = await _context.Vehiculos.FindAsync(idVehiculo);
                }

                if (vehiculo == null || vehiculo.activo == 0)
                    return "ERROR: Vehículo no disponible u operativo (no hallado localmente).";

                var vehiculoOp = await _context.VehiculosOperaciones.FindAsync(idVehiculo);
                if (vehiculoOp != null && vehiculoOp.estado_mecanico != "OPERATIVO")
                    return "ERROR: Vehículo no operativo mecánicamente.";

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

                // 4.5 Asegurar que el instructor y el periodo existen localmente (JIT SYNC)
                var instructorLocal = await _context.Instructores.AnyAsync(i => i.idProfesor == idInstructor);
                if (!instructorLocal)
                {
                    var cp = await _central.GetInstructorFromCentralAsync(idInstructor);
                    if (cp != null)
                    {
                        var ni = new Instructor
                        {
                            idProfesor = cp.idProfesor,
                            primerNombre = (cp.primerNombre ?? cp.nombres ?? "S/N").ToUpper(),
                            primerApellido = (cp.primerApellido ?? cp.apellidos ?? "S/N").ToUpper(),
                            nombres = (cp.nombres ?? "").ToUpper(),
                            apellidos = (cp.apellidos ?? "").ToUpper(),
                            activo = 1
                        };
                        _context.Instructores.Add(ni);
                        await _context.SaveChangesAsync();
                    }
                }

                // Asegurar que el periodo existe para evitar violación de FK
                if (matricula.idPeriodo != null && matricula.idPeriodo != "S/P")
                {
                    var periodoLocal = await _context.Periodos.AnyAsync(p => p.idPeriodo == matricula.idPeriodo);
                    if (!periodoLocal)
                    {
                        var periods = await _central.GetAllPeriodosFromCentralAsync();
                        foreach (var p in periods)
                        {
                            if (!await _context.Periodos.AnyAsync(px => px.idPeriodo == p.idPeriodo))
                            {
                                _context.Periodos.Add(new Periodo { 
                                    idPeriodo = p.idPeriodo, 
                                    detalle = p.detalle, 
                                    activo = p.activo == 1 
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
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
                    dia = DateTime.Today.ToString("dddd", new System.Globalization.CultureInfo("es-ES")).ToLower(),
                    hora_salida = DateTime.Now.TimeOfDay,
                    ensalida = 1,
                    user_asigna = registradoPor.ToString(),
                    cancelado = 0,
                    observaciones = observaciones // 🚀 Guardamos observación en la práctica
                };

                _context.Practicas.Add(practica);
                await _context.SaveChangesAsync();

                // 🚀 Vínculo con Agenda SIGAFI (Cierre de Ciclo) - Soporte para múltiples horas
                if (idsAsignacionHorario != null && idsAsignacionHorario.Any())
                {
                    foreach (var idH in idsAsignacionHorario)
                    {
                        // 1. Vincular práctica con el slot de agenda
                        var vinculacion = new PracticaHorarioAlumno
                        {
                            idPractica = practica.idPractica,
                            idAsignacionHorario = idH
                        };
                        _context.PracticasHorarios.Add(vinculacion);

                        // 2. Marcar Asistencia y Registrar Observación en la agenda central
                        var horario = await _context.HorariosAlumnos.FindAsync(idH);
                        if (horario != null)
                        {
                            horario.asiste = 1;
                            // Sincronizamos la observación si existe para trazabilidad total
                            if (!string.IsNullOrEmpty(observaciones))
                            {
                                horario.observacion = observaciones.Length > 100 
                                    ? observaciones.Substring(0, 97) + "..." 
                                    : observaciones;
                            }
                        }
                    }
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

        public async Task<string> RegistrarLlegadaAsync(int idPractica, int registradoPor)
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
