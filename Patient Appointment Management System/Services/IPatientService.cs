using Patient_Appointment_Management_System.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public interface IPatientService
    {
        Task<int> GetTotalPatientsCountAsync();
        Task<IEnumerable<Patient>> GetRecentPatientsAsync(int count = 5);
        Task<IEnumerable<Patient>> GetAllPatientsAsync();
        Task<Patient> GetPatientByIdAsync(int id); // Add this
        Task<bool> UpdatePatientAsync(Patient patient); // Add this
        Task<bool> DeletePatientAsync(int id); // Add this
    }
}