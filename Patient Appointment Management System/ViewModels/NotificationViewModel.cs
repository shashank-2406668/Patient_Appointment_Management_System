// File: Patient_Appointment_Management_System/ViewModels/NotificationViewModel.cs
using System;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; }
        public string? Url { get; set; }
        public string TimeAgo => GetTimeAgo(SentDate);

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";

            return dateTime.ToString("MMM dd, yyyy");
        }

        public string GetNotificationIcon()
        {
            return NotificationType switch
            {
                "Booking" => "bi-calendar-plus text-success",
                "Cancellation" => "bi-calendar-x text-danger",
                "Reschedule" => "bi-calendar-event text-warning",
                "Confirmation" => "bi-check-circle text-info",
                _ => "bi-bell"
            };
        }
    }
}