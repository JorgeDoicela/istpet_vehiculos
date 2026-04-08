namespace backend.DTOs
{
    /**
     * Auth DTOs: Refactored 2026.
     */
    public class LoginRequest
    {
        public string? usuario { get; set; }
        public string? password { get; set; }

        // Backwards compatibility for legacy code
        public string? Usuario => usuario;
        public string? Password => password;
    }

    public class LoginResponse
    {
        public string token { get; set; } = string.Empty;
        public int id_usuario { get; set; }
        public string usuario { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string rol { get; set; } = "guardia";
    }
}
