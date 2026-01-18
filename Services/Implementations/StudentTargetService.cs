using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    /// <summary>
    /// Implementation of IStudentTargetService for managing student daily targets.
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
                // Specific student IDs provided
                studentIds = dto.StudentIds;
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

        public async Task<List<TargetAchievementDto>> GetAchievementHistoryAsync(int studentId, AchievementHistoryFilter? filter = null)
        {
            var query = _context.TargetAchievements
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.Date)
                .AsQueryable();

            if (filter?.FromDate.HasValue == true)
            {
                query = query.Where(a => a.Date >= filter.FromDate.Value);
            }

            if (filter?.ToDate.HasValue == true)
            {
                query = query.Where(a => a.Date <= filter.ToDate.Value);
            }

            var achievements = await query.Take(30).ToListAsync();

            return achievements.Select(a => new TargetAchievementDto
            {
                StudentId = a.StudentId,
                Date = a.Date,
                MemorizationLinesTarget = a.MemorizationLinesTarget,
                RevisionPagesTarget = a.RevisionPagesTarget,
                ConsolidationPagesTarget = a.ConsolidationPagesTarget,
                MemorizationLinesAchieved = a.MemorizationLinesAchieved,
                RevisionPagesAchieved = a.RevisionPagesAchieved,
                ConsolidationPagesAchieved = a.ConsolidationPagesAchieved
            }).ToList();
        }

        public async Task RecordDailyAchievementsAsync()
        {
            // This method should be called by a background job at end of each day
            // It calculates the actual achievements from ProgressRecords for the day
            // OPTIMIZED: Uses batch queries instead of per-student queries to avoid N+1 problem
            
            var today = DateTime.UtcNow.Date;

            // Get all students with targets (single query)
            var studentsWithTargets = await _context.StudentTargets
                .IgnoreQueryFilters() // Background job needs all tenants
                .ToListAsync();

            if (!studentsWithTargets.Any())
            {
                return; // No targets to process
            }

            var studentIds = studentsWithTargets.Select(t => t.StudentId).ToList();

            // Batch fetch: Get all existing achievement records for today (single query)
            var existingAchievementsList = await _context.TargetAchievements
                .IgnoreQueryFilters()
                .Where(a => studentIds.Contains(a.StudentId) && a.Date == today)
                .Select(a => a.StudentId)
                .ToListAsync();
            var existingAchievements = existingAchievementsList.ToHashSet();

            // Filter out students who already have achievements recorded
            var studentsToProcess = studentsWithTargets
                .Where(t => !existingAchievements.Contains(t.StudentId))
                .ToList();

            if (!studentsToProcess.Any())
            {
                return; // All students already have achievements recorded
            }

            var studentIdsToProcess = studentsToProcess.Select(t => t.StudentId).ToList();

            // Batch fetch: Get all progress records for today for all students (single query)
            var allTodayProgress = await _context.ProgressRecords
                .IgnoreQueryFilters()
                .Where(p => studentIdsToProcess.Contains(p.StudentId) && p.Date.Date == today)
                .GroupBy(p => p.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    MemorizationVerses = g.Where(p => p.Type == ProgressType.Memorization)
                        .Sum(p => p.ToVerse - p.FromVerse + 1),
                    RevisionVerses = g.Where(p => p.Type == ProgressType.Revision)
                        .Sum(p => p.ToVerse - p.FromVerse + 1),
                    ConsolidationVerses = g.Where(p => p.Type == ProgressType.Consolidation)
                        .Sum(p => p.ToVerse - p.FromVerse + 1)
                })
                .ToDictionaryAsync(x => x.StudentId);

            // Create achievement records for all students
            var achievements = new List<TargetAchievement>();
            
            foreach (var target in studentsToProcess)
            {
                // Get progress or default to zeros
                var progress = allTodayProgress.GetValueOrDefault(target.StudentId);
                
                var memorizationVerses = progress?.MemorizationVerses ?? 0;
                var revisionVerses = progress?.RevisionVerses ?? 0;
                var consolidationVerses = progress?.ConsolidationVerses ?? 0;

                // Convert verses to lines/pages (approximate: 1 page ≈ 15 verses, 1 line ≈ 1 verse)
                var memorizationLines = memorizationVerses;
                var revisionPages = (int)Math.Ceiling(revisionVerses / 15.0);
                var consolidationPages = (int)Math.Ceiling(consolidationVerses / 15.0);

                achievements.Add(new TargetAchievement
                {
                    StudentId = target.StudentId,
                    Date = today,
                    MemorizationLinesTarget = target.MemorizationLinesTarget,
                    RevisionPagesTarget = target.RevisionPagesTarget,
                    ConsolidationPagesTarget = target.ConsolidationPagesTarget,
                    MemorizationLinesAchieved = memorizationLines,
                    RevisionPagesAchieved = revisionPages,
                    ConsolidationPagesAchieved = consolidationPages,
                    CreatedAt = DateTime.UtcNow,
                    AssociationId = target.AssociationId
                });
            }

            // Batch insert all achievements (single SaveChanges call)
            _context.TargetAchievements.AddRange(achievements);
            await _context.SaveChangesAsync();
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
