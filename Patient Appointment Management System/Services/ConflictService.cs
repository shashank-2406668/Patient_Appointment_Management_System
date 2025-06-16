using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    // Service to find and resolve appointment conflicts
    public class ConflictService : IConflictService
    {
        private readonly PatientAppointmentDbContext _context;
        private static int _conflictId = 0;

        public ConflictService(PatientAppointmentDbContext context)
        {
            _context = context;
        }

        // Find all scheduling conflicts (doctor double-booked)
        public async Task<List<SchedulingConflictViewModel>> GetActiveConflictsAsync(int maxCount = 0)
        {
            // Find all (DoctorId, Time) pairs with more than one appointment
            var conflictKeys = await _context.Appointments
                .GroupBy(a => new { a.DoctorId, a.AppointmentDateTime })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToListAsync();

            var conflicts = new List<SchedulingConflictViewModel>();
            var seenPairs = new HashSet<string>();

            foreach (var key in conflictKeys)
            {
                // Get all appointments for this doctor and time
                var appts = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == key.DoctorId && a.AppointmentDateTime == key.AppointmentDateTime)
                    .ToListAsync();

                // Compare each pair in the group
                for (int i = 0; i < appts.Count; i++)
                {
                    for (int j = i + 1; j < appts.Count; j++)
                    {
                        var a1 = appts[i];
                        var a2 = appts[j];
                        string pairKey = a1.AppointmentId < a2.AppointmentId
                            ? $"{a1.AppointmentId}-{a2.AppointmentId}"
                            : $"{a2.AppointmentId}-{a1.AppointmentId}";
                        if (seenPairs.Contains(pairKey)) continue;
                        seenPairs.Add(pairKey);

                        _conflictId++;
                        conflicts.Add(new SchedulingConflictViewModel
                        {
                            ConflictId = _conflictId,
                            DoctorName = a1.Doctor?.Name ?? "Unknown Doctor",
                            ConflictTime = a1.AppointmentDateTime,
                            Appointment1Id = a1.AppointmentId,
                            Patient1Name = a1.Patient?.Name ?? "Unknown Patient",
                            Appointment1Details = $"Appt. {a1.AppointmentId} for {a1.Patient?.Name ?? "Unknown"}",
                            Appointment2Id = a2.AppointmentId,
                            Patient2Name = a2.Patient?.Name ?? "Unknown Patient",
                            Appointment2Details = $"Appt. {a2.AppointmentId} for {a2.Patient?.Name ?? "Unknown"}",
                            ConflictDetails = $"Doctor {a1.Doctor?.Name ?? "Unknown"} is double booked.",
                            Message = $"Conflict for Dr. {a1.Doctor?.Name ?? "Unknown"} at {a1.AppointmentDateTime:t}"
                        });
                    }
                }
            }

            // Return all or just the first maxCount conflicts
            var ordered = conflicts.OrderByDescending(c => c.ConflictTime).ThenBy(c => c.DoctorName);
            return maxCount > 0 ? ordered.Take(maxCount).ToList() : ordered.ToList();
        }

        // Cancel an appointment to resolve a conflict
        public async Task<bool> ResolveConflictByCancellingAppointmentAsync(int appointmentId)
        {
            if (appointmentId <= 0) return false;
            var appt = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
            if (appt == null) return false;
            _context.Appointments.Remove(appt);
            try { return await _context.SaveChangesAsync() > 0; }
            catch { return false; }
        }
    }
}
