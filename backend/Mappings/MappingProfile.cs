using AutoMapper;
using backend.Models;
using backend.DTOs;

namespace backend.Mappings
{
    /**
     * Perfil Maestro de Automatización (AutoMapper)
     * Centraliza las reglas de transformación de datos para una escalabilidad masiva.
     */
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Estudiante -> EstudianteDto
            CreateMap<Estudiante, EstudianteDto>()
                .ForMember(dest => dest.NombreCompleto, 
                           opt => opt.MapFrom(src => $"{src.Apellidos} {src.Nombres}"));

            // Vehiculo -> VehiculoDto
            CreateMap<Vehiculo, VehiculoDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id_Vehiculo))
                .ForMember(dest => dest.Numero, opt => opt.MapFrom(src => $"V-{src.NumeroVehiculo}"))
                .ForMember(dest => dest.MarcaModelo, opt => opt.MapFrom(src => $"{src.Marca} {src.Modelo}"))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.EstadoMecanico));
        }
    }
}
