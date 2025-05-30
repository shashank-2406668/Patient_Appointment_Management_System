// File: Patient_Appointment_Management_System/Utils/PasswordHelper.cs
using System.Security.Cryptography;
using System.Text;

namespace Patient_Appointment_Management_System.Utils
{
    public static class PasswordHelper
    {
        // IMPORTANT: For a real application, use a much stronger hashing algorithm
        // like Argon2 or at least BCrypt (available via NuGet packages like BCrypt.Net-Next).
        // SHA256 without salting is NOT secure enough for production. This is for demonstration.
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                // Consider throwing an ArgumentNullException or returning an empty string/null based on your policy
                // For this example, let's throw as a password should not be empty
                throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");
            }

            using (var sha256 = SHA256.Create())
            {
                // In a real app, you would also use a "salt" here.
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(enteredPassword) || string.IsNullOrEmpty(storedHash))
            {
                return false; // Cannot verify if either is empty
            }
            // Hash the entered password using the same method and compare.
            return HashPassword(enteredPassword) == storedHash;
        }
    }
}