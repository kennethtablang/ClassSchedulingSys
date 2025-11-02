using System.Security.Cryptography;
using System.Text;

namespace ClassSchedulingSys.Helpers
{
    public static class TwoFactorHelper
    {
        // Simple SHA256 hash: code + secret
        // Use a secret from configuration so same code can't be guessed by reading DB
        public static string ComputeHash(string code, string secret)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(code + "|" + secret);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // Generate a 6-digit numeric code
        public static string GenerateNumericCode(int digits = 6)
        {
            var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var value = Math.Abs(BitConverter.ToInt32(bytes, 0));
            var max = (int)Math.Pow(10, digits);
            var code = (value % max).ToString().PadLeft(digits, '0');
            return code;
        }
    }
}
