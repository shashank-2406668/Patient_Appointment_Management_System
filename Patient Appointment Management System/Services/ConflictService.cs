using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public class ConflictService : IConflictService
    {
        private readonly PatientAppointmentDbContext _context;

        public ConflictService(PatientAppointmentDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ConflictViewModel>> GetActiveConflictsAsync(int count = 5)
        {
            var conflicts = new List<ConflictViewModel>();
            var today = DateTime.Today;

            // Example: Detect double bookings for doctors for upcoming appointments
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => a.AppointmentDateTime >= today && (a.Status == "Scheduled" || a.Status == "Confirmed"))
                .OrderBy(a => a.DoctorId)
                .ThenBy(a => a.AppointmentDateTime)
                .ToListAsync();

            for (int i = 0; i < upcomingAppointments.Count - 1; i++)
            {
                var app1 = upcomingAppointments[i];
                for (int j = i + 1; j < upcomingAppointments.Count; j++)
                {
                    var app2 = upcomingAppointments[j];

                    if (app1.DoctorId == app2.DoctorId)
                    {
                        // Simple overlap check (assumes appointments have a duration, e.g., 30 mins)
                        // This needs a more robust duration/end time logic for appointments
                        var app1EndTime = app1.AppointmentDateTime.AddMinutes(30); // Assuming 30 min duration
                        var app2EndTime = app2.AppointmentDateTime.AddMinutes(30);

                        bool timeOverlap = app1.AppointmentDateTime < app2EndTime && app2.AppointmentDateTime < app1EndTime;

                        if (timeOverlap && app1.AppointmentId != app2.AppointmentId)
                        {
                            conflicts.Add(new ConflictViewModel
                            {
                                Appointment1Id = app1.AppointmentId,
                                Patient1Name = app1.Patient?.Name ?? "N/A",
                                Appointment2Id = app2.AppointmentId,
                                Patient2Name = app2.Patient?.Name ?? "N/A",
                                DoctorName = app1.Doctor?.Name ?? "N/A",
                                ConflictTime = app1.AppointmentDateTime > app2.AppointmentDateTime ? app1.AppointmentDateTime : app2.AppointmentDateTime,
                                ConflictDetails = $"Double booking: Appt {app1.AppointmentId} ({app1.Patient?.Name}) and Appt {app2.AppointmentId} ({app2.Patient?.Name}) with {app1.Doctor?.Name} around {app1.AppointmentDateTime:g}.",
                                ResolutionSuggestion = "Cancel one appointment or reschedule."
                            });
                            // To avoid reporting the same pair multiple times in this simple loop, we might break or mark as processed
                            // For this example, it might report reciprocal conflicts.
                        }
                    }
                    else
                    {
                        // If sorted by DoctorId, once DoctorId changes, no more overlaps for app1 with subsequent appts
                        break;
                    }
                }
                if (conflicts.Count >= count) break; // Limit results
            }
            return conflicts.Take(count);
        }

        public async Task<bool> ResolveConflictByCancellingAppointmentAsync(int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) return false;

            appointment.Status = "Cancelled (Conflict)"; // Or just "Cancelled"
            if (appointment.BookedAvailabilitySlotId.HasValue)
            {
                var slot = await _context.AvailabilitySlots.FindAsync(appointment.BookedAvailabilitySlotId.Value);
                if (slot != null)
                {
                    slot.IsBooked = false; // Free up the slot
                }
            }
            // SystemLogService.AddLog($"Appointment {appointmentId} cancelled by admin due to conflict.");
            return await _context.SaveChangesAsync() > 0;
        }
    }
}