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
        public PatientService(PatientAppointmentDbContext context) { _context = context; }

        public async Task<int> GetTotalPatientsCountAsync() => await _context.Patients.CountAsync();
        public async Task<IEnumerable<Patient>> GetRecentPatientsAsync(int count = 5) => await _context.Patients.OrderByDescending(p => p.PatientId).Take(count).ToListAsync();
        public async Task<IEnumerable<Patient>> GetAllPatientsAsync() => await _context.Patients.OrderBy(p => p.Name).ToListAsync();
        public async Task<Patient> GetPatientByIdAsync(int id) => await _context.Patients.FindAsync(id);

        // =================================================================
        //                 *** THE EDIT FIX IS HERE ***
        // =================================================================
        public async Task<bool> UpdatePatientAsync(Patient patient)
        {
            // This line tells the DbContext to start tracking the entity
            // and marks its state as 'Modified'. Without this, SaveChangesAsync()
            // doesn't know what to update.
            _context.Update(patient);

            try
            {
                // This command will now find the 'Modified' entity and save it.
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // In a real app, log this exception
                return false;
            }
        }

        public async Task<bool> DeletePatientAsync(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return false;

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}