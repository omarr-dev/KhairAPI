using Microsoft.EntityFrameworkCore;
using KhairAPI.Models.Entities;

namespace KhairAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Halaqa> Halaqat { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<HalaqaTeacher> HalaqaTeachers { get; set; }
        public DbSet<StudentHalaqa> StudentHalaqat { get; set; }
        public DbSet<ProgressRecord> ProgressRecords { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<TeacherAttendance> TeacherAttendances { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(255);

                // Configure enum conversion
                entity.Property(e => e.Role)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });
            
            // Configure Teacher entity
            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Qualification).HasMaxLength(500);

                // Create index on UserId for better join performance
                entity.HasIndex(e => e.UserId);

                // Configure one-to-one relationship with User
                entity.HasOne(e => e.User)
                    .WithOne(u => u.Teacher)
                    .HasForeignKey<Teacher>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure Halaqa entity
            modelBuilder.Entity<Halaqa>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.TimeSlot).HasMaxLength(100);
            });
            
            // Configure Student entity
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.GuardianName).HasMaxLength(255);
                entity.Property(e => e.GuardianPhone).HasMaxLength(20);
            });
            
            // Configure HalaqaTeacher (many-to-many relationship)
            modelBuilder.Entity<HalaqaTeacher>(entity =>
            {
                entity.HasKey(e => new { e.HalaqaId, e.TeacherId });

                // Indexes for foreign keys (reverse lookup)
                entity.HasIndex(e => e.TeacherId);
                entity.HasIndex(e => e.HalaqaId);

                entity.HasOne(e => e.Halaqa)
                    .WithMany(h => h.HalaqaTeachers)
                    .HasForeignKey(e => e.HalaqaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Teacher)
                    .WithMany(t => t.HalaqaTeachers)
                    .HasForeignKey(e => e.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure StudentHalaqa (many-to-many relationship)
            modelBuilder.Entity<StudentHalaqa>(entity =>
            {
                entity.HasKey(e => new { e.StudentId, e.HalaqaId, e.TeacherId });

                entity.Property(e => e.TeacherId).IsRequired();

                // Indexes for common query patterns
                entity.HasIndex(e => e.HalaqaId);
                entity.HasIndex(e => e.TeacherId);
                entity.HasIndex(e => new { e.HalaqaId, e.TeacherId });

                entity.HasOne(e => e.Student)
                    .WithMany(s => s.StudentHalaqat)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Halaqa)
                    .WithMany(h => h.StudentHalaqat)
                    .HasForeignKey(e => e.HalaqaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Teacher)
                    .WithMany(t => t.StudentHalaqat)
                    .HasForeignKey(e => e.TeacherId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure ProgressRecord entity
            modelBuilder.Entity<ProgressRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SurahName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(1000);

                // Configure enum conversions
                entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.Quality)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Indexes for common queries (date ranges, student lookups)
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => e.StudentId);
                entity.HasIndex(e => e.HalaqaId);
                entity.HasIndex(e => e.TeacherId);
                entity.HasIndex(e => new { e.Date, e.StudentId });
                entity.HasIndex(e => new { e.Date, e.HalaqaId });

                // Configure relationships
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.ProgressRecords)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Teacher)
                    .WithMany(t => t.ProgressRecords)
                    .HasForeignKey(e => e.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Halaqa)
                    .WithMany(h => h.ProgressRecords)
                    .HasForeignKey(e => e.HalaqaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            // Configure Attendance entity
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Notes).HasMaxLength(500);

                // Configure enum conversion
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Indexes for date range queries
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => e.HalaqaId);
                entity.HasIndex(e => new { e.Date, e.HalaqaId });

                // Configure relationships
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Attendances)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Halaqa)
                    .WithMany(h => h.Attendances)
                    .HasForeignKey(e => e.HalaqaId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Create composite index for unique attendance per student per day
                entity.HasIndex(e => new { e.StudentId, e.Date })
                    .IsUnique();
            });
            
            // Configure TeacherAttendance entity
            modelBuilder.Entity<TeacherAttendance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Notes).HasMaxLength(500);

                // Configure enum conversion
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Indexes for date range queries and lookups
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => e.TeacherId);
                entity.HasIndex(e => e.HalaqaId);
                entity.HasIndex(e => new { e.Date, e.TeacherId });
                entity.HasIndex(e => new { e.Date, e.HalaqaId });

                // Configure relationships
                entity.HasOne(e => e.Teacher)
                    .WithMany(t => t.TeacherAttendances)
                    .HasForeignKey(e => e.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Halaqa)
                    .WithMany(h => h.TeacherAttendances)
                    .HasForeignKey(e => e.HalaqaId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create composite index for unique attendance per teacher per halaqa per day
                entity.HasIndex(e => new { e.TeacherId, e.HalaqaId, e.Date })
                    .IsUnique();
            });
        }
    }
}
