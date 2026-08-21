using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;

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

            var input = Encoding.UTF8.GetBytes(password);
            var digest = new Sha3Digest(256);
            var output = new byte[digest.GetDigestSize()];

            digest.BlockUpdate(input, 0, input.Length);
            digest.DoFinal(output, 0);

            return Convert.ToHexString(output).ToLowerInvariant();
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            var expectedHash = storedHash.Trim();
            var computedHash = Hash(password);

            var expectedBytes = Encoding.UTF8.GetBytes(expectedHash.ToLowerInvariant());
            var computedBytes = Encoding.UTF8.GetBytes(computedHash);

            return CryptographicOperations.FixedTimeEquals(expectedBytes, computedBytes);
        }
    }
}
