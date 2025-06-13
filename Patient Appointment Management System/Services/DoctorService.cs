using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly PatientAppointmentDbContext _context;

        public DoctorService(PatientAppointmentDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddDoctorAsync(Doctor doctor, string password)
        {
            if (await _context.Doctors.AnyAsync(d => d.Email == doctor.Email))
            {
                return false; // Email already exists
            }
            doctor.PasswordHash = PasswordHelper.HashPassword(password);
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
        {
            return await _context.Doctors.OrderBy(d => d.Name).ToListAsync();
        }

        public async Task<Doctor> GetDoctorByEmailAsync(string email)
        {
            return await _context.Doctors.FirstOrDefaultAsync(d => d.Email == email);
        }

        public async Task<Doctor> GetDoctorByIdAsync(int doctorId)
        {
            return await _context.Doctors.FindAsync(doctorId);
        }

        public async Task<IEnumerable<Doctor>> GetRecentDoctorsAsync(int count = 5)
        {
            return await _context.Doctors.OrderByDescending(d => d.DoctorId).Take(count).ToListAsync();
        }

        public async Task<int> GetTotalDoctorsCountAsync()
        {
            return await _context.Doctors.CountAsync();
        }

        public async Task<bool> UpdateDoctorProfileAsync(Doctor doctor)
        {
            // This method is specifically for the doctor's own profile update
            var doctorToUpdate = await _context.Doctors.FindAsync(doctor.DoctorId);
            if (doctorToUpdate == null) return false;

            doctorToUpdate.Name = doctor.Name;
            doctorToUpdate.Specialization = doctor.Specialization;
            doctorToUpdate.Phone = doctor.Phone;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Doctor> ValidateDoctorCredentialsAsync(string email, string password)
        {
            var doctor = await GetDoctorByEmailAsync(email);
            if (doctor != null && PasswordHelper.VerifyPassword(password, doctor.PasswordHash))
            {
                return doctor;
            }
            return null;
        }

        // =================================================================
        //      *** THIS IS THE IMPLEMENTATION OF THE NEW METHODS ***
        // =================================================================

        public async Task<bool> UpdateDoctorAsync(Doctor doctor)
        {
            // This is a generic update method for the admin
            _context.Entry(doctor).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // In a real app, log this exception
                return false;
            }
        }

        public async Task<bool> DeleteDoctorAsync(int doctorId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
            {
                return false; // Doctor not found
            }

            // In a real-world scenario, you might want to handle appointments
            // of the doctor being deleted (e.g., cancel or reassign them).
            // For now, we are performing a direct delete.
            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}