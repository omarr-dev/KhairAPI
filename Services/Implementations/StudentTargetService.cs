using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    /// <summary>
    /// Simplified service for managing student daily targets.
    /// Achievements are calculated on-demand from ProgressRecords - no background job needed.
    /// </summary>
    public class StudentTargetService : IStudentTargetService
    {
        private readonly AppDbContext _context;
        private readonly ITenantService _tenantService;

        public StudentTargetService(AppDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<StudentTargetDto?> GetTargetAsync(int studentId)
        {
            var target = await _context.StudentTargets
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            return target == null ? null : MapToDto(target);
        }

        public async Task<StudentTargetDto> SetTargetAsync(int studentId, SetStudentTargetDto dto)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            // Check if student exists
            var studentExists = await _context.Students.AnyAsync(s => s.Id == studentId);
            if (!studentExists)
            {
                throw new InvalidOperationException("الطالب غير موجود.");
            }

            var target = await _context.StudentTargets
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            if (target == null)
            {
                // Create new target
                target = new StudentTarget
                {
                    StudentId = studentId,
                    MemorizationLinesTarget = dto.MemorizationLinesTarget,
                    RevisionPagesTarget = dto.RevisionPagesTarget,
                    ConsolidationPagesTarget = dto.ConsolidationPagesTarget,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AssociationId = _tenantService.CurrentAssociationId.Value
                };
                _context.StudentTargets.Add(target);
            }
            else
            {
                // Update existing target
                target.MemorizationLinesTarget = dto.MemorizationLinesTarget;
                target.RevisionPagesTarget = dto.RevisionPagesTarget;
                target.ConsolidationPagesTarget = dto.ConsolidationPagesTarget;
                target.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return MapToDto(target);
        }

        public async Task<int> BulkSetTargetAsync(BulkSetTargetDto dto)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            // Determine which students to update
            List<int> studentIds;

            if (dto.StudentIds != null && dto.StudentIds.Any())
            {
                // Specific student IDs provided - validate they belong to current tenant
                studentIds = dto.StudentIds;
                
                // Security: Validate that all student IDs belong to the current tenant
                var validStudentIds = await _context.Students
                    .Where(s => studentIds.Contains(s.Id))  // Query filter will auto-apply tenant
                    .Select(s => s.Id)
                    .ToListAsync();
                    
                if (validStudentIds.Count != studentIds.Count)
                {
                    throw new InvalidOperationException("بعض الطلاب غير موجودين أو لا ينتمون لهذه الجمعية.");
                }
            }
            else if (dto.TeacherId.HasValue)
            {
                // All students of a specific teacher
                studentIds = await _context.StudentHalaqat
                    .Where(sh => sh.TeacherId == dto.TeacherId.Value && sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .Distinct()
                    .ToListAsync();
            }
            else if (dto.HalaqaId.HasValue)
            {
                // All students in a specific halaqa
                studentIds = await _context.StudentHalaqat
                    .Where(sh => sh.HalaqaId == dto.HalaqaId.Value && sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                throw new InvalidOperationException("يجب تحديد الطلاب أو المعلم أو الحلقة.");
            }

            if (!studentIds.Any())
            {
                return 0;
            }

            // Get existing targets for these students
            var existingTargets = await _context.StudentTargets
                .Where(t => studentIds.Contains(t.StudentId))
                .ToListAsync();

            var existingStudentIds = existingTargets.Select(t => t.StudentId).ToHashSet();
            var now = DateTime.UtcNow;

            // Update existing targets
            foreach (var target in existingTargets)
            {
                target.MemorizationLinesTarget = dto.MemorizationLinesTarget;
                target.RevisionPagesTarget = dto.RevisionPagesTarget;
                target.ConsolidationPagesTarget = dto.ConsolidationPagesTarget;
                target.UpdatedAt = now;
            }

            // Create new targets for students without existing targets
            var newTargets = studentIds
                .Where(id => !existingStudentIds.Contains(id))
                .Select(id => new StudentTarget
                {
                    StudentId = id,
                    MemorizationLinesTarget = dto.MemorizationLinesTarget,
                    RevisionPagesTarget = dto.RevisionPagesTarget,
                    ConsolidationPagesTarget = dto.ConsolidationPagesTarget,
                    CreatedAt = now,
                    UpdatedAt = now,
                    AssociationId = _tenantService.CurrentAssociationId.Value
                })
                .ToList();

            _context.StudentTargets.AddRange(newTargets);
            await _context.SaveChangesAsync();

            return studentIds.Count;
        }

        /// <summary>
        /// Gets achievement history for a student within a date range.
        /// Includes daily achievements, streak information, and summary statistics.
        /// Optimized with a single batched query for the entire date range.
        /// </summary>
        public async Task<AchievementHistoryDto?> GetAchievementHistoryAsync(int studentId, DateTime startDate, DateTime endDate)
        {
            // Normalize dates to UTC
            var start = NormalizeToUtc(startDate.Date);
            var end = NormalizeToUtc(endDate.Date);
            
            // Security: Limit range to 90 days to prevent abuse
            if ((end - start).TotalDays > 90)
            {
                throw new InvalidOperationException("لا يمكن طلب أكثر من 90 يوماً في طلب واحد.");
            }

            // Get target first (quick single-row lookup)
            var target = await _context.StudentTargets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            if (target == null)
            {
                return new AchievementHistoryDto
                {
                    StudentId = studentId,
                    StartDate = start,
                    EndDate = end,
                    HasTarget = false
                };
            }

            // Fetch the student's halaqa active days to correctly skip non-active days in streak calculation
            var halaqaActiveDaysStr = await _context.StudentHalaqat
                .AsNoTracking()
                .Where(sh => sh.StudentId == studentId && sh.IsActive)
                .Select(sh => sh.Halaqa.ActiveDays)
                .FirstOrDefaultAsync();
            var activeDaysSet = ParseActiveDays(halaqaActiveDaysStr);

            // Get all progress records in the date range with a single query
            var nextDayAfterEnd = end.AddDays(1);
            var progressRecords = await _context.ProgressRecords
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && p.Date >= start && p.Date < nextDayAfterEnd)
                .GroupBy(p => new { p.Date.Date, p.Type })
                .Select(g => new { 
                    Date = g.Key.Date, 
                    Type = g.Key.Type, 
                    TotalLines = g.Sum(p => p.NumberLines) 
                })
                .ToListAsync();

            // Build daily achievements
            var dailyAchievements = new List<TargetAchievementDto>();
            const double LinesPerPage = 15.0;

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var dayRecords = progressRecords.Where(p => p.Date.Date == date.Date).ToList();
                
                var memLines = dayRecords.FirstOrDefault(p => p.Type == ProgressType.Memorization)?.TotalLines ?? 0;
                var revLines = dayRecords.FirstOrDefault(p => p.Type == ProgressType.Revision)?.TotalLines ?? 0;
                var conLines = dayRecords.FirstOrDefault(p => p.Type == ProgressType.Consolidation)?.TotalLines ?? 0;

                dailyAchievements.Add(new TargetAchievementDto
                {
                    StudentId = studentId,
                    Date = date,
                    MemorizationLinesTarget = target.MemorizationLinesTarget,
                    RevisionPagesTarget = target.RevisionPagesTarget,
                    ConsolidationPagesTarget = target.ConsolidationPagesTarget,
                    MemorizationLinesAchieved = (int)Math.Round(memLines),
                    RevisionPagesAchieved = (int)Math.Ceiling(revLines / LinesPerPage),
                    ConsolidationPagesAchieved = (int)Math.Ceiling(conLines / LinesPerPage)
                });
            }

            // Calculate streaks (non-active halaqa days are skipped, not treated as streak-breakers)
            var (currentStreak, bestStreak) = CalculateStreaks(dailyAchievements, activeDaysSet);

            // Calculate summary statistics
            var daysWithTarget = dailyAchievements.Where(a => a.IsTargetMet).ToList();
            var daysActive = dailyAchievements.Where(a => 
                a.MemorizationLinesAchieved > 0 || 
                a.RevisionPagesAchieved > 0 || 
                a.ConsolidationPagesAchieved > 0).ToList();

            return new AchievementHistoryDto
            {
                StudentId = studentId,
                StartDate = start,
                EndDate = end,
                DailyAchievements = dailyAchievements,
                CurrentStreak = currentStreak,
                BestStreak = bestStreak,
                LastAchievedDate = daysWithTarget.OrderByDescending(a => a.Date).FirstOrDefault()?.Date,
                TotalDaysTargetMet = daysWithTarget.Count,
                TotalDaysActive = daysActive.Count,
                HasTarget = true
            };
        }

        /// <summary>
        /// Calculate current and best streaks from daily achievements.
        /// Current streak counts backwards from the most recent day where target was met.
        /// A streak is consecutive active-halaqa days where the target was achieved.
        /// Non-active days (e.g. weekend for a Mon–Fri halaqa) are skipped — they do not break a streak.
        /// </summary>
        private static (int CurrentStreak, int BestStreak) CalculateStreaks(
            List<TargetAchievementDto> achievements, HashSet<int> activeDaysOfWeek)
        {
            // Filter to only active halaqa days — non-active days are neutral, not streak-breakers
            var filtered = activeDaysOfWeek.Count > 0
                ? achievements.Where(a => activeDaysOfWeek.Contains((int)a.Date.DayOfWeek)).ToList()
                : achievements;

            if (!filtered.Any()) return (0, 0);

            // Sort by date descending for current streak calculation
            var sorted = filtered.OrderByDescending(a => a.Date).ToList();
            
            // Current streak: Find the most recent day with target met, then count backwards
            int currentStreak = 0;
            bool foundFirstTargetMet = false;
            
            foreach (var day in sorted)
            {
                if (day.IsTargetMet)
                {
                    foundFirstTargetMet = true;
                    currentStreak++;
                }
                else if (foundFirstTargetMet)
                {
                    // Once we've started counting and hit a non-target day, streak is broken
                    break;
                }
                // If we haven't found a target-met day yet, keep looking backwards
            }

            // Best streak: Find longest consecutive run of target-met days
            var sortedAsc = filtered.OrderBy(a => a.Date).ToList();
            int bestStreak = 0;
            int tempStreak = 0;
            
            foreach (var day in sortedAsc)
            {
                if (day.IsTargetMet)
                {
                    tempStreak++;
                    bestStreak = Math.Max(bestStreak, tempStreak);
                }
                else
                {
                    // Target not met - reset streak
                    tempStreak = 0;
                }
            }

            // Ensure current streak doesn't exceed best streak
            bestStreak = Math.Max(bestStreak, currentStreak);

            return (currentStreak, bestStreak);
        }

        private static HashSet<int> ParseActiveDays(string? activeDays)
        {
            if (string.IsNullOrWhiteSpace(activeDays))
                return new HashSet<int> { 0, 1, 2, 3, 4 }; // Default to Sun-Thu if not set

            var days = activeDays
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => int.TryParse(d.Trim(), out var day) ? day : -1)
                .Where(d => d >= 0 && d <= 6);

            return new HashSet<int>(days);
        }

        private static DateTime NormalizeToUtc(DateTime date)
        {
            return date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)
                : date.Date;
        }

        private static StudentTargetDto MapToDto(StudentTarget target)
        {
            return new StudentTargetDto
            {
                StudentId = target.StudentId,
                MemorizationLinesTarget = target.MemorizationLinesTarget,
                RevisionPagesTarget = target.RevisionPagesTarget,
                ConsolidationPagesTarget = target.ConsolidationPagesTarget,
                UpdatedAt = target.UpdatedAt
            };
        }
    }
}
