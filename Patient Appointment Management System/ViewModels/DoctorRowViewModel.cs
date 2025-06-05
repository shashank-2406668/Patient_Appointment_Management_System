// File: ViewModels/DoctorRowViewModel.cs
namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorRowViewModel
    {
        public int DoctorId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Specialization { get; set; }

        // Optional: If you implement activity tracking.
        public string LastActivity { get; set; } = "N/A"; // Default placeholder
    }
}