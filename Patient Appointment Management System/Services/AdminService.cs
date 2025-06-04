using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils; // Assuming PasswordHelper is here
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public class AdminService : IAdminService
    {
        private readonly PatientAppointmentDbContext _context;

        public AdminService(PatientAppointmentDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddAdminAsync(Admin admin, string plainPassword)
        {
            if (await _context.Admins.AnyAsync(a => a.Email == admin.Email))
            {
                // Consider throwing a custom exception or returning a more detailed result
                return false; // Email already exists
            }

            admin.PasswordHash = PasswordHelper.HashPassword(plainPassword); // Use your PasswordHelper
            _context.Admins.Add(admin);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Admin> GetAdminByIdAsync(int adminId)
        {
            return await _context.Admins.FindAsync(adminId);
        }

        public async Task<Admin> GetAdminByEmailAsync(string email)
        {
            return await _context.Admins.FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower());
        }

        public async Task<IEnumerable<Admin>> GetAllAdminsAsync()
        {
            return await _context.Admins.OrderBy(a => a.Name).ToListAsync();
        }

        public async Task<bool> UpdateAdminAsync(Admin admin)
        {
            var existingAdmin = await _context.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.AdminId == admin.AdminId);
            if (existingAdmin == null) return false;

            // Prevent changing the password hash directly through this method.
            // Have a separate method like ChangePasswordAsync if needed.
            admin.PasswordHash = existingAdmin.PasswordHash;

            _context.Admins.Update(admin);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAdminAsync(int adminId)
        {
            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null) return false;

            _context.Admins.Remove(admin);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}