using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.Models
{
    public class Admin
    {
        [Key]
        public int AdminId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } // Will be unique

        [Required]
        public string PasswordHash { get; set; } // For storing the hashed password

        [StringLength(50)]
        public string Role { get; set; } = "Admin"; // Default role
    }
}