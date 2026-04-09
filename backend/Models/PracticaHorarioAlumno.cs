using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /**
     * Practice-Schedule Junction Model: SIGAFI Parity.
     * Maps to sigafi_es.cond_practicas_horarios_alumnos
     * This links a Practice (cond_alumnos_practicas) to a Schedule (cond_alumnos_horarios).
     */
    public class PracticaHorarioAlumno
    {
        [Key, Column(Order = 0)]
        public int idPractica { get; set; }

        [Key, Column(Order = 1)]
        public int idAsignacionHorario { get; set; }

        // Navigation (Internal Logistics Logic)
        [ForeignKey("idPractica")]
        public virtual Practica? Practica { get; set; }

        [ForeignKey("idAsignacionHorario")]
        public virtual HorarioAlumno? Horario { get; set; }
    }
}
