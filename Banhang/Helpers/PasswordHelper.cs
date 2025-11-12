using System;
using System.Security.Cryptography;
using System.Text;

namespace Banhang.Helpers
{
    public static class PasswordHelper
    {
        private static string HashPasswordLegacy(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha256.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        private static bool IsBcryptHash(string? hash)
            => !string.IsNullOrEmpty(hash) && hash.StartsWith("$2", StringComparison.Ordinal);

        public static bool IsLegacyHash(string? hash) => !IsBcryptHash(hash);

        public static string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password);

        public static bool VerifyPassword(string password, string? storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            if (IsBcryptHash(storedHash))
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }

            var legacyHash = HashPasswordLegacy(password);
            return string.Equals(legacyHash, storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
