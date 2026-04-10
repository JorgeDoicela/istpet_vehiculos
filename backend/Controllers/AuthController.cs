using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services.Helpers;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ICentralStudentProvider _central;
        private readonly IAuditService _audit;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            IConfiguration config,
            ICentralStudentProvider central,
            IAuditService audit,
            ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _central = central;
            _audit = audit;
            _logger = logger;
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (req == null || string.IsNullOrEmpty(req.usuario))
                return BadRequest(ApiResponse<LoginResponse>.Fail("El usuario es requerido."));

            // SIGAFI es la única fuente de verdad para autenticación.
            // Si no responde no se acepta la copia local: un usuario desactivado en SIGAFI
            // no debe poder entrar aunque el espejo local aún lo tenga como activo.
            CentralUserDto? sigafiUser;
            try
            {
                sigafiUser = await _central.GetWebUserFromSigafiAsync(req.usuario);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SIGAFI usuarios_web no accesible al intentar login de {usuario}.", req.usuario);
                await _audit.LogAsync(req.usuario, "LOGIN_FAIL",
                    detalles: "SIGAFI no disponible; login bloqueado por seguridad.",
                    ipOrigen: ip);
                return StatusCode(503, ApiResponse<LoginResponse>.Fail(
                    "El servicio de autenticación (SIGAFI) no está disponible. Intente nuevamente en unos segundos."));
            }

            if (sigafiUser == null)
            {
                await Task.Delay(500);
                await _audit.LogAsync(req.usuario, "LOGIN_FAIL",
                    detalles: "Usuario no encontrado en SIGAFI.",
                    ipOrigen: ip);
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));
            }

            if (sigafiUser.activo == 0)
            {
                await _audit.LogAsync(req.usuario, "LOGIN_FAIL",
                    detalles: "Usuario inactivo en SIGAFI.",
                    ipOrigen: ip);
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Usuario inactivo. Contacte al administrador."));
            }

            if (!PasswordHelper.TryValidate(sigafiUser.password, req.password, out var needsRehash))
            {
                await Task.Delay(400);
                await _audit.LogAsync(req.usuario, "LOGIN_FAIL",
                    detalles: "Credenciales inválidas (SIGAFI).",
                    ipOrigen: ip);
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));
            }

            var user = await UpsertLocalUsuarioFromSigafiAsync(sigafiUser, req.password, needsRehash);

            user.rol = "guardia";
            if (user.salida && user.ingreso) user.rol = "admin";
            else if (user.salida) user.rol = "logistica";

            user.nombre_completo = await ResolveDisplayNameAsync(user.usuario);

            var token = CreateToken(user);

            await _audit.LogAsync(user.usuario, "LOGIN",
                detalles: $"Ingreso exitoso vía SIGAFI. Rol: {user.rol}",
                ipOrigen: ip);

            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                token = token,
                usuario = user.usuario,
                nombre = user.nombre_completo ?? "Usuario ISTPET",
                rol = user.rol
            }, "Ingreso exitoso (validado en SIGAFI)."));
        }

        private async Task<Usuario> UpsertLocalUsuarioFromSigafiAsync(CentralUserDto src, string? plainPassword, bool rehashToBcrypt)
        {
            var local = await _context.Usuarios.FindAsync(src.usuario);
            var passwordToStore = rehashToBcrypt && !string.IsNullOrEmpty(plainPassword)
                ? BCrypt.Net.BCrypt.HashPassword(plainPassword)
                : (src.password ?? string.Empty);

            if (local == null)
            {
                local = new Usuario
                {
                    usuario = src.usuario,
                    password = passwordToStore,
                    salida = src.salida != 0,
                    ingreso = src.ingreso != 0,
                    activo = src.activo != 0,
                    asistencia = src.asistencia != 0,
                    esRrhh = src.esRrhh != 0
                };
                _context.Usuarios.Add(local);
            }
            else
            {
                local.password = passwordToStore;
                local.salida = src.salida != 0;
                local.ingreso = src.ingreso != 0;
                local.activo = src.activo != 0;
                local.asistencia = src.asistencia != 0;
                local.esRrhh = src.esRrhh != 0;
            }

            await _context.SaveChangesAsync();
            return local;
        }

        private async Task<string> ResolveDisplayNameAsync(string usuario)
        {
            try
            {
                var ins = await _central.GetInstructorFromCentralAsync(usuario);
                if (ins != null)
                {
                    var n = $"{ins.apellidos} {ins.nombres}".Trim();
                    if (string.IsNullOrWhiteSpace(n)) n = $"{ins.primerApellido} {ins.primerNombre}".Trim();
                    if (!string.IsNullOrWhiteSpace(n)) return n;
                }
            }
            catch (Exception ex) { _logger.LogDebug(ex, "Nombre instructor SIGAFI no disponible para {u}", usuario); }

            try
            {
                var alumnoCentral = await _central.GetFromCentralAsync(usuario);
                if (!string.IsNullOrWhiteSpace(alumnoCentral?.NombreCompleto))
                    return alumnoCentral.NombreCompleto.Trim();
            }
            catch (Exception ex) { _logger.LogDebug(ex, "Nombre alumno SIGAFI no disponible para {u}", usuario); }

            var profesor = await _context.Instructores.FirstOrDefaultAsync(p => p.idProfesor == usuario);
            if (profesor != null)
            {
                var full = $"{profesor.apellidos} {profesor.nombres}".Trim();
                if (string.IsNullOrWhiteSpace(full)) full = $"{profesor.primerApellido} {profesor.primerNombre}".Trim();
                if (!string.IsNullOrWhiteSpace(full)) return full;
            }

            var alumno = await _context.Estudiantes.FirstOrDefaultAsync(a => a.idAlumno == usuario);
            if (alumno != null)
                return $"{alumno.apellidoPaterno} {alumno.apellidoMaterno} {alumno.primerNombre}".Trim();

            return "Usuario ISTPET";
        }

        private string CreateToken(Usuario user)
        {
            var jwtSection = _config.GetSection("JwtSettings");
            var rawKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? jwtSection["Key"]
                         ?? throw new InvalidOperationException("JWT Key no configurada.");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(rawKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.usuario),
                new Claim(ClaimTypes.Name, user.usuario),
                new Claim(ClaimTypes.Role, user.rol ?? "user")
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSection["ExpiryInMinutes"] ?? "480")),
                SigningCredentials = creds,
                Issuer = jwtSection["Issuer"],
                Audience = jwtSection["Audience"]
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }
    }
}
