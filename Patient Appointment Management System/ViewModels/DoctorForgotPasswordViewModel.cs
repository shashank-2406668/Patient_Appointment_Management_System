// File: Patient_Appointment_Management_System/ViewModels/DoctorForgotPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }
}