// File: Patient_Appointment_Management_System/ViewModels/AppointmentSummaryViewModel.cs
using System;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AppointmentSummaryViewModel
    {
        public int Id { get; set; }  // This was missing
        public string PatientName { get; set; } = string.Empty;
        public DateTime AppointmentDateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Issue { get; set; }  // This was missing
    }
}