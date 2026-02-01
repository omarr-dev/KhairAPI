using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using static KhairAPI.Core.Extensions.CacheKeys;

namespace KhairAPI.Services.Implementations
{
    public class ProgressService : IProgressService
    {
        private readonly AppDbContext _context;
        private readonly IQuranService _quranService;
        private readonly ITenantService _tenantService;
        private readonly IQuranVerseLinesService _quranVerseLinesService;
        private readonly ICacheService _cache;

        public ProgressService(
            AppDbContext context, 
            IQuranService quranService, 
            ITenantService tenantService,
            IQuranVerseLinesService quranVerseLinesService,
            ICacheService cache)
        {
            _context = context;
            _quranService = quranService;
            _tenantService = tenantService;
            _quranVerseLinesService = quranVerseLinesService;
            _cache = cache;
        }

        public async Task<ProgressRecordDto> CreateProgressRecordAsync(CreateProgressRecordDto dto, bool isSupervisorRecording = false)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            StudentHalaqa? studentAssignment;

            if (isSupervisorRecording)
            {
                // For supervisors, find the student's actual teacher in this halaqa
                studentAssignment = await _context.StudentHalaqat
                    .OrderBy(sh => sh.StudentId).ThenBy(sh => sh.HalaqaId).ThenBy(sh => sh.TeacherId)
                    .FirstOrDefaultAsync(sh =>
                        sh.StudentId == dto.StudentId &&
                        sh.HalaqaId == dto.HalaqaId &&
                        sh.IsActive);

                if (studentAssignment != null)
                {
                    // Update dto with the actual teacher ID
                    dto.TeacherId = studentAssignment.TeacherId;
                }
            }
            else
            {
                studentAssignment = await _context.StudentHalaqat
                    .OrderBy(sh => sh.StudentId).ThenBy(sh => sh.HalaqaId).ThenBy(sh => sh.TeacherId)
                    .FirstOrDefaultAsync(sh =>
                        sh.StudentId == dto.StudentId &&
                        sh.HalaqaId == dto.HalaqaId &&
                        sh.TeacherId == dto.TeacherId &&
                        sh.IsActive);
            }

            if (studentAssignment == null)
            {
                throw new InvalidOperationException("الطالب غير مسجل في هذه الحلقة");
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

            // Update streak if target was met for today
            await UpdateStreakOnProgressAsync(dto.StudentId, progressDate, dto.HalaqaId);

            InvalidateStatisticsCache();

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
            
            InvalidateStatisticsCache();
            
            return true;
        }

        private void InvalidateStatisticsCache()
        {
            _cache.Remove(SystemWideStats);
            _cache.RemoveByPrefix("supervisor_dashboard");
            _cache.RemoveByPrefix("halaqa_ranking");
            _cache.RemoveByPrefix("teacher_ranking");
            _cache.RemoveByPrefix("dashboard_stats");
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

        /// <summary>
        /// Updates streak when progress is recorded. Checks if all set targets are met for the day
        /// and increments streak accordingly.
        /// </summary>
        private async Task UpdateStreakOnProgressAsync(int studentId, DateTime progressDate, int halaqaId)
        {
            // Get student's target
            var studentTarget = await _context.StudentTargets
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            if (studentTarget == null)
            {
                // No target set - cannot have streak
                return;
            }

            // Check if already counted today (prevent duplicate streak increments)
            if (studentTarget.LastStreakDate.HasValue &&
                studentTarget.LastStreakDate.Value.Date == progressDate.Date)
            {
                return;
            }

            // Get halaqa to check if today is an active day
            var halaqa = await _context.Halaqat
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == halaqaId);

            if (halaqa == null)
            {
                return;
            }

            // Check if progress date is an active day for this halaqa
            var activeDays = ParseActiveDays(halaqa.ActiveDays);
            if (!activeDays.Contains((int)progressDate.DayOfWeek))
            {
                // Not an active day - don't update streak
                return;
            }

            // Get all progress records for this student on this date
            var dayProgress = await _context.ProgressRecords
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && p.Date.Date == progressDate.Date)
                .ToListAsync();

            // Calculate total progress by type
            const double LinesPerPage = 15.0;
            var memorizationLines = dayProgress
                .Where(p => p.Type == ProgressType.Memorization)
                .Sum(p => p.NumberLines);
            var revisionLines = dayProgress
                .Where(p => p.Type == ProgressType.Revision)
                .Sum(p => p.NumberLines);
            var consolidationLines = dayProgress
                .Where(p => p.Type == ProgressType.Consolidation)
                .Sum(p => p.NumberLines);

            // Check if all set targets are met
            bool memorizationMet = !studentTarget.MemorizationLinesTarget.HasValue ||
                                   memorizationLines >= studentTarget.MemorizationLinesTarget.Value;
            bool revisionMet = !studentTarget.RevisionPagesTarget.HasValue ||
                               revisionLines >= (studentTarget.RevisionPagesTarget.Value * LinesPerPage);
            bool consolidationMet = !studentTarget.ConsolidationPagesTarget.HasValue ||
                                    consolidationLines >= (studentTarget.ConsolidationPagesTarget.Value * LinesPerPage);

            // At least one target must be set, and all set targets must be met
            bool hasAnyTarget = studentTarget.MemorizationLinesTarget.HasValue ||
                               studentTarget.RevisionPagesTarget.HasValue ||
                               studentTarget.ConsolidationPagesTarget.HasValue;

            if (!hasAnyTarget || !memorizationMet || !revisionMet || !consolidationMet)
            {
                // Target not met - don't update streak
                return;
            }

            // Target met! Update streak
            // Check if this continues a streak (last active day) or starts a new one
            bool continuesStreak = false;

            if (studentTarget.LastStreakDate.HasValue && studentTarget.CurrentStreak > 0)
            {
                // Find the previous active day
                var checkDate = progressDate.AddDays(-1);
                while (checkDate >= progressDate.AddDays(-7)) // Look back up to 7 days
                {
                    if (activeDays.Contains((int)checkDate.DayOfWeek))
                    {
                        // This was the last active day
                        if (studentTarget.LastStreakDate.Value.Date == checkDate.Date)
                        {
                            continuesStreak = true;
                        }
                        break;
                    }
                    checkDate = checkDate.AddDays(-1);
                }
            }

            if (continuesStreak)
            {
                studentTarget.CurrentStreak++;
            }
            else
            {
                // Start new streak
                studentTarget.CurrentStreak = 1;
            }

            // Update longest streak if exceeded
            if (studentTarget.CurrentStreak > studentTarget.LongestStreak)
            {
                studentTarget.LongestStreak = studentTarget.CurrentStreak;
            }

            studentTarget.LastStreakDate = progressDate;
            studentTarget.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private static List<int> ParseActiveDays(string? activeDays)
        {
            if (string.IsNullOrWhiteSpace(activeDays))
                return new List<int> { 0, 1, 2, 3, 4 }; // Default to Sun-Thu if not set

            return activeDays
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => int.TryParse(d.Trim(), out var day) ? day : -1)
                .Where(d => d >= 0 && d <= 6)
                .ToList();
        }
    }
}

