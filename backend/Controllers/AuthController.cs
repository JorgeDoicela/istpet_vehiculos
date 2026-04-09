using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ICentralStudentProvider _central;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            IConfiguration config,
            ICentralStudentProvider central,
            ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _central = central;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest req)
        {
            if (req == null)
            {
                return BadRequest(ApiResponse<LoginResponse>.Fail("Datos de acceso no proporcionados."));
            }

            if (string.IsNullOrEmpty(req.usuario))
            {
                return BadRequest(ApiResponse<LoginResponse>.Fail("El usuario es requerido."));
            }

            CentralUserDto? sigafiUser = null;
            try
            {
                sigafiUser = await _central.GetWebUserFromSigafiAsync(req.usuario);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SIGAFI usuarios_web no accesible; se usará copia local si existe.");
            }

            Usuario user;
            if (sigafiUser != null)
            {
                if (sigafiUser.activo == 0)
                    return Unauthorized(ApiResponse<LoginResponse>.Fail("Usuario inactivo en SIGAFI."));

                if (!TryValidatePassword(sigafiUser.password, req.password, out var needsRehash))
                {
                    await Task.Delay(400);
                    return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));
                }

                user = await UpsertLocalUsuarioFromSigafiAsync(sigafiUser, req.password, needsRehash);
            }
            else
            {
                user = await _context.Usuarios.FirstOrDefaultAsync(u => u.usuario == req.usuario && u.activo);
                if (user == null)
                {
                    await Task.Delay(500);
                    return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));
                }

                var storedPassword = user.password ?? string.Empty;
                if (!TryValidatePassword(storedPassword, req.password, out var needsRehash))
                    return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));

                if (needsRehash)
                {
                    user.password = BCrypt.Net.BCrypt.HashPassword(req.password ?? string.Empty);
                    await _context.SaveChangesAsync();
                }
            }

            user.rol = "guardia";
            if (user.salida && user.ingreso) user.rol = "admin";
            else if (user.salida) user.rol = "logistica";

            user.nombre_completo = await ResolveDisplayNameAsync(user.usuario);

            var token = CreateToken(user);

            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                token = token,
                usuario = user.usuario,
                nombre = user.nombre_completo ?? "Usuario ISTPET",
                rol = user.rol
            }, sigafiUser != null ? "Ingreso exitoso (validado en SIGAFI)." : "Ingreso exitoso (copia local; SIGAFI no respondió)."));
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

        /// <summary>Nombre para mostrar: primero SIGAFI (profesor / alumno), si no hay datos, tablas locales.</summary>
        private async Task<string> ResolveDisplayNameAsync(string usuario)
        {
            try
            {
                var ins = await _central.GetInstructorFromCentralAsync(usuario);
                if (ins != null)
                {
                    var n = $"{ins.apellidos} {ins.nombres}".Trim();
                    if (string.IsNullOrWhiteSpace(n))
                        n = $"{ins.primerApellido} {ins.primerNombre}".Trim();
                    if (!string.IsNullOrWhiteSpace(n))
                        return n;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Nombre instructor SIGAFI no disponible para {usuario}", usuario);
            }

            try
            {
                var alumnoCentral = await _central.GetFromCentralAsync(usuario);
                var nombreAlumno = alumnoCentral?.NombreCompleto;
                if (!string.IsNullOrWhiteSpace(nombreAlumno))
                    return nombreAlumno.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Nombre alumno SIGAFI no disponible para {usuario}", usuario);
            }

            var profesor = await _context.Instructores.FirstOrDefaultAsync(p => p.idProfesor == usuario);
            if (profesor != null)
            {
                var fullName = $"{profesor.apellidos} {profesor.nombres}".Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = $"{profesor.primerApellido} {profesor.primerNombre}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName))
                    return fullName;
            }

            var alumno = await _context.Estudiantes.FirstOrDefaultAsync(a => a.idAlumno == usuario);
            if (alumno != null)
            {
                return $"{alumno.apellidoPaterno} {alumno.apellidoMaterno} {alumno.primerNombre}".Trim();
            }

            return "Usuario ISTPET";
        }

        private static bool TryValidatePassword(string stored, string? provided, out bool needsRehash)
        {
            needsRehash = false;
            stored ??= string.Empty;

            if (stored.StartsWith("$2a$", StringComparison.Ordinal) || stored.StartsWith("$2b$", StringComparison.Ordinal))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(provided ?? string.Empty, stored);
                }
                catch
                {
                    return false;
                }
            }

            if (string.Equals(stored, provided ?? string.Empty, StringComparison.Ordinal))
            {
                needsRehash = true;
                return true;
            }

            var calculatedHash = ComputeSha256Hash(provided ?? string.Empty);
            if (string.Equals(stored, calculatedHash, StringComparison.OrdinalIgnoreCase))
            {
                needsRehash = true;
                return true;
            }

            return false;
        }

        private string CreateToken(Usuario user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "SUPER_SECRET_KEY_PROD_2026_DEFAULT"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.usuario),
                new Claim(ClaimTypes.Name, user.usuario),
                new Claim(ClaimTypes.Role, user.rol ?? "user")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"] ?? "480")),
                SigningCredentials = creds,
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256Hash = SHA256.Create();
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder();
            foreach (var b in bytes) builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}
