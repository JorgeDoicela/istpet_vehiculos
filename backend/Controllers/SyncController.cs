using backend.DTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly IDataSyncService _syncService;

        public SyncController(IDataSyncService syncService)
        {
            _syncService = syncService;
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
    }
}
