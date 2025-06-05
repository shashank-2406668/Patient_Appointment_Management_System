// File: ViewModels/AdminDashboardViewModel.cs
using Patient_Appointment_Management_System.Models; // For SystemLog
using System.Collections.Generic;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }

        // These ViewModels (PatientRowViewModel, DoctorRowViewModel) are now defined in their own files
        // within the Patient_Appointment_Management_System.ViewModels namespace.
        public IEnumerable<PatientRowViewModel> RecentPatients { get; set; }
        public IEnumerable<DoctorRowViewModel> RecentDoctors { get; set; }

        public List<SystemLog> RecentSystemLogs { get; set; }
        public List<SchedulingConflictViewModel> ActiveConflicts { get; set; }

        public AdminDashboardViewModel()
        {
            RecentPatients = new List<PatientRowViewModel>();
            RecentDoctors = new List<DoctorRowViewModel>();
            RecentSystemLogs = new List<SystemLog>();
            ActiveConflicts = new List<SchedulingConflictViewModel>();
        }
    }

    // REMOVE THE DEFINITIONS OF PatientRowViewModel and DoctorRowViewModel FROM HERE
    // AS THEY ARE NOW IN THEIR OWN FILES.
    //
    // public class PatientRowViewModel { ... } // DELETE THIS BLOCK
    //
    // public class DoctorRowViewModel { ... }  // DELETE THIS BLOCK
}