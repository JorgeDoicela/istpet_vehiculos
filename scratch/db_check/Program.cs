using System;
using MySqlConnector;
using System.Data;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string connStr = "Server=192.168.7.50;Port=3307;User=root;Password=rootpass57;";
        string[] sigafiTables = { "alumnos", "profesores", "vehiculos", "matriculas", "cond_alumnos_practicas", "usuarios_web" };
        string[] istpetTables = { "estudiantes", "instructores", "vehiculos", "usuarios", "v_clases_activas", "v_alerta_mantenimiento" };

        await DescribeTables("sigafi_es", sigafiTables, connStr);
        await DescribeTables("istpet_vehiculos", istpetTables, connStr);
    }

    static async Task DescribeTables(string db, string[] tables, string baseConn)
    {
        Console.WriteLine($"\n--- DATABASE: {db} ---");
        try
        {
            using var conn = new MySqlConnection(baseConn + $"Database={db};");
            await conn.OpenAsync();
            foreach (var table in tables)
            {
                try {
                    Console.WriteLine($"\n[{table}]");
                    using var cmd = new MySqlCommand($"DESCRIBE {table}", conn);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"  - {reader[0]} ({reader[1]})");
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"  - ERROR: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical Error {db}: {ex.Message}");
        }
    }
}
