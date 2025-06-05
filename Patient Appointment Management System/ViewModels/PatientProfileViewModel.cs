// FILE: ViewModels/PatientProfileViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class PatientProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [StringLength(100, ErrorMessage = "Email Address cannot exceed 100 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone Number cannot exceed 20 characters.")]
        [Phone(ErrorMessage = "Invalid Phone Number format.")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? Dob { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        [Display(Name = "Address")]
        public string? Address { get; set; }
    }
}