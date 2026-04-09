using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * ISTPET Enterprise License Types: Refactored 2026.
     */
    public class TipoLicencia
    {
        [Key]
        public int id_tipo { get; set; }

        [Required]
        [MaxLength(10)]
        public string codigo { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string descripcion { get; set; } = string.Empty;

        public bool activo { get; set; } = true;

        /// <summary>Si está definido, el registro proviene de SIGAFI categoria_vehiculos (mapeo estable).</summary>
        public int? id_categoria_sigafi { get; set; }
    }
}
