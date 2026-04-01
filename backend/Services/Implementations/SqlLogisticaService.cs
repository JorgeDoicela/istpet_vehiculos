using backend.Data;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;

namespace backend.Services.Implementations
{
    /**
     * Stored Procedure Implementation of ILogisticaService
     * Executes SQL Procedures: sp_registrar_salida and sp_registrar_llegada
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
            var pIdMat = new MySqlParameter("p_id_matricula", idMatricula);
            var pIdVeh = new MySqlParameter("p_id_vehiculo", idVehiculo);
            var pIdIns = new MySqlParameter("p_id_instructor", idInstructor);
            var pObs = new MySqlParameter("p_obs", observaciones ?? (object)DBNull.Value);
            var pUser = new MySqlParameter("p_registrado_por", registradoPor);
            var pRes = new MySqlParameter("p_resultado", MySqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_registrar_salida(@p_id_matricula, @p_id_vehiculo, @p_id_instructor, @p_obs, @p_registrado_por, @p_resultado)",
                pIdMat, pIdVeh, pIdIns, pObs, pUser, pRes);

            return pRes.Value?.ToString() ?? "ERROR_DESCONOCIDO";
        }

        public async Task<string> RegistrarLlegadaAsync(int idRegistro, int kmLlegada, string observaciones, int registradoPor)
        {
            var pIdReg = new MySqlParameter("p_id_registro", idRegistro);
            var pKm = new MySqlParameter("p_km_llegada", kmLlegada);
            var pObs = new MySqlParameter("p_obs", observaciones ?? (object)DBNull.Value);
            var pUser = new MySqlParameter("p_registrado_por", registradoPor);
            var pRes = new MySqlParameter("p_resultado", MySqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_registrar_llegada(@p_id_registro, @p_km_llegada, @p_obs, @p_registrado_por, @p_resultado)",
                pIdReg, pKm, pObs, pUser, pRes);

            return pRes.Value?.ToString() ?? "ERROR_DESCONOCIDO";
        }
    }
}
