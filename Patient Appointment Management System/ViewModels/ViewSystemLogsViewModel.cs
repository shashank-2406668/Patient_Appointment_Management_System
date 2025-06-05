// File: ViewModels/ViewSystemLogsViewModel.cs
using Patient_Appointment_Management_System.Models; // For SystemLog
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList

namespace Patient_Appointment_Management_System.ViewModels
{
    public class ViewSystemLogsViewModel
    {
        public List<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalLogs { get; set; }

        // For filtering capabilities
        public string? FilterLevel { get; set; }
        public DateTime? FilterStartDate { get; set; }
        public DateTime? FilterEndDate { get; set; }

        // For populating the filter dropdown
        public List<SelectListItem> AvailableLogLevels { get; set; } = new List<SelectListItem>();

        public ViewSystemLogsViewModel()
        {
            // Populate with common log levels. This could also be fetched dynamically from distinct values in DB.
            AvailableLogLevels.Add(new SelectListItem("All Levels", ""));
            foreach (var level in Enum.GetNames(typeof(Patient_Appointment_Management_System.Models.LogLevel))) // Assuming your LogLevel enum
            {
                AvailableLogLevels.Add(new SelectListItem(level, level));
            }
        }
    }
}