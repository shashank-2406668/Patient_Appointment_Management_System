// File: ViewModels/SchedulingConflictViewModel.cs
using System;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class SchedulingConflictViewModel
    {
        public int ConflictId { get; set; } // <<<< THIS LINE IS CRUCIAL AND MUST BE PRESENT

        public string Message { get; set; }

        public int? Appointment1Id { get; set; }
        public string Appointment1Details { get; set; }
        public string Patient1Name { get; set; }

        public int? Appointment2Id { get; set; }
        public string Appointment2Details { get; set; }
        public string Patient2Name { get; set; }

        public string DoctorName { get; set; }
        public DateTime ConflictTime { get; set; }
        public string ConflictDetails { get; set; }
    }
}