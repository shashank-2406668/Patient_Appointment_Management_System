using System.ComponentModel.DataAnnotations; // Required for data annotations like [Required]

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Display(Name = "Email Address")] // How it will appear in <label asp-for="Email">
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)] // This hints to the view engine to render as a password input
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}