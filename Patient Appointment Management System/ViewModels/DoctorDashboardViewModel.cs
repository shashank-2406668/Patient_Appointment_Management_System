// ViewModels/DoctorDashboardViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorDashboardViewModel
    {
        [Display(Name = "Doctor")]
        public string DoctorDisplayName { get; set; } = string.Empty; // Initialize

        public List<AppointmentSummaryViewModel> TodaysAppointments { get; set; }

        public List<string> Notifications { get; set; }

        public DoctorDashboardViewModel()
        {
            TodaysAppointments = new List<AppointmentSummaryViewModel>();
            Notifications = new List<string>();
        }
    }
}