namespace backend.DTOs
{
    /// <summary>
    /// Resultado de comprobar lecturas remotas SIGAFI (misma lógica que MasterSync / API).
    /// </summary>
    public sealed class SigafiProbeResponse
    {
        public bool Connected { get; set; }
        public DateTime CheckedAtUtc { get; set; }
        public List<SigafiProbeModuleResult> Modules { get; set; } = new();
        public string? SampleAlumnoId { get; set; }
        public bool? SampleAlumnoDetailOk { get; set; }
        public string? SampleAlumnoError { get; set; }
    }

    public sealed class SigafiProbeModuleResult
    {
        public string Name { get; set; } = string.Empty;
        public int? RowCount { get; set; }
        public bool Ok { get; set; }
        public string? Error { get; set; }
        public object? Sample { get; set; }
    }
}
