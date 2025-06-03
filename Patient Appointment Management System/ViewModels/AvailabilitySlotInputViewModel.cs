using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AvailabilitySlotInputViewModel
    {
        [Required(ErrorMessage = "Please select a date.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Please select a start time.")]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Please select an end time.")]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }
    }
}