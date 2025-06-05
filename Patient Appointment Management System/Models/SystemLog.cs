// File: Models/SystemLog.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.Models
{
    public enum LogLevel
    {
        Information,
        Warning,
        Error,
        Critical
    }

    public class SystemLog
    {
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [StringLength(50)]
        public string Level { get; set; }

        [StringLength(100)]
        public string? Source { get; set; } // Made Source nullable too, often useful

        [Required]
        public string Message { get; set; }

        [StringLength(100)] // Added StringLength for EventType
        public string? EventType { get; set; } // Made EventType nullable

        // --- THIS IS THE KEY CHANGE ---
        [StringLength(450)] // Max length for typical user IDs (e.g., GUIDs from ASP.NET Identity)
        public string? UserId { get; set; } // Made UserId nullable (string? or just string if not using C# 8+ NRTs)

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] ({Source ?? "System"}): {Message}";
        }
    }
}