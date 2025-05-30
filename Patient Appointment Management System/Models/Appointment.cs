// File: Patient_Appointment_Management_System/Models/Appointment.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Appointment_Management_System.Models
{
    [Table("Appointments")] // Pluralized table name
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        // Foreign Key for Patient
        [Required]
        public int PatientId { get; set; }

        // Foreign Key for Doctor
        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDateTime { get; set; } // Combined Date and Time of appointment

        [Required]
        [StringLength(50)] // Increased length for more descriptive statuses
        public string Status { get; set; } // e.g., "Scheduled", "Confirmed", "Cancelled", "Completed"

        [StringLength(500)] // For patient's reported issue/reason
        public string? Issue { get; set; }

        // Optional: Link to a specific availability slot that was booked for this appointment
        public int? BookedAvailabilitySlotId { get; set; }


        // --- Navigation properties ---
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!; // Required, so initialize to null! or ensure loaded

        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!; // Required, so initialize to null! or ensure loaded

        [ForeignKey("BookedAvailabilitySlotId")]
        public virtual AvailabilitySlot? BookedAvailabilitySlot { get; set; } // An appointment might not always be tied to a predefined slot
    }
}