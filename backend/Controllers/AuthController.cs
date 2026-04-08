using backend.Data;
using backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest req)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioLogin == req.Usuario && u.Activo);

            if (user == null)
            {
                // Delay artificial para evitar ataques de timing (Opcional en ultra-seguro)
                await Task.Delay(500); 
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));
            }

            bool isValid = false;
            bool needsRehash = false;

            // 🛡️ PUENTE DE AUTENTICACIÓN MIGRATORIA (Zenith Auth Bridge 2026)
            
            // 1. Detección de Hash SIGAFI/Moderno (BCrypt)
            if (user.PasswordHash.StartsWith("$2a$") || user.PasswordHash.StartsWith("$2b$"))
            {
                try
                {
                    isValid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
                }
                catch { isValid = false; }
            }
            else
            {
                // 2. Validación de Texto Plano (Legacy SIGAFI)
                // Usado para la primera sincronización de usuarios_web
                isValid = string.Equals(user.PasswordHash, req.Password);
                
                if (!isValid)
                {
                    // 3. Validación de Hash SHA-256 (Legacy ISTPET interno)
                    string calculatedHash = ComputeSha256Hash(req.Password);
                    isValid = string.Equals(user.PasswordHash, calculatedHash, StringComparison.OrdinalIgnoreCase);
                }

                // 🛡️ RE-HASH AUTOMÁTICO A BCRYPT (Protección Proactiva)
                if (isValid)
                {
                    needsRehash = true;
                }
            }

            if (!isValid)
            {
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));
            }

            // Aplicar re-hash si es necesario (Mejora proactiva de seguridad)
            if (needsRehash)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
                await _context.SaveChangesAsync();
            }

            // 🛡️ GENERACIÓN DE JWT (Ultra Seguro)
            var token = CreateToken(user);

            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                Token = token,
                IdUsuario = user.Id_Usuario,
                Usuario = user.UsuarioLogin,
                Nombre = user.NombreCompleto ?? "Usuario ISTPET",
                Rol = user.Rol
            }, "Ingreso exitoso. Sesión protegida con JWT."));
        }

        private string CreateToken(backend.Models.Usuario user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "SUPER_SECRET_KEY_PROD_2026_DEFAULT"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id_Usuario.ToString()),
                new Claim(ClaimTypes.Name, user.UsuarioLogin),
                new Claim(ClaimTypes.Role, user.Rol)
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
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
