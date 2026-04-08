using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using backend.Data;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,logistica,guardia")]
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
