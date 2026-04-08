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
            // Matching usuario_login (snake_case) with req.usuario (if updated) or legacy req.Usuario
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.usuario_login == (req.usuario ?? req.Usuario) && u.activo);

            if (user == null)
            {
                await Task.Delay(500); 
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));
            }

            bool isValid = false;
            bool needsRehash = false;

            if (user.password_hash.StartsWith("$2a$") || user.password_hash.StartsWith("$2b$"))
            {
                try { isValid = BCrypt.Net.BCrypt.Verify(req.password ?? req.Password, user.password_hash); }
                catch { isValid = false; }
            }
            else
            {
                isValid = string.Equals(user.password_hash, req.password ?? req.Password);
                if (!isValid)
                {
                    string calculatedHash = ComputeSha256Hash(req.password ?? req.Password);
                    isValid = string.Equals(user.password_hash, calculatedHash, StringComparison.OrdinalIgnoreCase);
                }
                if (isValid) needsRehash = true;
            }

            if (!isValid) return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas."));

            if (needsRehash)
            {
                user.password_hash = BCrypt.Net.BCrypt.HashPassword(req.password ?? req.Password);
                await _context.SaveChangesAsync();
            }

            var token = CreateToken(user);

            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                token = token,
                id_usuario = user.id_usuario,
                usuario = user.usuario_login,
                nombre = user.nombre_completo ?? "Usuario ISTPET",
                rol = user.rol
            }, "Ingreso exitoso."));
        }

        private string CreateToken(backend.Models.Usuario user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "SUPER_SECRET_KEY_PROD_2026_DEFAULT"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.id_usuario.ToString()),
                new Claim(ClaimTypes.Name, user.usuario_login),
                new Claim(ClaimTypes.Role, user.rol)
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
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
