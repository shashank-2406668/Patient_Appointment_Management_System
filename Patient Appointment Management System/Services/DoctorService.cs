// File: Services/DoctorService.cs

using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    // The class definition was missing. All the logic must go inside this class.
    // It must also implement the IDoctorService interface.
    public class DoctorService : IDoctorService
    {
        private readonly PatientAppointmentDbContext _context;

        public DoctorService(PatientAppointmentDbContext context)
        {
            _context = context;
        }

        // === Methods from your original implementation ===
        public async Task<int> GetTotalDoctorsCountAsync()
        {
            return await _context.Doctors.CountAsync();
        }

        public async Task<IEnumerable<Doctor>> GetRecentDoctorsAsync(int count = 5)
        {
            return await _context.Doctors
                                 .OrderByDescending(d => d.DoctorId)
                                 .Take(count)
                                 .ToListAsync();
        }

        // === NEW METHOD IMPLEMENTATIONS ===

        public async Task<Doctor> GetDoctorByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            return await _context.Doctors.FirstOrDefaultAsync(d => d.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> AddDoctorAsync(Doctor doctor, string password)
        {
            if (doctor == null || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }
            // Hash the password before saving
            doctor.PasswordHash = PasswordHelper.HashPassword(password);
            await _context.Doctors.AddAsync(doctor);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Doctor> ValidateDoctorCredentialsAsync(string email, string password)
        {
            var doctorUser = await GetDoctorByEmailAsync(email);
            if (doctorUser != null && PasswordHelper.VerifyPassword(password, doctorUser.PasswordHash))
            {
                return doctorUser;
            }
            return null;
        }

        public async Task<Doctor> GetDoctorByIdAsync(int doctorId)
        {
            return await _context.Doctors.FindAsync(doctorId);
        }

        public async Task<bool> UpdateDoctorProfileAsync(Doctor doctor)
        {
            try
            {
                _context.Doctors.Update(doctor);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException) // Catches issues like unique constraint violations
            {
                // In a real app, you might log this exception
                return false;
            }
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
        {
            return await _context.Doctors.OrderBy(d => d.Name).ToListAsync();
        }
    }
}