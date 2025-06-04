using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
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

        public async Task<int> GetTotalDoctorsCountAsync()
        {
            return await _context.Doctors.CountAsync();
        }

        public async Task<IEnumerable<Doctor>> GetRecentDoctorsAsync(int count = 5)
        {
            return await _context.Doctors
                                 .OrderByDescending(d => d.DoctorId) // Or OrderByDescending(d => d.RegistrationDate)
                                 .Take(count)
                                 .ToListAsync();
        }
        // ... other doctor-related methods
    }
}