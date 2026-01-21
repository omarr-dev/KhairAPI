using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    public class ProgressService : IProgressService
    {
        private readonly AppDbContext _context;
        private readonly IQuranService _quranService;
        private readonly ITenantService _tenantService;
        private readonly IQuranVerseLinesService _quranVerseLinesService;

        public ProgressService(
            AppDbContext context, 
            IQuranService quranService, 
            ITenantService tenantService,
            IQuranVerseLinesService quranVerseLinesService)
        {
            _context = context;
            _quranService = quranService;
            _tenantService = tenantService;
            _quranVerseLinesService = quranVerseLinesService;
        }

        public async Task<ProgressRecordDto> CreateProgressRecordAsync(CreateProgressRecordDto dto)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            var studentAssignment = await _context.StudentHalaqat
                .OrderBy(sh => sh.StudentId).ThenBy(sh => sh.HalaqaId).ThenBy(sh => sh.TeacherId)
                .FirstOrDefaultAsync(sh =>
                    sh.StudentId == dto.StudentId &&
                    sh.HalaqaId == dto.HalaqaId &&
                    sh.TeacherId == dto.TeacherId &&
                    sh.IsActive);

            if (studentAssignment == null)
            {
                throw new InvalidOperationException("الطالب غير مسجل في هذه الحلقة مع هذا المعلم");
            }

            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null)
            {
                throw new InvalidOperationException("الطالب غير موجود");
            }

            var surah = _quranService.GetSurahByName(dto.SurahName);
            if (surah == null)
            {
                throw new InvalidOperationException("السورة غير موجودة");
            }

            // Calculate lines using the accurate Mushaf line data
            var numberOfLines = _quranVerseLinesService.CalculateLines(
                surah.Number, 
                dto.FromVerse, 
                dto.ToVerse);

            var progressRecord = new ProgressRecord
            {
                StudentId = dto.StudentId,
                TeacherId = dto.TeacherId,
                HalaqaId = dto.HalaqaId,
                Date = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc),
                Type = dto.Type,
                SurahName = dto.SurahName,
                FromVerse = dto.FromVerse,
                ToVerse = dto.ToVerse,
                Quality = dto.Quality,
                Notes = dto.Notes,
                NumberLines = numberOfLines,
                CreatedAt = DateTime.UtcNow,
                AssociationId = _tenantService.CurrentAssociationId.Value
            };

            _context.ProgressRecords.Add(progressRecord);

            if (dto.Type == ProgressType.Memorization)
            {
                var (nextSurah, nextVerse) = _quranService.GetNextPosition(
                    student.MemorizationDirection,
                    surah.Number,
                    dto.ToVerse
                );

                student.CurrentSurahNumber = nextSurah;
                student.CurrentVerse = nextVerse;
                student.JuzMemorized = _quranService.CalculateJuzMemorized(
                    student.MemorizationDirection,
                    nextSurah,
                    nextVerse
                );
            }

            var progressDate = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc);
            var existingAttendance = await _context.Attendances
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync(a =>
                    a.StudentId == dto.StudentId &&
                    a.HalaqaId == dto.HalaqaId &&
                    a.Date.Date == progressDate);

            if (existingAttendance == null)
            {
                var attendance = new Attendance
                {
                    StudentId = dto.StudentId,
                    HalaqaId = dto.HalaqaId,
                    Date = progressDate,
                    Status = AttendanceStatus.Present,
                    Notes = "حضور تلقائي - تم تسجيل تقدم",
                    CreatedAt = DateTime.UtcNow,
                    AssociationId = _tenantService.CurrentAssociationId.Value
                };
                _context.Attendances.Add(attendance);
            }

            await _context.SaveChangesAsync();

            var savedRecord = await _context.ProgressRecords
                .Include(pr => pr.Student)
                .Include(pr => pr.Teacher)
                .Include(pr => pr.Halaqa)
                .AsSplitQuery()
                .OrderBy(pr => pr.Id)
                .FirstOrDefaultAsync(pr => pr.Id == progressRecord.Id);

            return MapToDto(savedRecord!);
        }

        public async Task<IEnumerable<ProgressRecordDto>> GetProgressByDateAsync(DateTime date, int? teacherId = null, List<int>? halaqaFilter = null)
        {
            var query = _context.ProgressRecords
                .Include(pr => pr.Student)
                .Include(pr => pr.Teacher)
                .Include(pr => pr.Halaqa)
                .AsSplitQuery()
                .Where(pr => pr.Date.Date == date.Date);

            if (teacherId.HasValue)
            {
                query = query.Where(pr => pr.TeacherId == teacherId.Value);
            }
            
            // Apply halaqa filter for HalaqaSupervisors
            if (halaqaFilter != null && halaqaFilter.Any())
            {
                query = query.Where(pr => halaqaFilter.Contains(pr.HalaqaId));
            }

            var records = await query.OrderBy(pr => pr.CreatedAt).ToListAsync();
            return records.Select(MapToDto);
        }

        public async Task<IEnumerable<ProgressRecordDto>> GetProgressByStudentAsync(int studentId, DateTime? fromDate = null)
        {
            var query = _context.ProgressRecords
                .Include(pr => pr.Student)
                .Include(pr => pr.Teacher)
                .Include(pr => pr.Halaqa)
                .AsSplitQuery()
                .Where(pr => pr.StudentId == studentId);

            if (fromDate.HasValue)
            {
                query = query.Where(pr => pr.Date >= fromDate.Value.Date);
            }

            var records = await query.OrderByDescending(pr => pr.Date).ToListAsync();
            return records.Select(MapToDto);
        }

        public async Task<DailyProgressSummaryDto> GetDailyProgressSummaryAsync(DateTime date, int? teacherId = null, List<int>? halaqaFilter = null)
        {
            var records = await GetProgressByDateAsync(date, teacherId, halaqaFilter);
            var recordsList = records.ToList();

            return new DailyProgressSummaryDto
            {
                Date = date.Date,
                TotalMemorization = recordsList.Count(r => r.Type == "Memorization"),
                TotalRevision = recordsList.Count(r => r.Type == "Revision"),
                UniqueStudents = recordsList.Select(r => r.StudentId).Distinct().Count(),
                Records = recordsList
            };
        }

        public async Task<StudentProgressSummaryDto> GetStudentProgressSummaryAsync(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                throw new InvalidOperationException("الطالب غير موجود");
            }

            var allRecords = await _context.ProgressRecords
                .Include(pr => pr.Teacher)
                .Include(pr => pr.Halaqa)
                .AsSplitQuery()
                .Where(pr => pr.StudentId == studentId)
                .ToListAsync();

            var recentRecords = allRecords
                .OrderByDescending(pr => pr.Date)
                .Take(10)
                .ToList();

            return new StudentProgressSummaryDto
            {
                StudentId = studentId,
                StudentName = $"{student.FirstName} {student.LastName}",
                TotalMemorized = allRecords.Count(r => r.Type == ProgressType.Memorization),
                TotalRevised = allRecords.Count(r => r.Type == ProgressType.Revision),
                LastProgressDate = allRecords.Any() ? allRecords.Max(r => r.Date) : DateTime.MinValue,
                AverageQuality = allRecords.Any() ? allRecords.Average(r => (int)r.Quality) : 0,
                RecentProgress = recentRecords.Select(MapToDto).ToList()
            };
        }

        public async Task<bool> DeleteProgressRecordAsync(int id, int userId)
        {
            var record = await _context.ProgressRecords.FindAsync(id);
            if (record == null)
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            if (user.Role == UserRole.Teacher && record.TeacherId != user.Teacher?.Id)
                return false;

            _context.ProgressRecords.Remove(record);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ProgressRecordDto?> GetLastProgressByTypeAsync(int studentId, int type)
        {
            // Validate type is within valid range (0, 1, or 2)
            if (type < 0 || type > 2)
            {
                return null;
            }

            var progressType = (ProgressType)type;

            var lastRecord = await _context.ProgressRecords
                .Include(pr => pr.Student)
                .Include(pr => pr.Teacher)
                .Include(pr => pr.Halaqa)
                .AsSplitQuery()
                .Where(pr => pr.StudentId == studentId && pr.Type == progressType)
                .OrderByDescending(pr => pr.Date)
                .ThenByDescending(pr => pr.CreatedAt)
                .FirstOrDefaultAsync();

            return lastRecord != null ? MapToDto(lastRecord) : null;
        }

        public async Task<bool> TeacherHasAccessToStudentAsync(int teacherId, int studentId)
        {
            // Check if there's an active assignment between this teacher and student
            var hasActiveAssignment = await _context.StudentHalaqat
                .AnyAsync(sh => sh.TeacherId == teacherId && sh.StudentId == studentId && sh.IsActive);

            return hasActiveAssignment;
        }

        private ProgressRecordDto MapToDto(ProgressRecord record)
        {
            return new ProgressRecordDto
            {
                Id = record.Id,
                StudentId = record.StudentId,
                StudentName = record.Student != null ? $"{record.Student.FirstName} {record.Student.LastName}" : "",
                TeacherId = record.TeacherId,
                TeacherName = record.Teacher?.FullName ?? "",
                HalaqaId = record.HalaqaId,
                HalaqaName = record.Halaqa?.Name ?? "",
                Date = record.Date,
                Type = record.Type switch
                {
                    ProgressType.Memorization => "حفظ جديد",
                    ProgressType.Revision => "مراجعة",
                    ProgressType.Consolidation => "التثبيت",
                    _ => "غير محدد"
                },
                SurahName = record.SurahName,
                FromVerse = record.FromVerse,
                ToVerse = record.ToVerse,
                Quality = record.Quality switch
                {
                    QualityRating.Excellent => "ممتاز",
                    QualityRating.VeryGood => "جيد جداً",
                    QualityRating.Good => "جيد",
                    QualityRating.Acceptable => "مقبول",
                    _ => ""
                },
                Notes = record.Notes,
                NumberLines = record.NumberLines,
                CreatedAt = record.CreatedAt
            };
        }
    }
}

