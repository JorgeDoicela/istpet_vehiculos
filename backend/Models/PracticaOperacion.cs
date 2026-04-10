using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class PracticaOperacion
    {
        [Key]
        public int idPractica { get; set; }

        public string? observaciones { get; set; }
    }
}
