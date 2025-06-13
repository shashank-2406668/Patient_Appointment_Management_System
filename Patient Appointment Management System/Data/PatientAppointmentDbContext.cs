using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Models;

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
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----- Configure Entities and Relationships -----

            // --- Patient (Unchanged) ---
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.ToTable("Patients");
                entity.HasKey(e => e.PatientId);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Phone).IsRequired(false).HasMaxLength(20);
                entity.Property(e => e.Address).IsRequired(false).HasMaxLength(255);
                entity.Property(e => e.Dob).HasColumnType("date").IsRequired(false);
            });

            // --- Doctor (Unchanged) ---
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.ToTable("Doctors");
                entity.HasKey(e => e.DoctorId);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Specialization).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired(false).HasMaxLength(20);
            });

            // --- Admin (Unchanged) ---
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("Admins");
                entity.HasKey(e => e.AdminId);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired(false).HasMaxLength(50);
            });

            // --- Appointment ---
            // --- Appointment ---
            modelBuilder.Entity<Appointment>(entity =>
            {
                // ...
                // Relationship: One Doctor to Many Appointments
                entity.HasOne(a => a.Doctor)
                      .WithMany(d => d.Appointments)
                      .HasForeignKey(a => a.DoctorId)
                      .OnDelete(DeleteBehavior.Restrict); // <<< THIS IS THE FIX. Prevents cascade cycle.
            });

            // --- AvailabilitySlot (Unchanged) ---
            modelBuilder.Entity<AvailabilitySlot>(entity =>
            {
                entity.ToTable("AvailabilitySlots");
                // ... a lot of configuration here, no changes needed
                entity.HasOne(av => av.Doctor)
                      .WithMany(d => d.AvailabilitySlots)
                      .HasForeignKey(av => av.DoctorId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(av => av.BookedByAppointment)
                      .WithOne(ap => ap.BookedAvailabilitySlot)
                      .HasForeignKey<Appointment>(ap => ap.BookedAvailabilitySlotId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // --- Notification (Unchanged) ---
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                // ... configuration here, no changes needed
                entity.HasOne(n => n.Patient)
                     .WithMany()
                     .HasForeignKey(n => n.PatientId)
                     .IsRequired(false)
                     .OnDelete(DeleteBehavior.Cascade);
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