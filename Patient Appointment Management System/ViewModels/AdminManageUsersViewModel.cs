// In: ViewModels/AdminManageUsersViewModel.cs

using System.Collections.Generic;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AdminManageUsersViewModel
    {
        // You already have these two properties
        public List<AdminDisplayViewModel> Admins { get; set; }
        public List<DoctorRowViewModel> Doctors { get; set; }

        // ==========================================================
        //         *** ADD THIS NEW PROPERTY FOR PATIENTS ***
        // ==========================================================
        public List<PatientRowViewModel> Patients { get; set; }

        // Constructor to initialize lists and prevent null reference errors
        public AdminManageUsersViewModel()
        {
            Admins = new List<AdminDisplayViewModel>();
            Doctors = new List<DoctorRowViewModel>();
            Patients = new List<PatientRowViewModel>(); // Also initialize the new list
        }
    }
}