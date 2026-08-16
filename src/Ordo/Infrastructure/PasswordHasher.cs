using System;
using System.Security.Cryptography;
using System.Text;

namespace Ordo.Infrastructure
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(password));
            }

            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = SHA3_256.HashData(bytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            var expectedHash = storedHash.Trim();
            var computedHash = Hash(password);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHash),
                Encoding.UTF8.GetBytes(computedHash));
        }
    }
}
