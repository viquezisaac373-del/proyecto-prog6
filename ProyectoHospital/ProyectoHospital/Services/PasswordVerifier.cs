using System.Security.Cryptography;
using System.Text;

namespace ProyectoHospital.Services
{
    public static class PasswordVerifier
    {
        public static bool VerifySha512(byte[] salt, byte[] storedHash, string inputPassword)
        {
            byte[] passwordBytes = Encoding.Unicode.GetBytes(inputPassword);

            byte[] combined = new byte[salt.Length + passwordBytes.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, salt.Length, passwordBytes.Length);

            byte[] computedHash = SHA512.HashData(combined);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
    }
}
