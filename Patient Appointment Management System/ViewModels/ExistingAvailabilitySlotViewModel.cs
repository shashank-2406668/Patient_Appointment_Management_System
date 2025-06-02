// File: Patient_Appointment_Management_System/ViewModels/ExistingAvailabilitySlotViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class ExistingAvailabilitySlotViewModel
    {
        public int Id { get; set; } // AvailabilitySlotId

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime Date { get; set; }

        [DisplayFormat(DataFormatString = @"{0:hh\:mm tt}")]
        public TimeSpan StartTime { get; set; }

        [DisplayFormat(DataFormatString = @"{0:hh\:mm tt}")]
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; }
        public string? PatientNameIfBooked { get; set; } // To show which patient booked it
        public int? AppointmentIdIfBooked { get; set; } // To link to appointment details if needed
    }
}