// File: Services/ISystemLogService.cs
using Patient_Appointment_Management_System.Models; // For SystemLog model
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patient_Appointment_Management_System.Services
{
    public interface ISystemLogService
    {
        // Changed to accept structured log data and be an instance method
        Task LogEventAsync(SystemLog logEntry); // More flexible, or specific parameters as before:
        // Task LogEventAsync(string eventType, string message, string source, LogLevel level = LogLevel.Information, string userId = null);

        // --- THIS METHOD'S RETURN TYPE IS CHANGED ---
        Task<List<SystemLog>> GetRecentLogsAsync(int count = 10); // Returns List<SystemLog>

        // For pagination
        Task<(List<SystemLog> Logs, int TotalCount)> GetPaginatedLogsAsync(
            int pageNumber,
            int pageSize,
            string filterLevel = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}