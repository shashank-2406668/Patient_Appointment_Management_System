// File: Models/SystemLog.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.Models
{
    public enum LogLevel // You might have this enum defined elsewhere or directly use strings
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
        public string Level { get; set; } // Or use LogLevel enum: public LogLevel Level { get; set; }

        [StringLength(100)]
        public string Source { get; set; } // e.g., ControllerName, ServiceName

        [Required]
        public string Message { get; set; }

        public string EventType { get; set; } // Optional: e.g., "LoginSuccess", "AppointmentCreated"

        public string UserId { get; set; } // Optional: For tracking user associated with the log

        // Optional: For displaying nicely in views if you just do @log
        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] ({Source ?? "System"}): {Message}";
        }
    }
}