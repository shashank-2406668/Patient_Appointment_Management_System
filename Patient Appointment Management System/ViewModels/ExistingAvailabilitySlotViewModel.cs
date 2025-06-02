// File: Patient_Appointment_Management_System/ViewModels/ExistingAvailabilitySlotViewModel.cs
using System;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class ExistingAvailabilitySlotViewModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; }
        public string? PatientNameIfBooked { get; set; } // Optional
    }
}