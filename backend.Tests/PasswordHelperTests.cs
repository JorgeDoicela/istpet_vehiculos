using backend.Services.Helpers;
using Xunit;

namespace backend.Tests
{
    /// <summary>
    /// Tests unitarios para la lógica de validación de contraseñas.
    /// Cubre los tres formatos heredados soportados por el sistema.
    /// </summary>
    public class PasswordHelperTests
    {
        // ─────────────────────────────────────────────────────
        // BCrypt (formato moderno)
        // ─────────────────────────────────────────────────────

        [Fact]
        public void TryValidate_BcryptHash_CorrectPassword_ReturnsTrue()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("contraseña123");
            var result = PasswordHelper.TryValidate(hash, "contraseña123", out var needsRehash);
            Assert.True(result);
            Assert.False(needsRehash);
        }

        [Fact]
        public void TryValidate_BcryptHash_WrongPassword_ReturnsFalse()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("correcta");
            var result = PasswordHelper.TryValidate(hash, "incorrecta", out _);
            Assert.False(result);
        }

        // ─────────────────────────────────────────────────────
        // SHA-256 (formato intermedio SIGAFI)
        // ─────────────────────────────────────────────────────

        [Fact]
        public void TryValidate_Sha256Hash_CorrectPassword_ReturnsTrueAndNeedsRehash()
        {
            var sha = PasswordHelper.ComputeSha256("mi_password");
            var result = PasswordHelper.TryValidate(sha, "mi_password", out var needsRehash);
            Assert.True(result);
            Assert.True(needsRehash);
        }

        [Fact]
        public void TryValidate_Sha256Hash_WrongPassword_ReturnsFalse()
        {
            var sha = PasswordHelper.ComputeSha256("correcta");
            var result = PasswordHelper.TryValidate(sha, "incorrecta", out _);
            Assert.False(result);
        }

        // ─────────────────────────────────────────────────────
        // Texto plano (contraseñas antiguas)
        // ─────────────────────────────────────────────────────

        [Fact]
        public void TryValidate_PlainText_CorrectPassword_ReturnsTrueAndNeedsRehash()
        {
            var result = PasswordHelper.TryValidate("plain123", "plain123", out var needsRehash);
            Assert.True(result);
            Assert.True(needsRehash);
        }

        [Fact]
        public void TryValidate_PlainText_WrongPassword_ReturnsFalse()
        {
            var result = PasswordHelper.TryValidate("plain123", "wrong", out _);
            Assert.False(result);
        }

        // ─────────────────────────────────────────────────────
        // Casos límite
        // ─────────────────────────────────────────────────────

        [Fact]
        public void TryValidate_NullProvided_DoesNotThrow()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("algo");
            var result = PasswordHelper.TryValidate(hash, null, out _);
            Assert.False(result);
        }

        [Fact]
        public void TryValidate_EmptyStored_DoesNotThrow()
        {
            var result = PasswordHelper.TryValidate(string.Empty, "algo", out _);
            Assert.False(result);
        }

        [Fact]
        public void ComputeSha256_IsHexString_LengthIs64()
        {
            var hash = PasswordHelper.ComputeSha256("test");
            Assert.Equal(64, hash.Length);
            Assert.Matches("^[0-9a-f]+$", hash);
        }

        [Fact]
        public void ComputeSha256_SameInput_SameOutput()
        {
            var h1 = PasswordHelper.ComputeSha256("istpet2026");
            var h2 = PasswordHelper.ComputeSha256("istpet2026");
            Assert.Equal(h1, h2);
        }
    }
}
