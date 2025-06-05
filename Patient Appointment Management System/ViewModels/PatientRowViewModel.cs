// File: ViewModels/PatientRowViewModel.cs
namespace Patient_Appointment_Management_System.ViewModels
{
    public class PatientRowViewModel
    {
        public int PatientId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        // Optional: If you implement activity tracking.
        // For simplicity, you might get this from the Patient model's LastLoginTime or similar.
        public string LastActivity { get; set; } = "N/A"; // Default placeholder
    }
}