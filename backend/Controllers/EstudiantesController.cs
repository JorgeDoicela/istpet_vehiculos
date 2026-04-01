using AutoMapper;
using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstudiantesController : ControllerBase
    {
        private readonly IEstudianteService _estudianteService;
        private readonly IMapper _mapper;

        public EstudiantesController(IEstudianteService estudianteService, IMapper mapper)
        {
            _estudianteService = estudianteService;
            _mapper = mapper;
        }

        [HttpGet("{cedula}")]
        public async Task<ActionResult<ApiResponse<EstudianteDto>>> GetByCedula(string cedula)
        {
            var estudiante = await _estudianteService.GetByCedulaAsync(cedula);
            if (estudiante == null)
            {
                return NotFound(ApiResponse<EstudianteDto>.Fail("Estudiante no encontrado."));
            }

            // 🤖 AUTOMÁTICO: AutoMapper hace todo el trabajo
            var dto = _mapper.Map<EstudianteDto>(estudiante);

            return Ok(ApiResponse<EstudianteDto>.Ok(dto));
        }
    }
}
