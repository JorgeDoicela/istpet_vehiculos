using MySqlConnector;
using System;

class Program
{
    static void Main()
    {
        string connString = "Server=192.168.7.50;Port=3307;Database=istpet_vehiculos;User=root;Password=rootpass57;";
        try
        {
            using var conn = new MySqlConnection(connString);
            conn.Open();
            Console.WriteLine("--- TABLA: usuarios ---");
            using (var cmd = new MySqlCommand("DESCRIBE usuarios", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"{reader["Field"]} - {reader["Type"]} - {reader["Null"]} - {reader["Key"]}");
                }
            }

            Console.WriteLine("\n--- TABLA: registros_salida ---");
            using (var cmd = new MySqlCommand("DESCRIBE registros_salida", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"{reader["Field"]} - {reader["Type"]} - {reader["Null"]} - {reader["Key"]}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
        }
    }
}
