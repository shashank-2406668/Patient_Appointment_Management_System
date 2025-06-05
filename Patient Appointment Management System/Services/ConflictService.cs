// File: Services/ConflictService.cs
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
        private static int _tempConflictIdCounter = 0;

        public ConflictService(PatientAppointmentDbContext context)
        {
            _context = context;
        }

        public async Task<List<SchedulingConflictViewModel>> GetActiveConflictsAsync(int maxCount = 0)
        {
            // Step 1: Identify DoctorId and AppointmentDateTime combinations that have more than one appointment (potential conflicts)
            var conflictingGroupKeys = await _context.Appointments
                // .Where(a => a.Status == "Scheduled") // Add if you have a status to filter by
                .GroupBy(a => new { a.DoctorId, a.AppointmentDateTime })
                .Where(g => g.Count() > 1) // Filter groups that have more than one appointment
                .Select(g => g.Key) // Select only the DoctorId and AppointmentDateTime
                .ToListAsync();

            if (!conflictingGroupKeys.Any())
            {
                return new List<SchedulingConflictViewModel>(); // No conflicts found
            }

            // Step 2: Fetch all appointments that match these conflicting group keys
            // We build a list of expressions to use in an OR condition.
            // This is a bit more complex to build dynamically but can be efficient.
            // A simpler, though potentially less performant for many keys, would be multiple .Where clauses or iterating and querying.

            // Let's fetch appointments that fall into these conflicting slots.
            // We'll do the final pairing in memory to avoid overly complex EF translation.
            var potentiallyConflictingAppointments = new List<Appointment>();
            foreach (var key in conflictingGroupKeys)
            {
                var appointmentsInSlot = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == key.DoctorId && a.AppointmentDateTime == key.AppointmentDateTime)
                    // .Where(a => a.Status == "Scheduled") // Re-apply status filter if needed
                    .ToListAsync();
                potentiallyConflictingAppointments.AddRange(appointmentsInSlot);
            }

            // Ensure we only have unique appointments if a slot had more than 2 (though GroupBy above should handle this for pairs)
            // Grouping again in memory by Doctor and Time ensures we process actual conflict sets
            var inMemoryConflictGroups = potentiallyConflictingAppointments
                .GroupBy(a => new {
                    a.DoctorId,
                    DoctorName = a.Doctor?.Name ?? "Unknown Doctor", // Use ?. for safety
                    a.AppointmentDateTime
                })
                .Where(g => g.Count() > 1); // Re-affirm it's a conflict group

            var conflicts = new List<SchedulingConflictViewModel>();
            var processedAppointmentPairs = new HashSet<string>();

            foreach (var group in inMemoryConflictGroups)
            {
                var appointmentsInGroup = group.ToList();
                for (int i = 0; i < appointmentsInGroup.Count; i++)
                {
                    for (int j = i + 1; j < appointmentsInGroup.Count; j++)
                    {
                        var appt1 = appointmentsInGroup[i];
                        var appt2 = appointmentsInGroup[j];

                        string pairKey = appt1.AppointmentId < appt2.AppointmentId ?
                                         $"{appt1.AppointmentId}-{appt2.AppointmentId}" :
                                         $"{appt2.AppointmentId}-{appt1.AppointmentId}";

                        if (processedAppointmentPairs.Contains(pairKey)) continue;
                        processedAppointmentPairs.Add(pairKey);

                        _tempConflictIdCounter++;

                        conflicts.Add(new SchedulingConflictViewModel
                        {
                            ConflictId = _tempConflictIdCounter,
                            DoctorName = group.Key.DoctorName,
                            ConflictTime = group.Key.AppointmentDateTime,
                            Appointment1Id = appt1.AppointmentId,
                            Patient1Name = appt1.Patient?.Name ?? "Unknown Patient",
                            Appointment1Details = $"Appt. {appt1.AppointmentId} for {(appt1.Patient?.Name ?? "Unknown Patient")}",
                            Appointment2Id = appt2.AppointmentId,
                            Patient2Name = appt2.Patient?.Name ?? "Unknown Patient",
                            Appointment2Details = $"Appt. {appt2.AppointmentId} for {(appt2.Patient?.Name ?? "Unknown Patient")}",
                            ConflictDetails = $"Doctor {group.Key.DoctorName} is double booked at this time.",
                            Message = $"Scheduling conflict detected for Dr. {group.Key.DoctorName} at {group.Key.AppointmentDateTime.ToShortTimeString()}"
                        });
                    }
                }
            }

            var orderedConflicts = conflicts.OrderByDescending(c => c.ConflictTime).ThenBy(c => c.DoctorName);
            return maxCount > 0 ? orderedConflicts.Take(maxCount).ToList() : orderedConflicts.ToList();
        }

        public async Task<bool> ResolveConflictByCancellingAppointmentAsync(int appointmentId)
        {
            if (appointmentId <= 0) return false;

            var appointmentToCancel = await _context.Appointments
                                                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
            if (appointmentToCancel == null) return false;

            _context.Appointments.Remove(appointmentToCancel);
            try
            {
                return await _context.SaveChangesAsync() > 0;
            }
            catch (DbUpdateException) { return false; }
        }
    }
}