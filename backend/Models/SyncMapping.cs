namespace backend.Models
{
    /**
     * Adaptador Dinámico de Datos
     * Permite mapear "Campo de fuera" -> "Columna ISTPET" sin tocar la base de datos.
     */
    public class SyncMapping
    {
        public string SourceField { get; set; } = string.Empty; // Ej: "mail_estudiante"
        public string DestinationField { get; set; } = string.Empty; // Ej: "Email"
        public string EntityType { get; set; } = string.Empty; // Ej: "Estudiante"
        public bool IsRequired { get; set; } = true;
    }
}
