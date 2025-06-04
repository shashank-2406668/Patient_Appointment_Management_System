using Patient_Appointment_Management_System.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public interface IAdminService
    {
        Task<bool> AddAdminAsync(Admin admin, string plainPassword);
        Task<Admin> GetAdminByIdAsync(int adminId);
        Task<Admin> GetAdminByEmailAsync(string email);
        Task<IEnumerable<Admin>> GetAllAdminsAsync();
        Task<bool> UpdateAdminAsync(Admin admin); // If you need to update details later
        Task<bool> DeleteAdminAsync(int adminId); // If you need to delete admins
        // For viewSystemLogs, we'll handle logging separately for now.
    }
}