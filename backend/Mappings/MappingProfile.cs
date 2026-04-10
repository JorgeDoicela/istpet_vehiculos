using AutoMapper;
using backend.Models;
using backend.DTOs;

namespace backend.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Direct 1:1 Mappings (Property names match exactly)
            CreateMap<Estudiante, EstudianteDto>();
            CreateMap<Instructor, InstructorDto>();

            CreateMap<Vehiculo, VehiculoDto>()
                .ForMember(dest => dest.activo, opt => opt.MapFrom(src => src.activo));

            CreateMap<Matricula, EstudianteLogisticaResponse>()
                .ForMember(dest => dest.idAlumno, opt => opt.MapFrom(src => src.idAlumno))
                .ForMember(dest => dest.idMatricula, opt => opt.MapFrom(src => src.idMatricula))
                .ForMember(dest => dest.paralelo, opt => opt.MapFrom(src => src.paralelo))
                .ForMember(dest => dest.nivel, opt => opt.MapFrom(src => src.Curso != null ? src.Curso.Nivel : "N/A"));

            // Mirroring Practice DTO logic
            CreateMap<Practica, ReportePracticasDTO>()
                .ForMember(dest => dest.idPractica, opt => opt.MapFrom(src => src.idPractica))
                .ForMember(dest => dest.idAlumno, opt => opt.MapFrom(src => src.idalumno))
                .ForMember(dest => dest.numeroVehiculo, opt => opt.MapFrom(src => src.Vehiculo != null ? (src.Vehiculo.numero_vehiculo ?? "0") : "0"))
                .ForMember(dest => dest.profesor, opt => opt.MapFrom(src => src.Instructor != null ? src.Instructor.nombres : "N/A"))
                .ForMember(dest => dest.horaSalida, opt => opt.MapFrom(src => src.hora_salida.HasValue ? src.hora_salida.Value.ToString(@"hh\:mm\:ss") : "--:--:--"))
                .ForMember(dest => dest.horaLlegada, opt => opt.MapFrom(src => src.hora_llegada.HasValue ? src.hora_llegada.Value.ToString(@"hh\:mm\:ss") : null))
                .ForMember(dest => dest.tiempo, opt => opt.MapFrom(src => src.tiempo.HasValue ? src.tiempo.Value.ToString(@"hh\:mm\:ss") : "00:00:00"));

            CreateMap<Vehiculo, VehiculoLogisticaResponse>()
                .ForMember(dest => dest.numeroVehiculo, opt => opt.MapFrom(src => src.numero_vehiculo ?? "0"))
                .ForMember(dest => dest.vehiculoStr, opt => opt.MapFrom(src => $"{src.placa} - #{src.numero_vehiculo}"))
                .ForMember(dest => dest.idInstructorFijo, opt => opt.MapFrom(src => src.id_instructor_fijo))
                .ForMember(dest => dest.instructorNombre, opt => opt.MapFrom(src => src.InstructorFijo != null ? src.InstructorFijo.nombres : "SIN INSTRUCTOR"));
        }
    }
}
