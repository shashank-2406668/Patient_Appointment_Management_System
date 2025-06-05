using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class RescheduleAppointmentViewModel
    {
        public int AppointmentId { get; set; }

        // Current appointment info (display only)
        public int CurrentDoctorId { get; set; }
        public string CurrentDoctorName { get; set; } = string.Empty;
        public DateTime CurrentAppointmentDateTime { get; set; }

        // New appointment details
        [Required(ErrorMessage = "Please select a doctor.")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Please select an appointment date.")]
        [Display(Name = "Appointment Date")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please select a time slot.")]
        [Display(Name = "Time Slot")]
        public int SelectedAvailabilitySlotId { get; set; }

        [Display(Name = "Reason for Visit")]
        [StringLength(500, ErrorMessage = "Issue description cannot exceed 500 characters.")]
        public string? Issue { get; set; }

        // For dropdowns
        public List<SelectListItem> DoctorsList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableTimeSlots { get; set; } = new List<SelectListItem>();
    }
}