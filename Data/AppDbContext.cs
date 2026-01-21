using Microsoft.EntityFrameworkCore;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ITenantService? _tenantService;

        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantService? tenantService = null) : base(options)
        {
            _tenantService = tenantService;
        }

        // Multi-tenancy
        public DbSet<Association> Associations { get; set; } = null!;

        public DbSet<User> Users { get; set; }
        public DbSet<Halaqa> Halaqat { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<HalaqaTeacher> HalaqaTeachers { get; set; }
        public DbSet<StudentHalaqa> StudentHalaqat { get; set; }
        public DbSet<ProgressRecord> ProgressRecords { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<TeacherAttendance> TeacherAttendances { get; set; }
        public DbSet<StudentTarget> StudentTargets { get; set; } = null!;
        public DbSet<HalaqaSupervisorAssignment> HalaqaSupervisorAssignments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Association entity
            modelBuilder.Entity<Association>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Subdomain).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(100);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
                entity.HasIndex(e => e.AssociationId);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(255);

                // Configure enum conversion
                entity.Property(e => e.Role)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.Users)
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                entity.HasIndex(e => e.AssociationId);

                // Configure one-to-one relationship with User
                entity.HasOne(e => e.User)
                    .WithOne(u => u.Teacher)
                    .HasForeignKey<Teacher>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.Teachers)
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Halaqa entity
            modelBuilder.Entity<Halaqa>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AssociationId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.TimeSlot).HasMaxLength(100);

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.Halaqat)
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Student entity
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AssociationId);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.GuardianName).HasMaxLength(255);
                entity.Property(e => e.GuardianPhone).HasMaxLength(20);

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.Students)
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure HalaqaTeacher (many-to-many relationship)
            modelBuilder.Entity<HalaqaTeacher>(entity =>
            {
                entity.HasKey(e => new { e.HalaqaId, e.TeacherId });

                // Indexes for foreign keys (reverse lookup)
                entity.HasIndex(e => e.TeacherId);
                entity.HasIndex(e => e.HalaqaId);
                entity.HasIndex(e => e.AssociationId);

                entity.HasOne(e => e.Halaqa)
                    .WithMany(h => h.HalaqaTeachers)
                    .HasForeignKey(e => e.HalaqaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Teacher)
                    .WithMany(t => t.HalaqaTeachers)
                    .HasForeignKey(e => e.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.HalaqaTeachers)
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                entity.HasIndex(e => e.AssociationId);

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

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.StudentHalaqat)
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                entity.HasIndex(e => e.AssociationId);
                
                // Optimized index for achievement calculation (filter by student + date, group by type)
                entity.HasIndex(e => new { e.StudentId, e.Date, e.Type });

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

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.ProgressRecords)
                    .HasForeignKey(e => e.AssociationId)
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
                entity.HasIndex(e => e.AssociationId);

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

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.Attendances)
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                entity.HasIndex(e => e.AssociationId);

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

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.TeacherAttendances)
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure StudentTarget entity
            modelBuilder.Entity<StudentTarget>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.StudentId).IsUnique(); // One target per student
                entity.HasIndex(e => e.AssociationId);

                // Configure relationships
                entity.HasOne(e => e.Student)
                    .WithOne(s => s.Target)
                    .HasForeignKey<StudentTarget>(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany(a => a.StudentTargets)
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure HalaqaSupervisorAssignment entity
            modelBuilder.Entity<HalaqaSupervisorAssignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Composite unique index to prevent duplicate assignments
                entity.HasIndex(e => new { e.UserId, e.HalaqaId }).IsUnique();
                
                // Indexes for efficient lookups
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.HalaqaId);
                entity.HasIndex(e => e.AssociationId);

                // Configure relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.HalaqaAssignments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Halaqa)
                    .WithMany(h => h.SupervisorAssignments)
                    .HasForeignKey(e => e.HalaqaId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Multi-tenancy: Global query filter
                entity.HasQueryFilter(e => _tenantService == null || !_tenantService.CurrentAssociationId.HasValue
                    || e.AssociationId == _tenantService.CurrentAssociationId);

                // Foreign key to Association
                entity.HasOne(e => e.Association)
                    .WithMany()
                    .HasForeignKey(e => e.AssociationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }

        /// <summary>
        /// Override SaveChangesAsync to automatically assign AssociationId to new tenant entities.
        /// Also validates that all tenant entities have a valid AssociationId before saving.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenantIdToNewEntities();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Override SaveChanges to automatically assign AssociationId to new tenant entities.
        /// Also validates that all tenant entities have a valid AssociationId before saving.
        /// </summary>
        public override int SaveChanges()
        {
            ApplyTenantIdToNewEntities();
            return base.SaveChanges();
        }

        /// <summary>
        /// Automatically assigns AssociationId from TenantService to new entities implementing ITenantEntity.
        /// Validates that no tenant entity is saved without a valid AssociationId when tenant context exists.
        /// </summary>
        private void ApplyTenantIdToNewEntities()
        {
            var tenantEntries = ChangeTracker.Entries<ITenantEntity>()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in tenantEntries)
            {
                var entity = entry.Entity;

                // If AssociationId is not set (0) and we have a tenant context, auto-assign it
                if (entity.AssociationId == 0 && _tenantService?.CurrentAssociationId.HasValue == true)
                {
                    entity.AssociationId = _tenantService.CurrentAssociationId.Value;
                }

                // Validate: If we have a tenant context but AssociationId is still 0, throw an error
                // This catches bugs where tenant context exists but entity wasn't properly assigned
                if (entity.AssociationId == 0 && _tenantService?.CurrentAssociationId.HasValue == true)
                {
                    throw new InvalidOperationException(
                        $"Cannot save entity of type '{entity.GetType().Name}' without a valid AssociationId. " +
                        $"Tenant context is available (AssociationId: {_tenantService.CurrentAssociationId}) but entity has AssociationId = 0.");
                }
            }
        }
    }
}

