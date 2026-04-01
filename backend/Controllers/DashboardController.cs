using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("clases-activas")]
        public async Task<ActionResult<IEnumerable<ClaseActiva>>> GetClasesActivas()
        {
            return await _context.ClasesActivas.ToListAsync();
        }

        [HttpGet("alertas-mantenimiento")]
        public async Task<ActionResult<IEnumerable<AlertaMantenimiento>>> GetAlertasMantenimiento()
        {
            return await _context.AlertasMantenimiento.ToListAsync();
        }
    }
}
