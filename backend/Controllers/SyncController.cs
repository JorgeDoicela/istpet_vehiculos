using backend.DTOs;
using backend.Models;
using backend.Services.Implementations;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class SyncController : ControllerBase
    {
        private readonly IDataSyncService _syncService;
        private readonly SigafiExtractionProbe _sigafiProbe;

        public SyncController(IDataSyncService syncService, SigafiExtractionProbe sigafiProbe)
        {
            _syncService = syncService;
            _sigafiProbe = sigafiProbe;
        }

        [HttpPost("students")]
        public async Task<ActionResult<ApiResponse<SyncLog>>> SyncStudents([FromBody] List<JsonElement> externalData)
        {
            try
            {
                // Si no mandan datos, simulamos una ingesta externa con errores para probar el Escudo
                if (externalData == null || externalData.Count == 0)
                {
                    externalData = new List<JsonElement>
                    {
                        // Dato CORRECTO
                        JsonSerializer.Deserialize<JsonElement>(@"{""id_externo"":""1755555555"",""nombre_completo"":""Ana Rodriguez"",""correo_universidad"":""ana@univ.edu""}"),
                        // Dato con CÉDULA ERRÓNEA (El Escudo debe rechazarlo)
                        JsonSerializer.Deserialize<JsonElement>(@"{""id_externo"":""999"",""nombre_completo"":""Error Humano"",""correo_universidad"":""bad@data.com""}"),
                        // Dato con NOMBRE MALFORMADO (El Escudo debe limpiarlo)
                        JsonSerializer.Deserialize<JsonElement>(@"{""id_externo"":""1766666666"",""nombre_completo"":""Carlos 123 @ Sanchez"",""correo_universidad"":""carlos@univ.edu""}")
                    };
                }

                var syncLog = await _syncService.SyncExternalStudentsAsync(externalData);
                return Ok(ApiResponse<SyncLog>.Ok(syncLog, "Proceso de sincronización finalizado con éxito."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SyncLog>.Fail($"Fallo de integración crítica: {ex.Message}"));
            }
        }

        [HttpPost("instructors")]
        public async Task<ActionResult<ApiResponse<SyncLog>>> SyncInstructors()
        {
            try
            {
                var syncLog = await _syncService.SyncInstructorsAsync();
                return Ok(ApiResponse<SyncLog>.Ok(syncLog, "Sincronización de instructores (SIGAFI) finalizada."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SyncLog>.Fail($"Error en sincronización manual: {ex.Message}"));
            }
        }

        [HttpPost("master")]
        public async Task<ActionResult<ApiResponse<SyncLog>>> MasterSync()
        {
            try
            {
                var syncLog = await _syncService.MasterSyncAsync();
                return Ok(ApiResponse<SyncLog>.Ok(syncLog, "Master Sync SIGAFI ejecutado correctamente."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SyncLog>.Fail($"Error en Master Sync: {ex.Message}"));
            }
        }

        [HttpGet("ping-sigafi")]
        public async Task<ActionResult<ApiResponse<object>>> PingSigafi()
        {
            var ok = await _syncService.PingSigafiAsync();
            if (!ok)
            {
                return StatusCode(503, ApiResponse<object>.Fail("No hay conexión con el servidor SIGAFI remoto. Verifique la conectividad de red."));
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                connected = true,
                source = "SIGAFI",
                checkedAtUtc = DateTime.UtcNow
            }, "Conexión SIGAFI OK."));
        }

        /// <summary>
        /// Comprueba que cada SELECT contra SIGAFI (los mismos que usa MasterSync) ejecute sin error y devuelve conteos.
        /// </summary>
        [HttpGet("sigafi-probe")]
        public async Task<ActionResult<ApiResponse<SigafiProbeResponse>>> SigafiProbe(CancellationToken cancellationToken)
        {
            var report = await _sigafiProbe.RunAsync(cancellationToken);
            var allOk = report.Connected && report.Modules.All(m => m.Ok);
            var msg = allOk
                ? "Todas las extracciones SIGAFI respondieron correctamente."
                : "Hay módulos con error; revisa RowCount/Error por tabla.";

            var payload = new ApiResponse<SigafiProbeResponse>
            {
                Success = allOk,
                Message = msg,
                Data = report
            };

            if (!report.Connected)
                return StatusCode(503, payload);

            return Ok(payload);
        }
        /// <summary>
        /// Auditoría de Paridad: Compara el conteo de registros entre SIGAFI y la base de datos local.
        /// Este endpoint es público para facilitar la verificación rápida de la sincronización.
        /// </summary>
        [HttpGet("audit")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<backend.DTOs.DataParityAuditDto>>> GetAuditParity()
        {
            try
            {
                var audit = await _syncService.GetDataParityAuditAsync();
                return Ok(ApiResponse<backend.DTOs.DataParityAuditDto>.Ok(audit, "Auditoría de paridad completada."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<backend.DTOs.DataParityAuditDto>.Fail($"Error en auditoría: {ex.Message}"));
            }
        }

        /// <summary>
        /// Inserta una cédula para comparar los datos de un estudiante entre SIGAFI y la base local.
        /// </summary>
        [HttpGet("inspect/student/{idAlumno}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<backend.DTOs.ParityInspectionResult<backend.Services.Interfaces.CentralStudentDto, backend.Models.Estudiante>>>> InspectStudent(string idAlumno)
        {
            var res = await _syncService.InspectStudentParityAsync(idAlumno);
            return Ok(ApiResponse<backend.DTOs.ParityInspectionResult<backend.Services.Interfaces.CentralStudentDto, backend.Models.Estudiante>>.Ok(res));
        }

        /// <summary>
        /// Inserta un ID de profesor para comparar los datos entre SIGAFI y la base local.
        /// </summary>
        [HttpGet("inspect/instructor/{idProfesor}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<backend.DTOs.ParityInspectionResult<backend.Services.Interfaces.CentralInstructorDto, backend.Models.Instructor>>>> InspectInstructor(string idProfesor)
        {
            var res = await _syncService.InspectInstructorParityAsync(idProfesor);
            return Ok(ApiResponse<backend.DTOs.ParityInspectionResult<backend.Services.Interfaces.CentralInstructorDto, backend.Models.Instructor>>.Ok(res));
        }

        /// <summary>
        /// Inserta una placa para comparar los datos de un vehículo entre SIGAFI y la base local.
        /// </summary>
        [HttpGet("inspect/vehicle/{placa}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<backend.DTOs.ParityInspectionResult<backend.Services.Interfaces.CentralVehiculoDto, backend.Models.Vehiculo>>>> InspectVehicle(string placa)
        {
            var res = await _syncService.InspectVehicleParityAsync(placa);
            return Ok(ApiResponse<backend.DTOs.ParityInspectionResult<backend.Services.Interfaces.CentralVehiculoDto, backend.Models.Vehiculo>>.Ok(res));
        }

        [HttpGet("db-diag")]
        [AllowAnonymous]
        public async Task<IActionResult> DbDiag()
        {
            try
            {
                var conn = _syncService.GetDebugConnectionInfo();
                return Ok(conn);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
