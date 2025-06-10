// File: Services/IDoctorService.cs

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
        Task<bool> UpdateDoctorProfileAsync(Doctor doctor);
        Task<IEnumerable<Doctor>> GetAllDoctorsAsync();
    }
}