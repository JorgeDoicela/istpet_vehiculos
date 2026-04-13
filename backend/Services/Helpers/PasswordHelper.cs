using System;

namespace backend.Services.Helpers
{
    /// <summary>
    /// Lógica de validación de contraseñas ajustada a SIGAFI clásico.
    /// Soporta solo la validación en texto plano (límite de 20 caracteres en base de datos).
    /// </summary>
    public static class PasswordHelper
    {
        public static bool TryValidate(string stored, string? provided, out bool needsRehash)
        {
            needsRehash = false; // No usamos rehashing ya que no podemos pasar de 20 chars en SIGAFI.
            stored ??= string.Empty;

            if (string.Equals(stored, provided ?? string.Empty, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}
