// File: Services/SystemLogService.cs
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;    // For PatientAppointmentDbContext
using Patient_Appointment_Management_System.Models;  // For SystemLog model (and your LogLevel enum)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// Note: Microsoft.Extensions.Logging is not explicitly used here, but its LogLevel can be brought in by other ASP.NET Core dependencies.

namespace Patient_Appointment_Management_System.Services
{
    public class SystemLogService : ISystemLogService
    {
        private readonly PatientAppointmentDbContext _context; // Inject DbContext

        // Constructor to inject DbContext
        public SystemLogService(PatientAppointmentDbContext context)
        {
            _context = context;
        }

        // Instance method to log an event using the SystemLog model
        public async Task LogEventAsync(SystemLog logEntry)
        {
            if (logEntry == null) throw new ArgumentNullException(nameof(logEntry));

            // Ensure timestamp and level are set if not already
            logEntry.Timestamp = logEntry.Timestamp == DateTime.MinValue ? DateTime.UtcNow : logEntry.Timestamp;

            // --- QUALIFIED ENUM --- (Line 29 was here)
            logEntry.Level = string.IsNullOrEmpty(logEntry.Level) ?
                             Patient_Appointment_Management_System.Models.LogLevel.Information.ToString() :
                             logEntry.Level;

            _context.SystemLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }

        // Alternative LogEventAsync if you prefer specific parameters
        // public async Task LogEventAsync(string eventType, string message, string source,
        //                                 Patient_Appointment_Management_System.Models.LogLevel level = Patient_Appointment_Management_System.Models.LogLevel.Information, // QUALIFIED
        //                                 string userId = null)
        // {
        //     var logEntry = new SystemLog
        //     {
        //         Timestamp = DateTime.UtcNow,
        //         Level = level.ToString(), // level is now explicitly your enum
        //         EventType = eventType,
        //         Message = message,
        //         Source = source,
        //         UserId = userId
        //     };
        //     _context.SystemLogs.Add(logEntry);
        //     await _context.SaveChangesAsync();
        // }


        public async Task<List<SystemLog>> GetRecentLogsAsync(int count = 10)
        {
            if (count <= 0) count = 10;

            return await _context.SystemLogs
                                 .OrderByDescending(log => log.Timestamp)
                                 .Take(count)
                                 .ToListAsync();
        }

        public async Task<(List<SystemLog> Logs, int TotalCount)> GetPaginatedLogsAsync(
            int pageNumber, int pageSize, string filterLevel = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.SystemLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filterLevel))
            {
                // Assuming SystemLog.Level is stored as a string matching the enum member names
                query = query.Where(log => log.Level == filterLevel);
            }
            if (startDate.HasValue)
            {
                query = query.Where(log => log.Timestamp >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(log => log.Timestamp <= endOfDay);
            }

            int totalCount = await query.CountAsync();

            var logs = await query.OrderByDescending(log => log.Timestamp)
                                  .Skip((pageNumber - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();
            return (logs, totalCount);
        }
    }
}