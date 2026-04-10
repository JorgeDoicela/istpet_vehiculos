using System.Security.Cryptography;
using System.Text;

namespace backend.Services.Helpers
{
    /// <summary>
    /// Lógica de validación de contraseñas desacoplada del controlador.
    /// Soporta tres formatos heredados de SIGAFI:
    ///   1. BCrypt ($2a$/$2b$) — formato moderno preferido.
    ///   2. SHA-256 hex        — formato intermedio de SIGAFI.
    ///   3. Texto plano        — contraseñas antiguas (siempre fuerza rehash a BCrypt).
    /// </summary>
    public static class PasswordHelper
    {
        public static bool TryValidate(string stored, string? provided, out bool needsRehash)
        {
            needsRehash = false;
            stored ??= string.Empty;

            if (stored.StartsWith("$2a$", StringComparison.Ordinal)
                || stored.StartsWith("$2b$", StringComparison.Ordinal))
            {
                try { return BCrypt.Net.BCrypt.Verify(provided ?? string.Empty, stored); }
                catch { return false; }
            }

            if (string.Equals(stored, provided ?? string.Empty, StringComparison.Ordinal))
            {
                needsRehash = true;
                return true;
            }

            var hash = ComputeSha256(provided ?? string.Empty);
            if (string.Equals(stored, hash, StringComparison.OrdinalIgnoreCase))
            {
                needsRehash = true;
                return true;
            }

            return false;
        }

        public static string ComputeSha256(string rawData)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var sb = new StringBuilder(64);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
