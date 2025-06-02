// File: Patient_Appointment_Management_System/Utils/PasswordHelper.cs
using System;
using System.Security.Cryptography;

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
                throw new ArgumentNullException(nameof(password));
            }
            if (string.IsNullOrEmpty(hashedPassword))
            {
                // Consider logging this event: attempt to verify against an empty hash.
                return false;
            }

            string[] parts = hashedPassword.Split(SaltDelimiter);
            if (parts.Length != 2)
            {
                // Log an error: invalid hash format
                // This might happen if a non-hashed password or a hash in a different format is stored.
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
                return CryptographicOperations.FixedTimeEquals(hash, testHash);
            }
            catch (FormatException)
            {
                // Log an error: Base64 conversion failed, invalid hash format.
                return false;
            }
        }
    }
}