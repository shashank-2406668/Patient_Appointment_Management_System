using System.Collections.Generic;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AdminManageUsersViewModel
    {
        public List<AdminDisplayViewModel> Admins { get; set; }
        // Later, you might add:
        // public List<DoctorDisplayViewModel> Doctors { get; set; }
        // public List<PatientDisplayViewModel> Patients { get; set; }

        public AdminManageUsersViewModel()
        {
            Admins = new List<AdminDisplayViewModel>();
        }
        public List<DoctorRowViewModel> Doctors { get; set; } = new List<DoctorRowViewModel>();
    }
}