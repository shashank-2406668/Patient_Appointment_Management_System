// File: Patient_Appointment_Management_System/Data/PatientAppointmentDbContext.cs
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Models; // Ensure this matches your models namespace

namespace Patient_Appointment_Management_System.Data
{
    public class PatientAppointmentDbContext : DbContext
    {
        public PatientAppointmentDbContext(DbContextOptions<PatientAppointmentDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----- Configure Entities and Relationships -----

            // --- Patient ---
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.ToTable("Patients"); // Explicit table name
                entity.HasKey(e => e.PatientId);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique(); // Unique constraint for email
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Dob).IsRequired().HasColumnType("date");
            });

            // --- Doctor ---
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.ToTable("Doctors");
                entity.HasKey(e => e.DoctorId);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Specialization).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            });

            // --- Admin ---
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("Admins");
                entity.HasKey(e => e.AdminId);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100); // Making Email required for Admin too
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Role).HasMaxLength(50);
            });

            // --- Appointment ---
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointments");
                entity.HasKey(e => e.AppointmentId);
                entity.Property(e => e.AppointmentDateTime).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Issue).HasMaxLength(500);

                // Relationship: One Patient to Many Appointments
                entity.HasOne(a => a.Patient)
                      .WithMany(p => p.Appointments)
                      .HasForeignKey(a => a.PatientId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting patient if they have appointments

                // Relationship: One Doctor to Many Appointments
                entity.HasOne(a => a.Doctor)
                      .WithMany(d => d.Appointments)
                      .HasForeignKey(a => a.DoctorId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting doctor if they have appointments
            });

            // --- AvailabilitySlot ---
            modelBuilder.Entity<AvailabilitySlot>(entity =>
            {
                entity.ToTable("AvailabilitySlots");
                entity.HasKey(e => e.AvailabilitySlotId);
                entity.Property(e => e.Date).IsRequired().HasColumnType("date");
                entity.Property(e => e.StartTime).IsRequired().HasColumnType("time");
                entity.Property(e => e.EndTime).IsRequired().HasColumnType("time");

                // Relationship: One Doctor to Many AvailabilitySlots
                entity.HasOne(av => av.Doctor)
                      .WithMany(d => d.AvailabilitySlots)
                      .HasForeignKey(av => av.DoctorId)
                      .OnDelete(DeleteBehavior.Cascade); // If Doctor is deleted, their slots are also deleted

                // Relationship: One AvailabilitySlot can be booked by one Appointment
                // Appointment.BookedAvailabilitySlotId points to AvailabilitySlot.AvailabilitySlotId
                // AvailabilitySlot.BookedByAppointmentId points to Appointment.AppointmentId
                entity.HasOne(av => av.BookedByAppointment) // Navigation property in AvailabilitySlot
                      .WithOne(ap => ap.BookedAvailabilitySlot) // Navigation property in Appointment
                      .HasForeignKey<Appointment>(ap => ap.BookedAvailabilitySlotId) // FK is on Appointment table pointing to this slot
                      .IsRequired(false) // An appointment doesn't *have* to be from a pre-defined slot
                      .OnDelete(DeleteBehavior.SetNull); // If the slot is deleted, the FK in Appointment becomes NULL
            });
            // This configuration ^^^ for AvailabilitySlot <-> Appointment implies:
            // - Appointment has a nullable FK `BookedAvailabilitySlotId`
            // - AvailabilitySlot has a nullable FK `BookedByAppointmentId`
            // And their navigation properties link them. The `WithOne().HasForeignKey()` defines one side of the 1:1.
            // Let's refine the one-to-one relationship to be clearer for Appointment booking a Slot.
            // An Appointment *books* an AvailabilitySlot.
            // An AvailabilitySlot *is booked by* an Appointment.

            // --- Notification ---
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(e => e.NotificationId);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.NotificationType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SentDate).IsRequired();
                entity.Property(e => e.Url).HasMaxLength(255);

                // Relationships for recipients (optional based on querying needs)
                entity.HasOne(n => n.Patient)
                      .WithMany() // A patient can have many notifications, but Notification doesn't have a collection of Patients
                      .HasForeignKey(n => n.PatientId)
                      .IsRequired(false) // A notification doesn't always go to a Patient
                      .OnDelete(DeleteBehavior.Cascade); // If patient is deleted, their notifications are too

                entity.HasOne(n => n.Doctor)
                      .WithMany()
                      .HasForeignKey(n => n.DoctorId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.Admin)
                      .WithMany()
                      .HasForeignKey(n => n.AdminId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}