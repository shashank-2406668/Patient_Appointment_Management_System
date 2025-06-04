using Patient_Appointment_Management_System.Models; // For Patient, Doctor if showing details
using System.Collections.Generic;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public IEnumerable<PatientRowViewModel> RecentPatients { get; set; } // Or full Patient model
        public IEnumerable<DoctorRowViewModel> RecentDoctors { get; set; }   // Or full Doctor model
        public IEnumerable<string> RecentSystemLogs { get; set; }
        public IEnumerable<ConflictViewModel> ActiveConflicts { get; set; }

        public AdminDashboardViewModel()
        {
            RecentPatients = new List<PatientRowViewModel>();
            RecentDoctors = new List<DoctorRowViewModel>();
            RecentSystemLogs = new List<string>();
            ActiveConflicts = new List<ConflictViewModel>();
        }
    }

    // Simplified ViewModels for table rows to avoid sending full models if not needed
    public class PatientRowViewModel
    {
        public int PatientId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        // Add Login/Logout times if you implement activity tracking
        public string LastActivity { get; set; } = "N/A"; // Placeholder
    }

    public class DoctorRowViewModel
    {
        public int DoctorId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Specialization { get; set; }
        public string LastActivity { get; set; } = "N/A"; // Placeholder
    }
}