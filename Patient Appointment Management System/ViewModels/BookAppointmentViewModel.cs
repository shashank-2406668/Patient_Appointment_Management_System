// File: Patient_Appointment_Management_System/ViewModels/BookAppointmentViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        [Display(Name = "Available Time Slot")]
        public string SelectedAvailabilitySlotId { get; set; } = string.Empty; // Will hold "AvailabilitySlotId" as value

        [Required(ErrorMessage = "Please describe your medical issue.")]
        [StringLength(500, ErrorMessage = "Issue description cannot exceed 500 characters.")]
        [Display(Name = "Brief Description of Issue")]
        public string Issue { get; set; } = string.Empty;

        public IEnumerable<SelectListItem>? DoctorsList { get; set; }
        public List<SelectListItem> AvailableTimeSlots { get; set; } = new List<SelectListItem>(); // Will be list of SelectListItems (Text: "TimeRange", Value: "SlotId")

        public BookAppointmentViewModel()
        {
            AppointmentDate = DateTime.Today;
        }
    }
}