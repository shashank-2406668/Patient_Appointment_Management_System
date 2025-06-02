// File: Patient_Appointment_Management_System/ViewModels/PatientDashboardViewModel.cs
using System;
using System.Collections.Generic;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class PatientDashboardViewModel
    {
        public string PatientName { get; set; } = string.Empty;
        public List<AppointmentDetailViewModel> UpcomingAppointments { get; set; } = new List<AppointmentDetailViewModel>();
        public List<AppointmentDetailViewModel> AppointmentHistory { get; set; } = new List<AppointmentDetailViewModel>();
    }

    public class AppointmentDetailViewModel // Reusable for displaying appointment info
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorSpecialization { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Issue { get; set; }
        // You can add more fields here if needed, e.g., ClinicAddress
    }
}