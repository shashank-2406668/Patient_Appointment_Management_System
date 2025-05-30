// Patient_Appointment_Management_System/ViewModels/AdminViewModel.cs
// (You can also name this file AdminManagementViewModel.cs if you prefer)

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AdminUserViewModel // Represents a single admin user for display
    {
        public int AdminId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class AdminManagementViewModel // The main ViewModel for the Admin Management page
    {
        public List<AdminUserViewModel> Admins { get; set; }
        public List<string> SystemLogs { get; set; }

        public AdminManagementViewModel()
        {
            Admins = new List<AdminUserViewModel>();
            SystemLogs = new List<string>();
        }
    }

    public class AddAdminViewModel // ViewModel for the "Add Admin" modal form
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}