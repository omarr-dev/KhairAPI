using Microsoft.EntityFrameworkCore;
using KhairAPI.Models.Entities;

namespace KhairAPI.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

            // Check if data already exists
            if (await context.Users.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Create supervisor user
            var supervisorUser = new User
            {
                PhoneNumber = "+966501234567",
                FullName = "أحمد المشرف",
                Role = UserRole.Supervisor,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(supervisorUser);

            // Create teacher users
            var teacher1User = new User
            {
                PhoneNumber = "+966502345678",
                FullName = "محمد المعلم",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(teacher1User);

            var teacher2User = new User
            {
                PhoneNumber = "+966553456789",
                FullName = "عبدالله المعلم",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(teacher2User);

            await context.SaveChangesAsync();

            // Create teacher records
            var teacher1 = new Teacher
            {
                UserId = teacher1User.Id,
                FullName = teacher1User.FullName,
                PhoneNumber = teacher1User.PhoneNumber,
                Qualification = "إجازة في القرآن الكريم",
                JoinDate = DateTime.UtcNow
            };
            context.Teachers.Add(teacher1);

            var teacher2 = new Teacher
            {
                UserId = teacher2User.Id,
                FullName = teacher2User.FullName,
                PhoneNumber = teacher2User.PhoneNumber,
                Qualification = "بكالوريوس دراسات إسلامية",
                JoinDate = DateTime.UtcNow
            };
            context.Teachers.Add(teacher2);

            await context.SaveChangesAsync();

            // Create halaqat
            var halaqa1 = new Halaqa
            {
                Name = "حلقة الفجر",
                Location = "المسجد الكبير",
                TimeSlot = "5:30 - 7:00 صباحاً",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Halaqat.Add(halaqa1);

            var halaqa2 = new Halaqa
            {
                Name = "حلقة العصر",
                Location = "مسجد الحي",
                TimeSlot = "4:30 - 6:00 مساءً",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Halaqat.Add(halaqa2);

            await context.SaveChangesAsync();

            // Assign teachers to halaqat
            context.HalaqaTeachers.Add(new HalaqaTeacher
            {
                HalaqaId = halaqa1.Id,
                TeacherId = teacher1.Id,
                AssignedDate = DateTime.UtcNow,
                IsPrimary = true
            });

            context.HalaqaTeachers.Add(new HalaqaTeacher
            {
                HalaqaId = halaqa2.Id,
                TeacherId = teacher2.Id,
                AssignedDate = DateTime.UtcNow,
                IsPrimary = true
            });

            // Create sample students
            var students = new[]
            {
                new Student
                {
                    FirstName = "أحمد",
                    LastName = "محمد",
                    DateOfBirth = new DateTime(2010, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                    GuardianName = "محمد أحمد",
                    GuardianPhone = "+966504567890",
                    JuzMemorized = 5,
                    CreatedAt = DateTime.UtcNow
                },
                new Student
                {
                    FirstName = "فاطمة",
                    LastName = "علي",
                    DateOfBirth = new DateTime(2011, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                    GuardianName = "علي عبدالله",
                    GuardianPhone = "+966555678901",
                    JuzMemorized = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new Student
                {
                    FirstName = "خالد",
                    LastName = "سعد",
                    DateOfBirth = new DateTime(2009, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                    GuardianName = "سعد خالد",
                    GuardianPhone = "+966506789012",
                    JuzMemorized = 8,
                    CreatedAt = DateTime.UtcNow
                },
                new Student
                {
                    FirstName = "عائشة",
                    LastName = "عمر",
                    DateOfBirth = new DateTime(2012, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                    GuardianName = "عمر محمد",
                    GuardianPhone = "+966507890123",
                    JuzMemorized = 2,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Students.AddRange(students);
            await context.SaveChangesAsync();

            // Assign students to halaqat
            context.StudentHalaqat.Add(new StudentHalaqa
            {
                StudentId = students[0].Id,
                HalaqaId = halaqa1.Id,
                TeacherId = teacher1.Id,
                EnrollmentDate = DateTime.UtcNow,
                IsActive = true
            });

            context.StudentHalaqat.Add(new StudentHalaqa
            {
                StudentId = students[1].Id,
                HalaqaId = halaqa1.Id,
                TeacherId = teacher1.Id,
                EnrollmentDate = DateTime.UtcNow,
                IsActive = true
            });

            context.StudentHalaqat.Add(new StudentHalaqa
            {
                StudentId = students[2].Id,
                HalaqaId = halaqa2.Id,
                TeacherId = teacher2.Id,
                EnrollmentDate = DateTime.UtcNow,
                IsActive = true
            });

            context.StudentHalaqat.Add(new StudentHalaqa
            {
                StudentId = students[3].Id,
                HalaqaId = halaqa2.Id,
                TeacherId = teacher2.Id,
                EnrollmentDate = DateTime.UtcNow,
                IsActive = true
            });

            await context.SaveChangesAsync();
        }
    }
}
