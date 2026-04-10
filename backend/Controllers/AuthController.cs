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

            // Prioridad: validar en SIGAFI. Si SIGAFI no responde, usar espejo local (Master Sync / último login)
            // para que el sistema siga operativo; si SIGAFI responde y el usuario no existe, no se usa local.
            CentralUserDto? sigafiUser = null;
            var sigafiNoDisponible = false;
            try
            {
                sigafiUser = await _central.GetWebUserFromSigafiAsync(req.usuario);
            }
            catch (Exception ex)
            {
                sigafiNoDisponible = true;
                _logger.LogWarning(ex, "SIGAFI usuarios_web no accesible; se intentará login con espejo local.");
            }

            Usuario user;
            string mensajeExito;

            if (sigafiUser != null)
            {
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

                user = await UpsertLocalUsuarioFromSigafiAsync(sigafiUser, req.password, needsRehash);
                mensajeExito = "Ingreso exitoso (validado en SIGAFI).";
            }
            else if (sigafiNoDisponible)
            {
                var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.usuario == req.usuario && u.activo);
                if (localUser == null)
                {
                    await Task.Delay(500);
                    await _audit.LogAsync(req.usuario, "LOGIN_FAIL",
                        detalles: "SIGAFI caído y sin usuario activo en espejo local.",
                        ipOrigen: ip);
                    return StatusCode(503, ApiResponse<LoginResponse>.Fail(
                        "SIGAFI no responde y no hay copia local de este usuario. Intente más tarde o sincronice cuando haya conexión."));
                }

                if (!PasswordHelper.TryValidate(localUser.password ?? string.Empty, req.password, out var needsRehashLocal))
                {
                    await _audit.LogAsync(req.usuario, "LOGIN_FAIL",
                        detalles: "Credenciales inválidas (espejo local).",
                        ipOrigen: ip);
                    return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));
                }

                if (needsRehashLocal)
                {
                    localUser.password = BCrypt.Net.BCrypt.HashPassword(req.password ?? string.Empty);
                    await _context.SaveChangesAsync();
                }

                user = localUser;
                mensajeExito = "Ingreso en modo respaldo (última copia local). Cuando SIGAFI vuelva, los datos se actualizarán al sincronizar.";
            }
            else
            {
                await Task.Delay(500);
                await _audit.LogAsync(req.usuario, "LOGIN_FAIL",
                    detalles: "Usuario no encontrado en SIGAFI.",
                    ipOrigen: ip);
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));
            }

            user.rol = "guardia";
            if (user.salida && user.ingreso) user.rol = "admin";
            else if (user.salida) user.rol = "logistica";

            // Nombre para el JWT: espejo local primero. SIGAFI puede tardar mucho (timeouts TCP)
            // desde Docker y dejaba el login colgado si se consultaba antes que el espejo.
            user.nombre_completo = await ResolveDisplayNameAsync(user.usuario, sigafiNoDisponible);

            var token = CreateToken(user);

            await _audit.LogAsync(user.usuario, "LOGIN",
                detalles: $"{mensajeExito} Rol: {user.rol}",
                ipOrigen: ip);

            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                token = token,
                usuario = user.usuario,
                nombre = user.nombre_completo ?? "Usuario ISTPET",
                rol = user.rol
            }, mensajeExito));
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

        private static string? FormatoNombreInstructor(Instructor? p)
        {
            if (p == null) return null;
            var full = $"{p.apellidos} {p.nombres}".Trim();
            if (string.IsNullOrWhiteSpace(full)) full = $"{p.primerApellido} {p.primerNombre}".Trim();
            return string.IsNullOrWhiteSpace(full) ? null : full;
        }

        private static string? FormatoNombreAlumno(Estudiante? a)
        {
            if (a == null) return null;
            var s = $"{a.apellidoPaterno} {a.apellidoMaterno} {a.primerNombre}".Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        /// <param name="sigafiNoDisponibleEnLogin">
        /// Si ya falló <see cref="ICentralStudentProvider.GetWebUserFromSigafiAsync"/> con excepción,
        /// no volvemos a llamar a SIGAFI aquí (evita esperas largas en cadena).
        /// </param>
        private async Task<string> ResolveDisplayNameAsync(string usuario, bool sigafiNoDisponibleEnLogin)
        {
            var prof = await _context.Instructores.AsNoTracking()
                .FirstOrDefaultAsync(p => p.idProfesor == usuario);
            var localName = FormatoNombreInstructor(prof);
            if (!string.IsNullOrWhiteSpace(localName))
                return localName!;

            var alum = await _context.Estudiantes.AsNoTracking()
                .FirstOrDefaultAsync(a => a.idAlumno == usuario);
            localName = FormatoNombreAlumno(alum);
            if (!string.IsNullOrWhiteSpace(localName))
                return localName!;

            if (sigafiNoDisponibleEnLogin)
                return string.IsNullOrWhiteSpace(usuario) ? "Usuario ISTPET" : usuario.Trim();

            var desdeSigafi = await TryResolveDisplayNameFromSigafiWithTimeoutAsync(usuario, TimeSpan.FromSeconds(2.5));
            if (!string.IsNullOrWhiteSpace(desdeSigafi))
                return desdeSigafi!;

            return string.IsNullOrWhiteSpace(usuario) ? "Usuario ISTPET" : usuario.Trim();
        }

        private async Task<string?> TryResolveDisplayNameFromSigafiWithTimeoutAsync(string usuario, TimeSpan maxWait)
        {
            var lookup = ResolveDisplayNameFromSigafiOnlyAsync(usuario);
            var completed = await Task.WhenAny(lookup, Task.Delay(maxWait));
            if (completed != lookup)
            {
                _logger.LogDebug("Nombre para login: SIGAFI no respondió en {Ms} ms; se usa login como etiqueta.", maxWait.TotalMilliseconds);
                return null;
            }

            try
            {
                return await lookup;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Nombre SIGAFI no resuelto para {u}", usuario);
                return null;
            }
        }

        private async Task<string?> ResolveDisplayNameFromSigafiOnlyAsync(string usuario)
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
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Nombre instructor SIGAFI no disponible para {u}", usuario);
            }

            try
            {
                var alumnoCentral = await _central.GetFromCentralAsync(usuario);
                if (!string.IsNullOrWhiteSpace(alumnoCentral?.NombreCompleto))
                    return alumnoCentral!.NombreCompleto.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Nombre alumno SIGAFI no disponible para {u}", usuario);
            }

            return null;
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
