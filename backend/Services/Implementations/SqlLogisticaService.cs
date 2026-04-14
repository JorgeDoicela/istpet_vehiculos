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


        public async Task<string> RegistrarSalidaAsync(int idMatricula, int idVehiculo, string idInstructor, string usuarioLogin, IEnumerable<int>? idsAsignacionHorario = null, string? observaciones = null)
        {
            idInstructor = (idInstructor ?? "").Trim();
            Console.WriteLine($"[Service] RegistrarSalidaAsync IN: Mat={idMatricula}, Veh={idVehiculo}, Ins='{idInstructor}'");
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validar que el vehículo exista y esté operativo (con JIT SYNC)
                var vehiculo = await _context.Vehiculos.FindAsync(idVehiculo);
                if (vehiculo == null || vehiculo.activo == 0)
                {
                    Console.WriteLine($"[Service] ERROR: Vehículo {idVehiculo} no encontrado o inactivo.");
                    return $"ERROR: Vehículo #{idVehiculo} no disponible u operativo en SIGAFI.";
                }

                var vehiculoOp = await _context.VehiculosOperaciones.FindAsync(idVehiculo);
                if (vehiculoOp != null && vehiculoOp.estado_mecanico != "OPERATIVO")
                {
                    Console.WriteLine($"[Service] ERROR: Vehículo {idVehiculo} no operativo mecánicamente ({vehiculoOp.estado_mecanico}).");
                    return $"ERROR: Vehículo #{idVehiculo} ({vehiculo.placa}) no operativo mecánicamente.";
                }

                // 2. Validar que el vehículo no esté ya en uso (ensalida = 1)
                var vehiculoOcupado = await _context.Practicas
                    .AnyAsync(p => p.idvehiculo == idVehiculo && p.ensalida == 1 && (p.cancelado ?? 0) == 0);

                if (vehiculoOcupado)
                {
                    Console.WriteLine($"[Service] ERROR: Vehículo {idVehiculo} ya está EN USO.");
                    return "VEHICULO_EN_USO";
                }

                // 3. Obtener datos de la matrícula para idAlumno
                var matricula = await _context.Matriculas.FindAsync(idMatricula);
                if (matricula == null) 
                {
                    Console.WriteLine($"[Service] ERROR: Matrícula {idMatricula} no encontrada.");
                    return $"ERROR: Matrícula #{idMatricula} no encontrada en SIGAFI.";
                }

                Console.WriteLine($"[Service] Matrícula OK: idAlumno={matricula.idAlumno}, idPeriodo={matricula.idPeriodo}");

                // 4. Validar que el estudiante no esté ya en pista
                var estudianteOcupado = await _context.Practicas
                    .AnyAsync(p => p.idalumno == matricula.idAlumno && p.ensalida == 1 && (p.cancelado ?? 0) == 0);

                if (estudianteOcupado)
                {
                    Console.WriteLine($"[Service] ERROR: Estudiante {matricula.idAlumno} ya está EN PISTA.");
                    return "ESTUDIANTE_EN_PISTA";
                }

                // 4.5 Asegurar que el instructor y el periodo existen (Solo validación)
                // Usamos TRIM en la consulta para manejar el padding de CHAR(14) en SIGAFI
                var instructorLocal = await _context.Instructores.AnyAsync(i => i.idProfesor.Trim() == idInstructor);
                
                if (!instructorLocal) 
                {
                    Console.WriteLine($"[Service] ERROR: Instructor '{idInstructor}' no localizado en tabla profesores (con Trim).");
                    return $"ERROR: Instructor {idInstructor} no encontrado en tabla profesores de SIGAFI.";
                }

                // Asegurar que el periodo existe para evitar violación de FK
                if (matricula.idPeriodo != null && matricula.idPeriodo != "S/P")
                {
                    var idPer = matricula.idPeriodo.Trim();
                    var periodoLocal = await _context.Periodos.AnyAsync(p => p.idPeriodo.Trim() == idPer);
                    if (!periodoLocal)
                    {
                        Console.WriteLine($"[Service] ADVERTENCIA: Periodo '{idPer}' no encontrado en tabla periodos.");
                    }
                }

                // 5. Registrar salida (Usando modelo Practica que mapea a cond_alumnos_practicas)
                var practica = new Practica
                {
                    idalumno = matricula.idAlumno,
                    idvehiculo = idVehiculo,
                    idProfesor = idInstructor,
                    idPeriodo = (matricula.idPeriodo ?? "S/P").Trim(),
                    fecha = DateTime.Today,
                    dia = DateTime.Today.ToString("dddd", new System.Globalization.CultureInfo("es-ES")).ToLower(),
                    hora_salida = DateTime.Now.TimeOfDay,
                    ensalida = 1,
                    user_asigna = usuarioLogin,
                    cancelado = 0,
                    observaciones = observaciones
                };

                Console.WriteLine($"[Service] Intentando guardar práctica para Alumno={practica.idalumno}, Veh={practica.idvehiculo}, Ins={practica.idProfesor}");

                _context.Practicas.Add(practica);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[Service] Práctica guardada con ID={practica.idPractica}");

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

        public async Task<string> RegistrarLlegadaAsync(int idPractica, string usuarioLogin)
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
                practica.user_llegada = usuarioLogin;

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
    
        public async Task<string> EliminarSalidaAsync(int idPractica, string usuarioLogin)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Obtener registro de la práctica
                var practica = await _context.Practicas.FindAsync(idPractica);
                if (practica == null)
                    return "ERROR: Registro de práctica no encontrado.";

                // 2. Liberar agenda (si estaba vinculada)
                var vinculos = await _context.PracticasHorarios
                    .Where(vh => vh.idPractica == idPractica)
                    .ToListAsync();

                foreach (var v in vinculos)
                {
                    var horario = await _context.HorariosAlumnos.FindAsync(v.idAsignacionHorario);
                    if (horario != null)
                    {
                        horario.asiste = 0; // Restaurar a pendiente
                    }
                }

                // 3. Eliminar vínculos y la práctica (Cascada manual por precaución)
                if (vinculos.Any())
                {
                    _context.PracticasHorarios.RemoveRange(vinculos);
                }

                _context.Practicas.Remove(practica);
                
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
