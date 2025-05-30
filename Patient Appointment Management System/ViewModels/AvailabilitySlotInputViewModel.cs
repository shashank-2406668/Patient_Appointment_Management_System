// File: Patient_Appointment_Management_System/ViewModels/AvailabilitySlotInputViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AvailabilitySlotInputViewModel
    {
        [Required(ErrorMessage = "Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Start Time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End Time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }
    }
}