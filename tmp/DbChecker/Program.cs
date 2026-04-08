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
            Console.WriteLine("--- TABLE: usuarios ---");
            DescribeTable(conn, "usuarios");
            
            Console.WriteLine("\n--- TABLE: registros_salida ---");
            DescribeTable(conn, "registros_salida");
            
            Console.WriteLine("\n--- TABLE: registros_llegada ---");
            DescribeTable(conn, "registros_llegada");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
        }
    }

    static void DescribeTable(MySqlConnection conn, string tableName)
    {
        using var cmd = new MySqlCommand($"DESCRIBE {tableName}", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine($"{reader["Field"]} - {reader["Type"]}");
        }
    }
}
