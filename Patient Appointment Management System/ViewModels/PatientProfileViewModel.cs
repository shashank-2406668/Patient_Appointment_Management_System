// ViewModels/PatientProfileViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class PatientProfileViewModel
    {
        // --- THIS IS THE CRITICAL ADDITION ---
        public int Id { get; set; }
        // -------------------------------------

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty; // Initialize

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty; // Initialize

        [StringLength(20)] // To accommodate country code + number
        [Phone(ErrorMessage = "Invalid Phone Number format.")] // This [Phone] attribute is fine
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty; // Initialize, even if form makes it optional, ViewModel can default

        [Required(ErrorMessage = "Date of Birth is required.")] // If editable and required
        [DataType(DataType.Date)]
        public DateTime? Dob { get; set; } // <<<< MAKE THIS NON-NULLABLE if it's directly mapped

        [StringLength(500)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty; // Initialize
    }
}