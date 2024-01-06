using System;
using System.Security.Cryptography;
using System.Text;

namespace Iotbcdg.Auth
{
    public class AuthHandler
    {
        private readonly string pepper;

        public AuthHandler(string pepper)
        {
            this.pepper = pepper;
        }

        public (string, string) HashPassword(string password)
        {
            string salt = GenerateSalt();
            using var sha256 = SHA256.Create();
            string combinedString = password + salt + pepper;
            byte[] combinedBytes = Encoding.UTF8.GetBytes(combinedString);
            byte[] hashBytes = sha256.ComputeHash(combinedBytes);

            return (Convert.ToBase64String(hashBytes), salt);
        }

        public bool VerifyPassword(string enteredPassword, string salt, string storedHashedPassword)
        {
            using var sha256 = SHA256.Create();
            string combinedString = enteredPassword + salt + pepper;
            byte[] combinedBytes = Encoding.UTF8.GetBytes(combinedString);
            byte[] enteredHashBytes = sha256.ComputeHash(combinedBytes);
            string enteredHash = Convert.ToBase64String(enteredHashBytes);

            return enteredHash == storedHashedPassword;
        }

        private static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }
    }
}