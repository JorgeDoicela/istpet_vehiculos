namespace backend.DTOs
{
    /// <summary>
    /// Resumen de paridad de datos entre SIGAFI (Remoto) y la base de datos local istpet_vehiculos.
    /// </summary>
    public sealed class DataParityAuditDto
    {
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsSigafiConnected { get; set; }
        public List<TableParityInfo> Tables { get; set; } = new();
    }

    /// <summary>
    /// Información de paridad para una tabla específica.
    /// </summary>
    public sealed class TableParityInfo
    {
        public string TableName { get; set; } = string.Empty;
        public int RemoteCount { get; set; }
        public int LocalCount { get; set; }
        public int Difference => RemoteCount - LocalCount;
        
        /// <summary>
        /// Determina el estado de la sincronización.
        /// </summary>
        public string Status => Difference == 0 
            ? "SINCRONIZADO" 
            : (Difference > 0 ? "PENDIENTE_SYNC" : "EXTRA_LOCAL");

        /// <summary>
        /// Porcentaje de paridad (0 a 100).
        /// </summary>
        public double ParityPercentage => RemoteCount == 0 ? 100 : Math.Min(100, (double)LocalCount / RemoteCount * 100);
    }
}
