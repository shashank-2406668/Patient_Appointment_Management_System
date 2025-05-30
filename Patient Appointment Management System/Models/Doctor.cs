// File: Patient_Appointment_Management_System/Models/Doctor.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Appointment_Management_System.Models
{
    [Table("Doctors")] // Pluralized table name
    public class Doctor
    {
        [Key]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Specialization is required.")]
        [StringLength(100)]
        public string Specialization { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password hash is required.")]
        public string PasswordHash { get; set; } // Stores the HASHED password

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Invalid Phone Number.")]
        public string Phone { get; set; }

        // Navigation property for related appointments
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        // Navigation property for doctor's availability slots
        public virtual ICollection<AvailabilitySlot> AvailabilitySlots { get; set; } = new List<AvailabilitySlot>();
    }
}