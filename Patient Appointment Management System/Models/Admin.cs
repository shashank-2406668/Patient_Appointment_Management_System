// File: Patient_Appointment_Management_System/Models/Admin.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Appointment_Management_System.Models
{
    [Table("Admins")] // Pluralized table name by convention
    public class Admin
    {
        [Key]
        public int AdminId { get; set; }

        [StringLength(100)]
        public string? Name { get; set; } // Optional based on your model

        [Required(ErrorMessage = "Email is required for Admin.")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } // Required, so not nullable

        [Required(ErrorMessage = "Password hash is required for Admin.")]
        public string PasswordHash { get; set; } // Stores the HASHED password

        [StringLength(50)]
        public string? Role { get; set; } // Optional
    }
}