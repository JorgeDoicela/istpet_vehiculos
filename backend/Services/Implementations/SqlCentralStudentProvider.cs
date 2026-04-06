using backend.Data;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    /**
     * Real SQL Bridge implementation for the ISTPET Central Database.
     * Ready for when the DBA provides the final database name.
     */
    public class SqlCentralStudentProvider : ICentralStudentProvider
    {
        private readonly AppDbContext _context;
        // 🛠️ CONFIGURACIÓN: El día que tengas el nombre de la BD Central, cámbialo aquí.
        // Ejemplo: "academico_istpet" o "db_central"
        private const string CENTRAL_DB_NAME = "ISTPET_CENTRAL_DB";

        public SqlCentralStudentProvider(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CentralStudentDto?> GetFromCentralAsync(string cedula)
        {
            try 
            {
                /* 
                 * USANDO PUENTE SQL REAL:
                 * Realizamos un query cross-database (hacia otra BD en el mismo servidor).
                 * Si la BD central no existe aún, este catch capturará el error de forma segura.
                 */
                string sql = $@"
                    SELECT 
                        cedula AS Cedula, 
                        nombres AS Nombres, 
                        apellidos AS Apellidos
                    FROM {CENTRAL_DB_NAME}.estudiantes
                    WHERE cedula = @p0
                    LIMIT 1";

                // Nota: Esto requiere que el usuario de MySQL tenga permisos sobre ambas bases de datos.
                var result = await _context.Database.SqlQueryRaw<CentralStudentDto>(sql, cedula)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception)
            {
                // En modo "Sin Configurar", simplemente no encuentra nada en la central de forma segura.
                return null;
            }
        }
    }
}
