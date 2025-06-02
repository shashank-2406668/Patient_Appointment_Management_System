// File: Patient_Appointment_Management_System/ViewModels/DoctorProfileViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorProfileViewModel
    {
        public int Id { get; set; } // Hidden field in form, used for POST

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Email Address (Read-only)")] // Typically not changed here
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required.")]
        [StringLength(100)]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country Code is required.")]
        [Display(Name = "Country Code")]
        public string CountryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number format.")]
        [StringLength(15, MinimumLength = 7, ErrorMessage = "Phone number must be between 7 and 15 digits.")]
        [RegularExpression(@"^\d{7,15}$", ErrorMessage = "Phone number must contain only digits.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Bio { get; set; }

        [Range(0, 70, ErrorMessage = "Years of experience must be between 0 and 70.")]
        [Display(Name = "Years of Experience")]
        public int? YearsOfExperience { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }
    }
}