namespace Patient_Appointment_Management_System.ViewModels
{
    public class ConflictViewModel
    {
        public int? Appointment1Id { get; set; }
        public string Patient1Name { get; set; }
        public int? Appointment2Id { get; set; } // If it's a double booking
        public string Patient2Name { get; set; } // If it's a double booking
        public string DoctorName { get; set; }
        public DateTime ConflictTime { get; set; }
        public string ConflictDetails { get; set; }
        public string ResolutionSuggestion { get; set; }
    }
}