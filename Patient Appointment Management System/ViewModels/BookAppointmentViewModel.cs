using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem

namespace Patient_Appointment_Management_System.ViewModels
{
    public class BookAppointmentViewModel
    {
        [Required(ErrorMessage = "Please select a doctor.")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Please select an appointment date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; } = DateTime.Today; // Default to today

        // This will now hold the selected AvailabilitySlotId
        [Required(ErrorMessage = "Please select an available time slot.")]
        [Display(Name = "Available Time Slot")]
        public int SelectedAvailabilitySlotId { get; set; } // Changed from string to int

        [Required(ErrorMessage = "Please describe your medical issue.")]
        [StringLength(500, ErrorMessage = "Issue description cannot exceed 500 characters.")]
        [Display(Name = "Brief Description of Issue")]
        public string Issue { get; set; } = string.Empty;

        // For populating the Doctor dropdown
        public IEnumerable<SelectListItem>? DoctorsList { get; set; }

        // This will be populated by AJAX based on Doctor and Date selection
        // It will contain SelectListItems where Value is AvailabilitySlotId and Text is the time range
        public List<SelectListItem> AvailableTimeSlots { get; set; } = new List<SelectListItem>();

        // public BookAppointmentViewModel() // Constructor already defaults AppointmentDate
        // {
        //     // AppointmentDate = DateTime.Today; // Default to today
        // }
    }
}