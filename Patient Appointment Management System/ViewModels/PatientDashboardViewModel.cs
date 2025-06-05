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

        // Add these new properties
        public List<NotificationViewModel> Notifications { get; set; } = new List<NotificationViewModel>();
        public int UnreadNotificationCount { get; set; }
    }

    public class AppointmentDetailViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorSpecialization { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Issue { get; set; }
    }
}