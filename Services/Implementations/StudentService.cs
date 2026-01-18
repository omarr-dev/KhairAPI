using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    public class StudentService : IStudentService
    {
        private readonly AppDbContext _context;
        private readonly IQuranService _quranService;
        private readonly ITenantService _tenantService;

        public StudentService(AppDbContext context, IQuranService quranService, ITenantService tenantService)
        {
            _context = context;
            _quranService = quranService;
            _tenantService = tenantService;
        }

        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .AsSplitQuery()
                .ToListAsync();

            return students.Select(MapToDto);
        }

        public async Task<PaginatedResponse<StudentDto>> GetStudentsPaginatedAsync(StudentFilterDto filter)
        {
            var query = _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                query = query.Where(s =>
                    s.FirstName.ToLower().Contains(searchLower) ||
                    s.LastName.ToLower().Contains(searchLower) ||
                    (s.GuardianName != null && s.GuardianName.ToLower().Contains(searchLower)) ||
                    (s.GuardianPhone != null && s.GuardianPhone.Contains(searchLower))
                );
            }

            if (filter.HalaqaId.HasValue && filter.HalaqaId.Value > 0)
            {
                query = query.Where(s => s.StudentHalaqat.Any(sh => sh.HalaqaId == filter.HalaqaId.Value && sh.IsActive));
            }

            if (filter.TeacherId.HasValue && filter.TeacherId.Value > 0)
            {
                query = query.Where(s => s.StudentHalaqat.Any(sh => sh.TeacherId == filter.TeacherId.Value && sh.IsActive));
            }

            var totalCount = await query.CountAsync();

            query = filter.SortBy?.ToLower() switch
            {
                "juz" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(s => s.JuzMemorized)
                    : query.OrderBy(s => s.JuzMemorized),
                "createdat" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(s => s.CreatedAt)
                    : query.OrderBy(s => s.CreatedAt),
                _ => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(s => s.FirstName).ThenByDescending(s => s.LastName)
                    : query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName)
            };

            var students = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResponse<StudentDto>
            {
                Items = students.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<IEnumerable<StudentDto>> GetStudentsByHalaqaAsync(int halaqaId)
        {
            var students = await _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .AsSplitQuery()
                .Where(s => s.StudentHalaqat.Any(sh => sh.HalaqaId == halaqaId && sh.IsActive))
                .ToListAsync();

            return students.Select(MapToDto);
        }

        public async Task<IEnumerable<StudentDto>> GetStudentsByTeacherAsync(int teacherId)
        {
            var students = await _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .AsSplitQuery()
                .Where(s => s.StudentHalaqat.Any(sh => sh.TeacherId == teacherId && sh.IsActive))
                .ToListAsync();

            return students.Select(MapToDto);
        }

        public async Task<StudentDto?> GetStudentByIdAsync(int id)
        {
            var student = await _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .AsSplitQuery()
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync(s => s.Id == id);

            return student != null ? MapToDto(student) : null;
        }

        public async Task<StudentDetailDto?> GetStudentDetailsAsync(int id)
        {
            var student = await _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .AsSplitQuery()
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return null;

            var activeAssignment = student.StudentHalaqat.FirstOrDefault(sh => sh.IsActive);
            var halaqaId = activeAssignment?.HalaqaId;

            var allProgressRecords = await _context.ProgressRecords
                .Where(p => p.StudentId == id)
                .OrderByDescending(p => p.Date)
                .Include(p => p.Teacher)
                .Include(p => p.Halaqa)
                .ToListAsync();

            var progressRecords = allProgressRecords.Take(20).ToList();

            var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);
            var attendanceRecords = await _context.Attendances
                .Where(a => a.StudentId == id && a.Date >= sixtyDaysAgo)
                .OrderByDescending(a => a.Date)
                .Include(a => a.Halaqa)
                .ToListAsync();

            var memorizationRecords = allProgressRecords.Where(p => p.Type == ProgressType.Memorization).ToList();
            var revisionRecords = allProgressRecords.Where(p => p.Type == ProgressType.Revision).ToList();

            var totalVersesMemorized = memorizationRecords.Sum(p => p.ToVerse - p.FromVerse + 1);
            var totalVersesRevised = revisionRecords.Sum(p => p.ToVerse - p.FromVerse + 1);

            double averageQuality = 0;
            string averageQualityText = "غير محدد";
            if (allProgressRecords.Any())
            {
                averageQuality = allProgressRecords.Average(p => (int)p.Quality);
                averageQualityText = averageQuality switch
                {
                    < 0.5 => "ممتاز",
                    < 1.5 => "جيد جداً",
                    < 2.5 => "جيد",
                    _ => "مقبول"
                };
            }

            var presentDays = attendanceRecords.Count(a => a.Status == AttendanceStatus.Present);
            var absentDays = attendanceRecords.Count(a => a.Status == AttendanceStatus.Absent);
            var lateDays = attendanceRecords.Count(a => a.Status == AttendanceStatus.Late);
            var totalClassDays = attendanceRecords.Count;
            var attendanceRate = totalClassDays > 0 ? (double)(presentDays + lateDays) / totalClassDays * 100 : 0;

            var surah = _quranService.GetSurahByNumber(student.CurrentSurahNumber);

            return new StudentDetailDto
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                DateOfBirth = student.DateOfBirth,
                GuardianName = student.GuardianName,
                GuardianPhone = student.GuardianPhone,
                Phone = student.Phone,
                IdNumber = student.IdNumber,
                MemorizationDirection = student.MemorizationDirection.ToString(),
                CurrentSurahNumber = student.CurrentSurahNumber,
                CurrentSurahName = surah?.Name,
                CurrentVerse = student.CurrentVerse,
                JuzMemorized = student.JuzMemorized,
                HalaqaId = halaqaId,
                CurrentHalaqa = activeAssignment?.Halaqa?.Name,
                HalaqaActiveDays = activeAssignment?.Halaqa?.ActiveDays,
                TeacherName = activeAssignment?.Teacher?.FullName,
                CreatedAt = student.CreatedAt,
                Stats = new StudentStatsDto
                {
                    TotalVersesMemorized = totalVersesMemorized,
                    TotalVersesRevised = totalVersesRevised,
                    AttendanceRate = Math.Round(attendanceRate, 1),
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LateDays = lateDays,
                    TotalClassDays = totalClassDays,
                    AverageQuality = Math.Round(averageQuality, 2),
                    AverageQualityText = averageQualityText,
                    TotalProgressRecords = allProgressRecords.Count
                },
                RecentProgress = progressRecords.Select(p => new ProgressRecordDto
                {
                    Id = p.Id,
                    StudentId = p.StudentId,
                    StudentName = student.FullName,
                    TeacherId = p.TeacherId,
                    TeacherName = p.Teacher?.FullName,
                    HalaqaId = p.HalaqaId,
                    HalaqaName = p.Halaqa?.Name ?? "",
                    Date = p.Date,
                    Type = p.Type switch
                    {
                        ProgressType.Memorization => "حفظ جديد",
                        ProgressType.Revision => "مراجعة",
                        ProgressType.Consolidation => "التثبيت",
                        _ => "غير محدد"
                    },
                    SurahName = p.SurahName,
                    FromVerse = p.FromVerse,
                    ToVerse = p.ToVerse,
                    Quality = p.Quality switch
                    {
                        QualityRating.Excellent => "ممتاز",
                        QualityRating.VeryGood => "جيد جداً",
                        QualityRating.Good => "جيد",
                        QualityRating.Acceptable => "مقبول",
                        _ => "غير محدد"
                    },
                    Notes = p.Notes,
                    CreatedAt = p.CreatedAt
                }).ToList(),
                RecentAttendance = attendanceRecords.Select(a => new AttendanceRecordDto
                {
                    Id = a.Id,
                    StudentId = a.StudentId,
                    StudentName = student.FullName,
                    HalaqaId = a.HalaqaId,
                    HalaqaName = a.Halaqa?.Name ?? "",
                    Date = a.Date,
                    Status = a.Status switch
                    {
                        AttendanceStatus.Present => "حاضر",
                        AttendanceStatus.Absent => "غائب",
                        AttendanceStatus.Late => "متأخر",
                        _ => "غير محدد"
                    },
                    Notes = a.Notes,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };
        }

        public async Task<StudentDto> CreateStudentAsync(CreateStudentDto dto)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            var direction = dto.MemorizationDirection?.ToLower() == "backward"
                ? MemorizationDirection.Backward
                : MemorizationDirection.Forward;

            var student = new Student
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DateOfBirth = dto.DateOfBirth.HasValue
                    ? DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc)
                    : null,
                GuardianName = dto.GuardianName,
                GuardianPhone = dto.GuardianPhone,
                Phone = dto.Phone,
                IdNumber = dto.IdNumber,
                MemorizationDirection = direction,
                CurrentSurahNumber = dto.CurrentSurahNumber,
                CurrentVerse = dto.CurrentVerse,
                JuzMemorized = 0,
                CreatedAt = DateTime.UtcNow,
                AssociationId = _tenantService.CurrentAssociationId.Value
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            if (dto.HalaqaId.HasValue && dto.TeacherId.HasValue)
            {
                var studentHalaqa = new StudentHalaqa
                {
                    StudentId = student.Id,
                    HalaqaId = dto.HalaqaId.Value,
                    TeacherId = dto.TeacherId.Value,
                    EnrollmentDate = DateTime.UtcNow,
                    IsActive = true,
                    AssociationId = _tenantService.CurrentAssociationId.Value
                };

                _context.StudentHalaqat.Add(studentHalaqa);
                await _context.SaveChangesAsync();
            }

            return await GetStudentByIdAsync(student.Id) ?? MapToDto(student);
        }

        public async Task<StudentDto?> UpdateStudentAsync(int id, UpdateStudentDto dto)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return null;

            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            student.DateOfBirth = dto.DateOfBirth.HasValue
                ? DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc)
                : null;
            student.GuardianName = dto.GuardianName;
            student.GuardianPhone = dto.GuardianPhone;
            student.Phone = dto.Phone;
            student.IdNumber = dto.IdNumber;

            await _context.SaveChangesAsync();
            return await GetStudentByIdAsync(id);
        }

        public async Task<StudentDto?> UpdateMemorizationAsync(int id, UpdateMemorizationDto dto)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return null;

            var direction = dto.MemorizationDirection?.ToLower() == "backward"
                ? MemorizationDirection.Backward
                : MemorizationDirection.Forward;

            student.MemorizationDirection = direction;
            student.CurrentSurahNumber = dto.CurrentSurahNumber;
            student.CurrentVerse = dto.CurrentVerse;

            student.JuzMemorized = _quranService.CalculateJuzMemorized(
                direction,
                dto.CurrentSurahNumber,
                dto.CurrentVerse
            );

            await _context.SaveChangesAsync();
            return await GetStudentByIdAsync(id);
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignStudentToHalaqaAsync(AssignStudentDto dto)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            var studentExists = await _context.Students.AnyAsync(s => s.Id == dto.StudentId);
            if (!studentExists)
                return false;

            var halaqaExists = await _context.Halaqat.AnyAsync(h => h.Id == dto.HalaqaId);
            if (!halaqaExists)
                return false;

            var teacherAssignedToHalaqa = await _context.HalaqaTeachers
                .AnyAsync(ht => ht.TeacherId == dto.TeacherId && ht.HalaqaId == dto.HalaqaId);
            if (!teacherAssignedToHalaqa)
                return false;

            var existingAssignment = await _context.StudentHalaqat
                .FirstOrDefaultAsync(sh => sh.StudentId == dto.StudentId &&
                                           sh.HalaqaId == dto.HalaqaId &&
                                           sh.TeacherId == dto.TeacherId);

            if (existingAssignment != null)
            {
                existingAssignment.IsActive = true;
                await _context.SaveChangesAsync();
                return true;
            }

            var studentHalaqa = new StudentHalaqa
            {
                StudentId = dto.StudentId,
                HalaqaId = dto.HalaqaId,
                TeacherId = dto.TeacherId,
                EnrollmentDate = DateTime.UtcNow,
                IsActive = true,
                AssociationId = _tenantService.CurrentAssociationId.Value
            };

            _context.StudentHalaqat.Add(studentHalaqa);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<StudentAssignmentDto>> GetStudentAssignmentsAsync(int studentId)
        {
            var assignments = await _context.StudentHalaqat
                .Where(sh => sh.StudentId == studentId)
                .Include(sh => sh.Halaqa)
                .Include(sh => sh.Teacher)
                .ToListAsync();

            return assignments.Select(sh => new StudentAssignmentDto
            {
                StudentId = sh.StudentId,
                HalaqaId = sh.HalaqaId,
                HalaqaName = sh.Halaqa.Name,
                TeacherId = sh.TeacherId,
                TeacherName = sh.Teacher.FullName,
                EnrollmentDate = sh.EnrollmentDate,
                IsActive = sh.IsActive
            });
        }

        public async Task<StudentAssignmentDto?> UpdateAssignmentAsync(int studentId, int halaqaId, int teacherId, UpdateAssignmentDto dto)
        {
            var assignment = await _context.StudentHalaqat
                .Include(sh => sh.Halaqa)
                .Include(sh => sh.Teacher)
                .FirstOrDefaultAsync(sh => sh.StudentId == studentId &&
                                           sh.HalaqaId == halaqaId &&
                                           sh.TeacherId == teacherId);

            if (assignment == null)
                return null;

            var teacherAssignedToHalaqa = await _context.HalaqaTeachers
                .AnyAsync(ht => ht.TeacherId == dto.TeacherId && ht.HalaqaId == dto.HalaqaId);
            if (!teacherAssignedToHalaqa)
                return null;

            assignment.HalaqaId = dto.HalaqaId;
            assignment.TeacherId = dto.TeacherId;
            assignment.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            await _context.Entry(assignment).Reference(sh => sh.Halaqa).LoadAsync();
            await _context.Entry(assignment).Reference(sh => sh.Teacher).LoadAsync();

            return new StudentAssignmentDto
            {
                StudentId = assignment.StudentId,
                HalaqaId = assignment.HalaqaId,
                HalaqaName = assignment.Halaqa.Name,
                TeacherId = assignment.TeacherId,
                TeacherName = assignment.Teacher.FullName,
                EnrollmentDate = assignment.EnrollmentDate,
                IsActive = assignment.IsActive
            };
        }

        public async Task<bool> DeleteAssignmentAsync(int studentId, int halaqaId, int teacherId)
        {
            var assignment = await _context.StudentHalaqat
                .FirstOrDefaultAsync(sh => sh.StudentId == studentId &&
                                           sh.HalaqaId == halaqaId &&
                                           sh.TeacherId == teacherId);

            if (assignment == null)
                return false;

            _context.StudentHalaqat.Remove(assignment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<StudentDto>> SearchStudentsAsync(string searchTerm)
        {
            var query = _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(s =>
                    s.FirstName.ToLower().Contains(searchTerm) ||
                    s.LastName.ToLower().Contains(searchTerm) ||
                    (s.GuardianName != null && s.GuardianName.ToLower().Contains(searchTerm)) ||
                    (s.GuardianPhone != null && s.GuardianPhone.Contains(searchTerm))
                );
            }

            var students = await query.ToListAsync();
            return students.Select(MapToDto);
        }

        private StudentDto MapToDto(Student student)
        {
            var activeAssignment = student.StudentHalaqat.FirstOrDefault(sh => sh.IsActive);

            return new StudentDto
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                DateOfBirth = student.DateOfBirth,
                GuardianName = student.GuardianName,
                GuardianPhone = student.GuardianPhone,
                Phone = student.Phone,
                IdNumber = student.IdNumber,
                MemorizationDirection = student.MemorizationDirection.ToString(),
                CurrentSurahNumber = student.CurrentSurahNumber,
                CurrentVerse = student.CurrentVerse,
                JuzMemorized = student.JuzMemorized,
                CurrentHalaqa = activeAssignment?.Halaqa?.Name,
                TeacherName = activeAssignment?.Teacher?.FullName,
                CreatedAt = student.CreatedAt,
                Assignments = student.StudentHalaqat.Select(sh => new StudentAssignmentDto
                {
                    StudentId = sh.StudentId,
                    HalaqaId = sh.HalaqaId,
                    HalaqaName = sh.Halaqa?.Name ?? "",
                    TeacherId = sh.TeacherId,
                    TeacherName = sh.Teacher?.FullName ?? "",
                    EnrollmentDate = sh.EnrollmentDate,
                    IsActive = sh.IsActive
                }).ToList()
            };
        }
    }
}

