using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public class PatientService : IPatientService
    {
        private readonly PatientAppointmentDbContext _context;

        public PatientService(PatientAppointmentDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotalPatientsCountAsync()
        {
            return await _context.Patients.CountAsync();
        }

        public async Task<IEnumerable<Patient>> GetRecentPatientsAsync(int count = 5)
        {
            // Define "recent" as per your needs, e.g., by PatientId (if auto-incrementing) or a CreatedDate field
            // Assuming PatientId is auto-incrementing and higher IDs are more recent
            return await _context.Patients
                                 .OrderByDescending(p => p.PatientId) // Or OrderByDescending(p => p.RegistrationDate)
                                 .Take(count)
                                 .ToListAsync();
        }
        // ... other patient-related methods
    }
}