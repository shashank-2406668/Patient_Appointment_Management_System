// File: Patient_Appointment_Management_System/Models/Notification.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Appointment_Management_System.Models
{
    [Table("Notifications")] // Pluralized table name
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        // Specific recipient Foreign Keys (only one should be populated per notification)
        public int? PatientId { get; set; }
        public int? DoctorId { get; set; }
        public int? AdminId { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        [StringLength(50)] // Increased length
        public string NotificationType { get; set; } // e.g., "AppointmentConfirmation", "PasswordReset", "NewMessage"

        public DateTime SentDate { get; set; } = DateTime.UtcNow; // Default to current UTC time

        public bool IsRead { get; set; } = false;

        [StringLength(255)]
        public string? Url { get; set; } // Optional URL to navigate to from the notification

        // --- Navigation properties (optional but good for querying) ---
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }

        [ForeignKey("AdminId")]
        public virtual Admin? Admin { get; set; }
    }
}