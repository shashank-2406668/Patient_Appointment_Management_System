using Patient_Appointment_Management_System.Models; // If returning list of Doctor
using System.Collections.Generic; // For List
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public interface IDoctorService
    {
        Task<int> GetTotalDoctorsCountAsync();
        Task<IEnumerable<Doctor>> GetRecentDoctorsAsync(int count = 5);
        // ... other doctor-related methods
    }
}