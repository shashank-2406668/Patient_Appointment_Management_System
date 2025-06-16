using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    // Service for admin-related database actions
    public class AdminService : IAdminService
    {
        private readonly PatientAppointmentDbContext _context;
        public AdminService(PatientAppointmentDbContext context) { _context = context; }

        // Add a new admin if email is not taken
        public async Task<bool> AddAdminAsync(Admin admin, string plainPassword)
        {
            if (await _context.Admins.AnyAsync(a => a.Email == admin.Email)) return false;
            admin.PasswordHash = PasswordHelper.HashPassword(plainPassword);
            _context.Admins.Add(admin);
            return await _context.SaveChangesAsync() > 0;
        }

        // Get admin by ID
        public async Task<Admin> GetAdminByIdAsync(int adminId)
        {
            return await _context.Admins.FindAsync(adminId);
        }

        // Get admin by email
        public async Task<Admin> GetAdminByEmailAsync(string email)
        {
            return await _context.Admins.FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower());
        }

        // Get all admins
        public async Task<IEnumerable<Admin>> GetAllAdminsAsync()
        {
            return await _context.Admins.OrderBy(a => a.Name).ToListAsync();
        }

        // Update admin info (except password)
        public async Task<bool> UpdateAdminAsync(Admin admin)
        {
            var old = await _context.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.AdminId == admin.AdminId);
            if (old == null) return false;
            admin.PasswordHash = old.PasswordHash; // Don't change password here
            _context.Admins.Update(admin);
            return await _context.SaveChangesAsync() > 0;
        }

        // Delete admin by ID
        public async Task<bool> DeleteAdminAsync(int adminId)
        {
            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null) return false;
            _context.Admins.Remove(admin);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
