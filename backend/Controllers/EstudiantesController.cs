using backend.Data;
using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers
{
    /**
     * Estudiantes Controller: Absolute SIGAFI Naming Parity 2026.
     */
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin,logistica,guardia")]
    public class EstudiantesController : ControllerBase
    {
        private readonly IEstudianteService _estudianteService;
        private readonly IMapper _mapper;

        public EstudiantesController(IEstudianteService estudianteService, IMapper mapper)
        {
            _estudianteService = estudianteService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EstudianteDto>>> GetAll()
        {
            var estudiantes = await _estudianteService.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<EstudianteDto>>(estudiantes));
        }

        [HttpGet("{idAlumno}")]
        public async Task<ActionResult<EstudianteDto>> GetByIdAlumno(string idAlumno)
        {
            var estudiante = await _estudianteService.GetByIdAlumnoAsync(idAlumno);
            if (estudiante == null) return NotFound();
            return Ok(_mapper.Map<EstudianteDto>(estudiante));
        }
    }
}
