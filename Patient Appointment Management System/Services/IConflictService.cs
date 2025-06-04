using Patient_Appointment_Management_System.ViewModels; // For ConflictViewModel
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public interface IConflictService
    {
        Task<IEnumerable<ConflictViewModel>> GetActiveConflictsAsync(int count = 5);
        Task<bool> ResolveConflictByCancellingAppointmentAsync(int appointmentId);
    }
}