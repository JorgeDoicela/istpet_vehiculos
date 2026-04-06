using backend.Data;
using backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest req)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioLogin == req.Usuario && u.Activo);

            if (user == null)
            {
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Usuario no encontrado o inactivo."));
            }

            bool isValid = false;

            // --- PUENTE HÍBRIDO DE SEGURIDAD (Zenith Auth Bridge) ---
            
            // 1. Detección de Hash Legacy (SIGAFI BCrypt)
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
                // 2. Validación de Hash Moderno (SHA-256 Hex)
                // Usado para registros nuevos o el admin inicial del SQL_SCHEMA
                string calculatedHash = ComputeSha256Hash(req.Password);
                isValid = string.Equals(user.PasswordHash, calculatedHash, StringComparison.OrdinalIgnoreCase);
            }

            if (!isValid)
            {
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Contraseña incorrecta."));
            }

            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                IdUsuario = user.Id_Usuario,
                Usuario = user.UsuarioLogin,
                Nombre = user.NombreCompleto ?? "Usuario ISTPET",
                Rol = user.Rol
            }, "Ingreso exitoso mediante el Puente de Seguridad Híbrida."));
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
