namespace backend.DTOs
{
    public class EstudianteDto
    {
        public string Cedula { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public bool Activo { get; set; }
    }

    public class VehiculoDto
    {
        public int Id { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string MarcaModelo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int Kilometraje { get; set; }
    }
}
