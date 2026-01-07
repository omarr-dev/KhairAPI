using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using Hangfire;

namespace KhairAPI.Services.Implementations
{
    public class AttendanceBackgroundService : IAttendanceBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AttendanceBackgroundService> _logger;

        public AttendanceBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AttendanceBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 3600)]
        [JobDisplayName("تسجيل غياب الطلاب التلقائي")]
        public async Task<int> MarkAbsentForMissingAttendanceAsync(DateTime? date = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // If date is null, use the current date in the local timezone (usually what's expected for daily jobs)
            var targetDate = (date ?? DateTime.UtcNow).Date;
            var dayOfWeek = (int)targetDate.DayOfWeek;
            var totalCreated = 0;

            _logger.LogInformation("Processing absent records for date: {Date}, DayOfWeek: {DayOfWeek}",
                targetDate.ToString("yyyy-MM-dd"), dayOfWeek);

            // 1. Get all active halaqat and their active days
            var halaqat = await context.Halaqat
                .Where(h => h.IsActive && h.ActiveDays != null)
                .ToListAsync();

            // 2. Filter halaqat that are active today (in-memory parsing of ActiveDays string)
            var targetHalaqaIds = halaqat
                .Where(h => ParseActiveDays(h.ActiveDays).Contains(dayOfWeek))
                .Select(h => h.Id)
                .ToList();

            if (!targetHalaqaIds.Any())
            {
                _logger.LogInformation("No active halaqat for today: {Date}", targetDate.ToString("yyyy-MM-dd"));
                return 0;
            }

            // 3. Find students in these halaqat who don't have an attendance record for target date
            // Using a single query to find missing attendance records to avoid N+1 issues
            var studentsMissingAttendance = await context.StudentHalaqat
                .Where(sh => targetHalaqaIds.Contains(sh.HalaqaId) && sh.IsActive)
                .Where(sh => !context.Attendances.Any(a => a.Date == targetDate && a.StudentId == sh.StudentId))
                .Select(sh => new { sh.StudentId, sh.HalaqaId })
                .ToListAsync();

            if (!studentsMissingAttendance.Any())
            {
                _logger.LogInformation("All students already have attendance records for {Date}", targetDate.ToString("yyyy-MM-dd"));
                return 0;
            }

            // 4. Group by StudentId to ensure each student gets only one attendance record
            // (Database constraint IX_Attendances_StudentId_Date allows only one per day)
            var uniqueStudentsMissingAttendance = studentsMissingAttendance
                .GroupBy(x => x.StudentId)
                .Select(g => g.First())
                .ToList();

            foreach (var record in uniqueStudentsMissingAttendance)
            {
                var attendance = new Attendance
                {
                    StudentId = record.StudentId,
                    HalaqaId = record.HalaqaId,
                    Date = targetDate,
                    Status = AttendanceStatus.Absent,
                    Notes = "تم التسجيل تلقائياً - غياب",
                    CreatedAt = DateTime.UtcNow
                };

                context.Attendances.Add(attendance);
                totalCreated++;
            }

            if (totalCreated > 0)
            {
                try
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Successfully created {Count} absent records for {Date}",
                        totalCreated, targetDate.ToString("yyyy-MM-dd"));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_Attendances_StudentId_Date") == true)
                {
                    _logger.LogWarning("Some attendance records were already created by another process for {Date}. Skipping duplicates.",
                        targetDate.ToString("yyyy-MM-dd"));
                }
            }

            if (totalCreated > 0)
            {
                _logger.LogInformation("Total absent records created for {Date}: {Count}",
                    targetDate.ToString("yyyy-MM-dd"), totalCreated);
            }
            else
            {
                _logger.LogInformation("No absent records needed for {Date}", targetDate.ToString("yyyy-MM-dd"));
            }

            return totalCreated;
        }

        private List<int> ParseActiveDays(string? activeDays)
        {
            if (string.IsNullOrWhiteSpace(activeDays))
                return new List<int>();

            return activeDays
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => int.TryParse(d.Trim(), out var day) ? day : -1)
                .Where(d => d >= 0 && d <= 6)
                .ToList();
        }
    }
}

