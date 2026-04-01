using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TipoLicenciaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TipoLicenciaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TipoLicencia>>> Get()
        {
            // Esta es tu primera consulta REAL a la base de datos MySQL
            return await _context.TipoLicencias.ToListAsync();
        }
    }
}
