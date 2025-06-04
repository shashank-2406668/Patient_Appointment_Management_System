using Patient_Appointment_Management_System.Models; // If returning list of Patient
using System.Collections.Generic; // For List
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public interface IPatientService
    {
        Task<int> GetTotalPatientsCountAsync();
        Task<IEnumerable<Patient>> GetRecentPatientsAsync(int count = 5); // Get top 5 recent
        // ... other patient-related methods
    }
}