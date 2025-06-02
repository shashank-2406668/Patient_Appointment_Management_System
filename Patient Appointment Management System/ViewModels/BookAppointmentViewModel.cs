// File: Patient_Appointment_Management_System/ViewModels/BookAppointmentViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // Required for SelectList

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
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please select a time slot.")]
        [Display(Name = "Time Slot")]
        public string TimeSlot { get; set; } // e.g., "09:00 AM - 10:00 AM"

        [Required(ErrorMessage = "Please describe your medical issue.")]
        [StringLength(500, ErrorMessage = "Issue description cannot exceed 500 characters.")]
        [Display(Name = "Brief Description of Issue")]
        public string Issue { get; set; }

        // For populating the Doctor dropdown in the view
        public IEnumerable<SelectListItem>? DoctorsList { get; set; }

        // For populating time slots in the view
        // You can make this dynamic later based on doctor availability
        public List<string> AvailableTimeSlots { get; set; } = new List<string>
        {
            "09:00 AM - 10:00 AM",
            "10:00 AM - 11:00 AM",
            "12:00 PM - 01:00 PM", // Corrected to PM
            "03:00 PM - 04:00 PM",
            "04:00 PM - 05:00 PM"
        };

        public BookAppointmentViewModel()
        {
            // Set default appointment date to today to avoid issues with min attribute if model state is invalid
            AppointmentDate = DateTime.Today;
        }
    }
}