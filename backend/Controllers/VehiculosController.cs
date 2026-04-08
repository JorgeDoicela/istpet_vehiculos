using AutoMapper;
using backend.DTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,logistica,guardia")]
    public class VehiculosController : ControllerBase
    {
        private readonly IVehiculoService _vehiculoService;
        private readonly IMapper _mapper;

        public VehiculosController(IVehiculoService vehiculoService, IMapper mapper)
        {
            _vehiculoService = vehiculoService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<VehiculoDto>>>> Get()
        {
            var vehiculos = await _vehiculoService.GetVehiculosAsync();

            // 🤖 AUTOMÁTICO: Conversión mágica de colecciones
            var dtoList = _mapper.Map<IEnumerable<VehiculoDto>>(vehiculos);

            return Ok(ApiResponse<IEnumerable<VehiculoDto>>.Ok(dtoList));
        }

        [HttpGet("{placa}")]
        public async Task<ActionResult<ApiResponse<VehiculoDto>>> GetByPlaca(string placa)
        {
            var vehiculo = await _vehiculoService.GetVehiculoByPlacaAsync(placa);
            if (vehiculo == null)
            {
                return NotFound(ApiResponse<VehiculoDto>.Fail("Vehículo no encontrado."));
            }

            var dto = _mapper.Map<VehiculoDto>(vehiculo);

            return Ok(ApiResponse<VehiculoDto>.Ok(dto));
        }
    }
}
