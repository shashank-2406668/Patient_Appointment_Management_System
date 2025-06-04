using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System; // For DateTime

namespace Patient_Appointment_Management_System.Services
{
    public class SystemLogService : ISystemLogService
    {
        // In a real app, this would interact with a logging database or file system
        private static readonly List<string> _logs = new List<string>
        {
            $"{DateTime.Now.AddMinutes(-120)}: System startup initiated.",
            $"{DateTime.Now.AddMinutes(-65)}: Admin 'Initial Admin' logged in.",
            $"{DateTime.Now.AddMinutes(-30)}: Patient 'Test Patient' registered.",
            $"{DateTime.Now.AddMinutes(-5)}: Doctor 'Dr. Test' updated availability."
        };

        public Task<IEnumerable<string>> GetRecentLogsAsync(int count = 10)
        {
            // Simulate fetching recent logs
            var recentLogs = _logs.OrderByDescending(log => log.Substring(0, log.IndexOf(':'))) // Simple sort by date prefix
                                  .Take(count)
                                  .ToList();
            return Task.FromResult<IEnumerable<string>>(recentLogs);
        }

        // Method to add logs (call this from other services/controllers where actions happen)
        public static void AddLog(string message)
        {
            _logs.Insert(0, $"{DateTime.Now}: {message}"); // Add to the beginning to keep it somewhat ordered by recent
            if (_logs.Count > 100) // Cap the log size
            {
                _logs.RemoveAt(_logs.Count - 1);
            }
        }
    }
}