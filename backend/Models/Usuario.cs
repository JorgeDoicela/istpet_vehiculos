using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /**
     * ISTPET Enterprise User Model
     * Aligned with SQL Security Schema
     */
    public class Usuario
    {
        [Key]
        public int Id_Usuario { get; set; }

        [Required]
        [MaxLength(50)]
        public string UsuarioLogin { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public string Rol { get; set; } = "guardia";

        [MaxLength(100)]
        public string? NombreCompleto { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime CreadoEn { get; set; } = DateTime.Now;
    }
}
