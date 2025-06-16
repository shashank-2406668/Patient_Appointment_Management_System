// FILE: Utils/PasswordHelper.cs
using System;
using System.Security.Cryptography; // Required for Rfc2898DeriveBytes etc.

namespace Patient_Appointment_Management_System.Utils
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 10000; // .NET Core 3.1+ default is 10000, .NET 6+ is 100,000 for Identity
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
        private const char SaltDelimiter = ';';



        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                // It's often better to return an empty string or a specific invalid hash marker
                // rather than throwing an ArgumentNullException from a helper like this,
                // to prevent application crashes if an empty password somehow gets here.
                // However, following your provided code:
                throw new ArgumentNullException(nameof(password));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                Algorithm,
                KeySize
            );
            return string.Join(SaltDelimiter, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password))
            {
                // Consistent with HashPassword, or return false.
                throw new ArgumentNullException(nameof(password));
            }
            if (string.IsNullOrEmpty(hashedPassword))
            {
                return false; // Cannot verify against an empty/null hash
            }

            string[] parts = hashedPassword.Split(SaltDelimiter);
            if (parts.Length != 2)
            {
                // Invalid hash format stored in the database or passed.
                // Consider logging this as an error if it occurs.
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] hash = Convert.FromBase64String(parts[1]);

                byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    Iterations,
                    Algorithm,
                    KeySize
                );
                // Use CryptographicOperations.FixedTimeEquals for security against timing attacks.
                return CryptographicOperations.FixedTimeEquals(hash, testHash);
            }
            catch (FormatException)
            {
                // One of the Base64 strings was not in a valid format.
                // Consider logging this as an error.
                return false;
            }
        }
    }
}