// ViewModels/AppointmentSummaryViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class AppointmentSummaryViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Patient")]
        public string PatientName { get; set; } = string.Empty; // Initialize

        [Display(Name = "Appointment Slot")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd hh:mm tt}", ApplyFormatInEditMode = true)]
        public DateTime AppointmentDateTime { get; set; }

        public string Status { get; set; } = string.Empty; // Initialize
    }
}