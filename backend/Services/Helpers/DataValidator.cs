using System.Text.RegularExpressions;
using backend.Models;

namespace backend.Services.Helpers
{
    /**
     * El "Escáner de Aduanas" de ISTPET
     * Sanitiza y limpia cualquier dato antes de enviarlo a la Base de Datos.
     */
    public class DataValidator
    {
        public static bool IsValidCedula(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula)) return false;
            // Solo dígitos, entre 10 y 15 caracteres
            return Regex.IsMatch(cedula, @"^\d{10,15}$");
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true; // Opcional en DB
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "SIN_NOMBRE";
            // Quitamos caracteres especiales y recortamos espacios
            return Regex.Replace(name, @"[^a-zA-Z\s]", "").Trim();
        }
    }
}
