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
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } // Just the Date part

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; } // Just the Time part

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }   // Just the Time part

        public bool IsBooked { get; set; } = false;

        // Foreign key for the appointment that booked this slot
        public int? BookedByAppointmentId { get; set; }


        // Navigation Properties
        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [ForeignKey("BookedByAppointmentId")]
        public virtual Appointment? BookedByAppointment { get; set; } // The appointment that booked this slot
    }
}