using System;
using System.Security.Cryptography;
using System.Text;

namespace Banhang.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
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

        public static bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
