using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public interface ISystemLogService
    {
        Task<IEnumerable<string>> GetRecentLogsAsync(int count = 10);
    }
}