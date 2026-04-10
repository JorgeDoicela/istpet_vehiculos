using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * Academic Period Model: SIGAFI Parity 2026.
     * Aligned with SIGAFI 'periodos' table schema.
     */
    public class Periodo
    {
        [Key]
        [MaxLength(7)]
        public string idPeriodo { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? detalle { get; set; }

        public DateTime? fecha_inicial { get; set; }
        public DateTime? fecha_final { get; set; }

        public bool cerrado { get; set; } = false;
        public DateTime? fecha_maxima_autocierre { get; set; }
        public bool activo { get; set; } = true;
        public bool creditos { get; set; } = false;
        public int numero_pagos { get; set; } = 1;
        public DateTime? fecha_matrucla_extraordinaria { get; set; }
        public int? foliop { get; set; }
        
        public bool permiteMatricula { get; set; } = false;
        public bool ingresoCalificaciones { get; set; } = false;
        public bool permiteCalificacionesInstituto { get; set; } = false;
        public bool periodoactivoinstituto { get; set; } = false;
        public bool visualizaPowerBi { get; set; } = false;
        public bool esInstituto { get; set; } = false;
        public bool periodoPlanificacion { get; set; } = false;
    }
}
