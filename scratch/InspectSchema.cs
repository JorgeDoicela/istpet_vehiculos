
using System;
using System.Data;
using MySqlConnector;

namespace SchemaInspector
{
    class Program
    {
        static void Main(string[] args)
        {
            string connString = "Server=192.168.7.50;Port=3307;Database=istpet_vehiculos;User=root;Password=rootpass57;";
            using var conn = new MySqlConnection(connString);
            conn.Open();
            
            string[] tables = { "profesores", "alumnos", "usuarios_web" };
            foreach (var table in tables)
            {
                Console.WriteLine($"\n--- Columns in {table} ---");
                using var cmd = new MySqlCommand($"DESCRIBE {table};", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"{reader[0]} | {reader[1]} | {reader[2]} | {reader[4]}");
                }
            }
        }
    }
}
