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
                // Assuming Phone, Address, and Dob are part of your Patient model
                entity.Property(e => e.Phone).IsRequired(false).HasMaxLength(20); // Made optional as per typical design
                entity.Property(e => e.Address).IsRequired(false).HasMaxLength(255); // Made optional
                entity.Property(e => e.Dob).HasColumnType("date").IsRequired(false); // Made optional
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
                entity.Property(e => e.Phone).IsRequired(false).HasMaxLength(20); // Made optional
            });

            // --- Admin ---
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("Admins");
                entity.HasKey(e => e.AdminId);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100); // Updated: Name is required
                entity.Property(e => e.Role).IsRequired(false).HasMaxLength(50); // Role can be optional or have a default
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
                      .WithMany(p => p.Appointments) // Assumes Patient model has ICollection<Appointment> Appointments
                      .HasForeignKey(a => a.PatientId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting patient if they have appointments

                // Relationship: One Doctor to Many Appointments
                entity.HasOne(a => a.Doctor)
                      .WithMany(d => d.Appointments) // Assumes Doctor model has ICollection<Appointment> Appointments
                      .HasForeignKey(a => a.DoctorId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting doctor if they have appointments

                // Relationship with AvailabilitySlot is defined below where AvailabilitySlot is the principal
                // in one part of the one-to-one definition you provided.
            });

            // --- AvailabilitySlot ---
            modelBuilder.Entity<AvailabilitySlot>(entity =>
            {
                entity.ToTable("AvailabilitySlots");
                entity.HasKey(e => e.AvailabilitySlotId);
                entity.Property(e => e.Date).IsRequired().HasColumnType("date");
                entity.Property(e => e.StartTime).IsRequired().HasColumnType("time"); // Store as TimeSpan, maps to time in DB
                entity.Property(e => e.EndTime).IsRequired().HasColumnType("time");   // Store as TimeSpan, maps to time in DB
                entity.Property(e => e.IsBooked).IsRequired().HasDefaultValue(false);


                // Relationship: One Doctor to Many AvailabilitySlots
                entity.HasOne(av => av.Doctor)
                      .WithMany(d => d.AvailabilitySlots) // Assumes Doctor model has ICollection<AvailabilitySlot> AvailabilitySlots
                      .HasForeignKey(av => av.DoctorId)
                      .OnDelete(DeleteBehavior.Cascade); // If Doctor is deleted, their slots are also deleted

                // Relationship: One AvailabilitySlot can be booked by one Appointment (One-to-One)
                // This establishes that an AvailabilitySlot *can have one* BookedByAppointment
                // And that Appointment *can have one* BookedAvailabilitySlot (itself).
                // The FK `BookedAvailabilitySlotId` is on the `Appointment` table.
                entity.HasOne(av => av.BookedByAppointment) // Navigation property in AvailabilitySlot to the Appointment that booked it
                      .WithOne(ap => ap.BookedAvailabilitySlot) // Navigation property in Appointment to the Slot it booked
                      .HasForeignKey<Appointment>(ap => ap.BookedAvailabilitySlotId) // The FK is on the Appointment table
                      .IsRequired(false) // An Appointment does not *have* to be booked via an AvailabilitySlot
                                         // An AvailabilitySlot is not always booked (IsBooked flag handles this state)
                      .OnDelete(DeleteBehavior.SetNull); // If an AvailabilitySlot is deleted, the Appointment's BookedAvailabilitySlotId becomes NULL.
                                                         // This implies the appointment is no longer tied to a specific pre-defined slot.
                                                         // The IsBooked flag on AvailabilitySlot should be managed by application logic.
            });

            // --- Notification ---
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(e => e.NotificationId);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.NotificationType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SentDate).IsRequired();
                entity.Property(e => e.IsRead).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.Url).HasMaxLength(255);

                // Define FKs for recipients directly for clarity, assuming PatientId, DoctorId, AdminId are nullable ints in Notification model
                entity.HasOne(n => n.Patient)
                      .WithMany() // Assuming Patient does not have a direct ICollection<Notification>
                      .HasForeignKey(n => n.PatientId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Cascade); // If patient is deleted, their notifications are too

                entity.HasOne(n => n.Doctor)
                      .WithMany() // Assuming Doctor does not have a direct ICollection<Notification>
                      .HasForeignKey(n => n.DoctorId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.Admin)
                      .WithMany() // Assuming Admin does not have a direct ICollection<Notification>
                      .HasForeignKey(n => n.AdminId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Consider adding unique constraints or indexes where appropriate, e.g.,
            // modelBuilder.Entity<AvailabilitySlot>()
            //    .HasIndex(s => new { s.DoctorId, s.Date, s.StartTime })
            //    .IsUnique(); // This would prevent a doctor from creating the exact same slot twice.
        }
    }
}