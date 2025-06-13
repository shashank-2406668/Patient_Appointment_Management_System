using Patient_Appointment_Management_System.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public interface IDoctorService
    {
        Task<int> GetTotalDoctorsCountAsync();
        Task<IEnumerable<Doctor>> GetRecentDoctorsAsync(int count = 5);
        Task<Doctor> GetDoctorByEmailAsync(string email);
        Task<bool> AddDoctorAsync(Doctor doctor, string password);
        Task<Doctor> ValidateDoctorCredentialsAsync(string email, string password);
        Task<Doctor> GetDoctorByIdAsync(int doctorId);
        Task<IEnumerable<Doctor>> GetAllDoctorsAsync();

        // This was for the Doctor's own profile page. We'll create a more generic one for the admin.
        Task<bool> UpdateDoctorProfileAsync(Doctor doctor);

        // =================================================================
        //     *** ADD THESE NEW METHOD SIGNATURES FOR ADMIN ACTIONS ***
        // =================================================================

        /// <summary>
        /// Updates a doctor's record from the admin panel.
        /// </summary>
        Task<bool> UpdateDoctorAsync(Doctor doctor);

        /// <summary>
        /// Deletes a doctor from the system by their ID.
        /// </summary>
        Task<bool> DeleteDoctorAsync(int doctorId);
    }
}