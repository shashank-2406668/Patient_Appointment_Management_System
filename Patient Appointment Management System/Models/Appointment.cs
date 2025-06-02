// File: Patient_Appointment_Management_System/Models/Appointment.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Appointment_Management_System.Models
{
    [Table("Appointments")]
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDateTime { get; set; }

        // Add EndDateTime if you want to store appointment end explicitly
        // public DateTime EndDateTime { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // e.g., "Scheduled", "Confirmed", "Cancelled", "Completed"

        [StringLength(500)]
        public string? Issue { get; set; }

        // Foreign Key to link to the specific AvailabilitySlot that was booked
        public int? BookedAvailabilitySlotId { get; set; }


        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [ForeignKey("BookedAvailabilitySlotId")]
        public virtual AvailabilitySlot? BookedAvailabilitySlot { get; set; }
    }
}