// File: Patient_Appointment_Management_System/ViewModels/DoctorProfileViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorProfileViewModel
    {
        // This ID is crucial for identifying the doctor on POST to update the correct record.
        // It should be included as a hidden field in the form.
        public int Id { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } // Typically, email is not editable after registration or is a unique identifier

        [Required(ErrorMessage = "Specialization is required.")]
        [Display(Name = "Specialization")] // Using "Specialization" as per your ViewModel
        public string Specialization { get; set; }

        // For "Doctor ID" display, if it's different from the database 'Id'
        // If Model.Id is the Doctor ID you want to display, you can just use that.
        // Otherwise, add a string property like: public string DoctorDisplayId { get; set; }
        // For now, we'll assume Model.Id is sufficient for identification and display if needed.

        [Display(Name = "Country Code")]
        public string? CountryCode { get; set; } // e.g., "+91", "+1"

        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits if provided.")]
        public string? PhoneNumber { get; set; } // Just the 10-digit number part

        [Display(Name = "Biography / About")]
        [DataType(DataType.MultilineText)]
        public string? Bio { get; set; }

        [Display(Name = "Years of Experience")]
        [Range(0, 70, ErrorMessage = "Years of experience must be between 0 and 70.")]
        public int? YearsOfExperience { get; set; }

        [Display(Name = "Address")]
        [DataType(DataType.MultilineText)]
        public string? Address { get; set; }
    }
}