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
        public int AvailabilitySlotId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsBooked { get; set; } = false;

        // Optional: Direct link to the appointment that booked this slot
        public int? BookedByAppointmentId { get; set; }


        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!;

        // Optional: Navigation property back to the appointment
        // This creates a one-to-one relationship from AvailabilitySlot to Appointment (if an appointment booked it)
        // Ensure your DbContext snapshot handles this correctly (it might be a one-to-one or one-to-zero-or-one)
        [ForeignKey("BookedByAppointmentId")]
        public virtual Appointment? BookedByAppointment { get; set; }
    }
}