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
        /// Calculates achievement for a student on a specific date.
        /// Uses pre-stored NumberLines values for efficient aggregation.
        /// </summary>
        public async Task<TargetAchievementDto?> CalculateAchievementAsync(int studentId, DateTime date)
        {
            var targetDate = date.Date;
            var nextDay = targetDate.AddDays(1);

            // Parallel database calls for better performance
            var targetTask = _context.StudentTargets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            // Use SQL aggregation with stored NumberLines
            var progressTask = _context.ProgressRecords
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && p.Date >= targetDate && p.Date < nextDay)
                .GroupBy(p => p.Type)
                .Select(g => new { Type = g.Key, TotalLines = g.Sum(p => p.NumberLines) })
                .ToListAsync();

            await Task.WhenAll(targetTask, progressTask);

            var target = targetTask.Result;
            if (target == null)
            {
                return null; // No target set for this student
            }

            var progressByType = progressTask.Result;

            // Extract totals by type
            var memorizationLines = progressByType
                .FirstOrDefault(p => p.Type == ProgressType.Memorization)?.TotalLines ?? 0;
            var revisionLines = progressByType
                .FirstOrDefault(p => p.Type == ProgressType.Revision)?.TotalLines ?? 0;
            var consolidationLines = progressByType
                .FirstOrDefault(p => p.Type == ProgressType.Consolidation)?.TotalLines ?? 0;

            // Convert lines to pages for revision/consolidation (1 page = 15 lines in Medina Mushaf)
            const double LinesPerPage = 15.0;

            return new TargetAchievementDto
            {
                StudentId = studentId,
                Date = targetDate,
                MemorizationLinesTarget = target.MemorizationLinesTarget,
                RevisionPagesTarget = target.RevisionPagesTarget,
                ConsolidationPagesTarget = target.ConsolidationPagesTarget,
                MemorizationLinesAchieved = (int)Math.Round(memorizationLines),
                RevisionPagesAchieved = (int)Math.Ceiling(revisionLines / LinesPerPage),
                ConsolidationPagesAchieved = (int)Math.Ceiling(consolidationLines / LinesPerPage)
            };
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
