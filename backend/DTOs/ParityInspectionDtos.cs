namespace backend.DTOs
{
    /// <summary>
    /// Resultado de la inspección detallada de paridad para una entidad.
    /// </summary>
    /// <typeparam name="TRemote">Tipo del DTO retornado por SIGAFI.</typeparam>
    /// <typeparam name="TLocal">Tipo del DTO/Modelo local.</typeparam>
    public sealed class ParityInspectionResult<TRemote, TLocal>
    {
        public string EntityId { get; set; } = string.Empty;
        public DateTime InspectedAtUtc { get; set; } = DateTime.UtcNow;
        
        /// <summary>Datos tal cual vienen del servidor remoto SIGAFI.</summary>
        public TRemote? RemoteData { get; set; }
        
        /// <summary>Datos tal cual están guardados en la base de datos local.</summary>
        public TLocal? LocalData { get; set; }
        
        /// <summary>Determina si ambos registros existen y están sincronizados (basado en lógica de negocio).</summary>
        public bool InParity { get; set; }
        
        /// <summary>Lista de discrepancias encontradas entre ambos lados.</summary>
        public List<string> Mismatches { get; set; } = new();

        /// <summary>Estado descriptivo de la entidad.</summary>
        public string Status => (RemoteData == null) 
            ? "NO_ENCONTRADO_EN_SIGAFI" 
            : (LocalData == null ? "PENDIENTE_DE_SINCRONIZACION" : (InParity ? "SINCRONIZADO_OK" : "DISCREPANCIA_DETECTADA"));
    }
}
