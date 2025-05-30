// ViewModels/PatientRegisterViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class PatientRegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country Code is required.")]
        [Display(Name = "Country Code")]
        public string CountryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits.")]
        [Display(Name = "Phone Number (10 digits)")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime Dob { get; set; } // Value type, no warning here typically

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@#$%^&*!]).{8,}$",
            ErrorMessage = "Password must include uppercase, lowercase, a number, and a special character (@#$%^&*!).")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}