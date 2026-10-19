using System.Security.Cryptography;

namespace QuanLyLichHoc.Services
{
    /// <summary>Băm mật khẩu PBKDF2-SHA256 kèm salt ngẫu nhiên. Định dạng: v1.iter.salt.hash</summary>
    public static class PasswordHasher
    {
        private const int Iterations = 100_000, SaltSize = 16, KeySize = 32;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            var p = stored.Split('.');
            if (p.Length != 4 || p[0] != "v1" || !int.TryParse(p[1], out var iter)) return false;
            try
            {
                var salt = Convert.FromBase64String(p[2]);
                var expected = Convert.FromBase64String(p[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iter, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException) { return false; }
        }
    }
}
