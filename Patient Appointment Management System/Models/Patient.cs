// File: Patient_Appointment_Management_System/Models/Patient.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Appointment_Management_System.Models
{
    [Table("Patients")] // Pluralized table name
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password hash is required.")]
        public string PasswordHash { get; set; } // Stores the HASHED password

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(20)]
        [Phone]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(255)] // Added a StringLength for Address
        public string Address { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [Column(TypeName = "date")]
        [DataType(DataType.Date)]
        public DateTime Dob { get; set; }

        // Navigation property for related appointments
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
} 