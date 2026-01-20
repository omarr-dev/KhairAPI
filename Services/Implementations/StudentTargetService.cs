using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    /// <summary>
    /// Internal record for aggregated progress data
    /// </summary>
    internal record ProgressAggregation(int StudentId, DateTime Date, ProgressType Type, double TotalLines);

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

            // Calculate streaks
            var (currentStreak, bestStreak) = CalculateStreaks(dailyAchievements);

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
        /// Gets achievement history for multiple students in a single batch call.
        /// Optimized for the "My Students" page.
        /// </summary>
        public async Task<Dictionary<int, AchievementHistoryDto>> GetAchievementHistoryBatchAsync(
            IEnumerable<int> studentIds, 
            DateTime startDate, 
            DateTime endDate)
        {
            var studentIdList = studentIds.ToList();
            if (!studentIdList.Any())
            {
                return new Dictionary<int, AchievementHistoryDto>();
            }

            // Normalize dates to UTC
            var start = NormalizeToUtc(startDate.Date);
            var end = NormalizeToUtc(endDate.Date);
            
            // Security: Limit range to 90 days
            if ((end - start).TotalDays > 90)
            {
                throw new InvalidOperationException("لا يمكن طلب أكثر من 90 يوماً في طلب واحد.");
            }

            // Get all targets for the students in one query
            var targets = await _context.StudentTargets
                .AsNoTracking()
                .Where(t => studentIdList.Contains(t.StudentId))
                .ToDictionaryAsync(t => t.StudentId);

            // Get all progress records for all students in the date range
            var nextDayAfterEnd = end.AddDays(1);
            var allProgress = await _context.ProgressRecords
                .AsNoTracking()
                .Where(p => studentIdList.Contains(p.StudentId) && p.Date >= start && p.Date < nextDayAfterEnd)
                .GroupBy(p => new { p.StudentId, p.Date.Date, p.Type })
                .Select(g => new ProgressAggregation(
                    g.Key.StudentId,
                    g.Key.Date, 
                    g.Key.Type, 
                    g.Sum(p => p.NumberLines) 
                ))
                .ToListAsync();

            // Group progress by student
            var progressByStudent = allProgress
                .GroupBy(p => p.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            const double LinesPerPage = 15.0;
            var result = new Dictionary<int, AchievementHistoryDto>();

            foreach (var studentId in studentIdList)
            {
                targets.TryGetValue(studentId, out var target);
                var studentProgress = progressByStudent.TryGetValue(studentId, out var progress) 
                    ? progress 
                    : new List<ProgressAggregation>();

                if (target == null)
                {
                    result[studentId] = new AchievementHistoryDto
                    {
                        StudentId = studentId,
                        StartDate = start,
                        EndDate = end,
                        HasTarget = false
                    };
                    continue;
                }

                // Build daily achievements for this student
                var dailyAchievements = new List<TargetAchievementDto>();
                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    var dayRecords = studentProgress.Where(p => p.Date.Date == date.Date).ToList();
                    
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

                var (currentStreak, bestStreak) = CalculateStreaks(dailyAchievements);
                var daysWithTarget = dailyAchievements.Where(a => a.IsTargetMet).ToList();
                var daysActive = dailyAchievements.Where(a => 
                    a.MemorizationLinesAchieved > 0 || 
                    a.RevisionPagesAchieved > 0 || 
                    a.ConsolidationPagesAchieved > 0).ToList();

                result[studentId] = new AchievementHistoryDto
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

            return result;
        }

        /// <summary>
        /// Calculate current and best streaks from daily achievements.
        /// Current streak counts from the most recent day backwards.
        /// </summary>
        private static (int CurrentStreak, int BestStreak) CalculateStreaks(List<TargetAchievementDto> achievements)
        {
            if (!achievements.Any()) return (0, 0);

            // Sort by date descending for current streak calculation
            var sorted = achievements.OrderByDescending(a => a.Date).ToList();
            
            // Current streak: count from most recent day (or today) backwards
            int currentStreak = 0;
            foreach (var day in sorted)
            {
                if (day.IsTargetMet)
                    currentStreak++;
                else
                    break; // Streak broken
            }

            // Best streak: find longest consecutive run
            var sortedAsc = achievements.OrderBy(a => a.Date).ToList();
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
                    tempStreak = 0;
                }
            }

            return (currentStreak, bestStreak);
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
