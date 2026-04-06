using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class RegistroSalida
    {
        [Key]
        public int Id_Registro { get; set; }

        public int IdMatricula { get; set; }

        public int IdVehiculo { get; set; }

        public int IdInstructor { get; set; }

        public DateTime FechaHoraSalida { get; set; } = DateTime.Now;



        public string? ObservacionesSalida { get; set; }

        public int? RegistradoPor { get; set; }

        // Navigation Properties
        [ForeignKey("IdMatricula")]
        public virtual Matricula? Matricula { get; set; }

        [ForeignKey("IdVehiculo")]
        public virtual Vehiculo? Vehiculo { get; set; }

        [ForeignKey("IdInstructor")]
        public virtual Instructor? Instructor { get; set; }

        [ForeignKey("RegistradoPor")]
        public virtual Usuario? UsuarioRegistrador { get; set; }
    }

    public class RegistroLlegada
    {
        [Key]
        public int Id_Llegada { get; set; }

        public int IdRegistro { get; set; }

        public DateTime FechaHoraLlegada { get; set; } = DateTime.Now;



        public string? ObservacionesLlegada { get; set; }

        public int? RegistradoPor { get; set; }

        // Navigation Properties
        [ForeignKey("IdRegistro")]
        public virtual RegistroSalida? RegistroSalida { get; set; }

        [ForeignKey("RegistradoPor")]
        public virtual Usuario? UsuarioRegistrador { get; set; }
    }
}
