// File: Patient_Appointment_Management_System/Models/AvailabilitySlot.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Appointment_Management_System.Models
{
    [Table("AvailabilitySlots")]
    public class AvailabilitySlot
    {
        [Key]
        public int AvailabilitySlotId { get; set; } // Changed name for clarity

        [Required]
        public int DoctorId { get; set; } // Foreign Key to Doctor

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; } // The date of availability

        [Required]
        [Column(TypeName = "time")]
        public TimeSpan StartTime { get; set; } // Start time of the slot

        [Required]
        [Column(TypeName = "time")]
        public TimeSpan EndTime { get; set; } // End time of the slot

        public bool IsBooked { get; set; } = false; // True if an appointment has booked this slot

        // If an appointment books this slot, this FK links them.
        // This allows an AvailabilitySlot to know which Appointment booked it.
        public int? BookedByAppointmentId { get; set; }


        // --- Navigation properties ---
        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!;

        // This navigation property allows you to get the Appointment that booked this specific slot.
        [ForeignKey("BookedByAppointmentId")]
        public virtual Appointment? BookedByAppointment { get; set; }
    }
}