// FILE: ViewModels/DoctorProfileViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorProfileViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string Specialization { get; set; }

        [Display(Name = "Country Code")]
        public string? CountryCode { get; set; }

        // To: ViewModels/DoctorProfileViewModel.cs

        [Display(Name = "Phone Number")]
        // We've removed the strict validation to allow empty phone numbers.
        public string? PhoneNumber { get; set; }

        // This property will hold the model for the password change form
        public ChangePasswordViewModel ChangePassword { get; set; }

        public DoctorProfileViewModel()
        {
            // Initialize the nested view model
            ChangePassword = new ChangePasswordViewModel();
        }
    }
}