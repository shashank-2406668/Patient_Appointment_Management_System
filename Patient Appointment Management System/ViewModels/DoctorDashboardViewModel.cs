// File: Patient_Appointment_Management_System/ViewModels/DoctorDashboardViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorDashboardViewModel
    {
        [Display(Name = "Doctor")]
        public string DoctorDisplayName { get; set; } = string.Empty;

        public List<AppointmentSummaryViewModel> TodaysAppointments { get; set; }

        public List<NotificationViewModel> Notifications { get; set; }

        public int UnreadNotificationCount { get; set; }

        public DoctorDashboardViewModel()
        {
            TodaysAppointments = new List<AppointmentSummaryViewModel>();
            Notifications = new List<NotificationViewModel>();
        }
    }
}