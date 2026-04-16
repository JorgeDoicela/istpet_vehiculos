using MySqlConnector;
using System;
using System.Data;

var connectionString = "Server=192.168.7.50;Port=3307;Database=sigafi_es;Uid=root;Pwd=rootpass57;Charset=utf8;AllowZeroDateTime=True;";

try {
    using var conn = new MySqlConnection(connectionString);
    conn.Open();
    Console.WriteLine("Conexión exitosa.");

    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT idPractica, idalumno, idvehiculo, ensalida, cancelado, hora_salida, hora_llegada FROM cond_alumnos_practicas WHERE fecha >= CURDATE() ORDER BY idPractica DESC LIMIT 10";
    
    using var reader = cmd.ExecuteReader();
    Console.WriteLine("idPractica | idalumno | idvehiculo | ensalida | cancelado | hora_salida | hora_llegada");
    while (reader.Read()) {
        Console.WriteLine($"{reader[0]} | {reader[1]} | {reader[2]} | {reader[3]} | {reader[4]} | {reader[5]} | {reader[6]}");
    }
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
