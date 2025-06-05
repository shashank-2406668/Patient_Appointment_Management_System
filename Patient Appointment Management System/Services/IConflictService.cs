// File: Services/IConflictService.cs
using Patient_Appointment_Management_System.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public interface IConflictService
    {
        // Ensure this signature matches
        Task<List<SchedulingConflictViewModel>> GetActiveConflictsAsync(int maxCount = 0);

        // Add your ResolveConflictByCancellingAppointmentAsync method here if not already present
        Task<bool> ResolveConflictByCancellingAppointmentAsync(int appointmentId);
    }
}