// FILE: ViewModels/ChangePasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)] // Changed minimum length to 8 for better security
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        // ======================================================================
        // THE FIX: Renamed from 'ConfirmNewPassword' to 'ConfirmPassword'
        // This makes it match the property name used in the Controller and View.
        // ======================================================================
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}