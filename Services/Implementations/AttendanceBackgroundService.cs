using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

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

        public async Task<int> MarkAbsentForMissingAttendanceAsync(DateTime date)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var targetDate = date.Date;
            var dayOfWeek = (int)targetDate.DayOfWeek;
            var totalCreated = 0;

            _logger.LogInformation("Processing absent records for date: {Date}, DayOfWeek: {DayOfWeek}",
                targetDate.ToString("yyyy-MM-dd"), dayOfWeek);

            var halaqat = await context.Halaqat
                .Where(h => h.IsActive && h.ActiveDays != null)
                .ToListAsync();

            foreach (var halaqa in halaqat)
            {
                var activeDays = ParseActiveDays(halaqa.ActiveDays);

                if (!activeDays.Contains(dayOfWeek))
                {
                    _logger.LogDebug("Skipping Halaqa {HalaqaId} ({HalaqaName}) - not an active day",
                        halaqa.Id, halaqa.Name);
                    continue;
                }

                _logger.LogInformation("Processing Halaqa {HalaqaId} ({HalaqaName}) for date {Date}",
                    halaqa.Id, halaqa.Name, targetDate.ToString("yyyy-MM-dd"));

                var studentIds = await context.StudentHalaqat
                    .Where(sh => sh.HalaqaId == halaqa.Id && sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .ToListAsync();

                if (!studentIds.Any())
                {
                    _logger.LogDebug("No active students in Halaqa {HalaqaId}", halaqa.Id);
                    continue;
                }

                var studentsWithAttendance = await context.Attendances
                    .Where(a => a.Date == targetDate && studentIds.Contains(a.StudentId))
                    .Select(a => a.StudentId)
                    .ToListAsync();

                var studentsWithoutAttendance = studentIds
                    .Except(studentsWithAttendance)
                    .ToList();

                if (!studentsWithoutAttendance.Any())
                {
                    _logger.LogDebug("All students in Halaqa {HalaqaId} have attendance records", halaqa.Id);
                    continue;
                }

                foreach (var studentId in studentsWithoutAttendance)
                {
                    var attendance = new Attendance
                    {
                        StudentId = studentId,
                        HalaqaId = halaqa.Id,
                        Date = targetDate,
                        Status = AttendanceStatus.Absent,
                        Notes = "تم التسجيل تلقائياً - غياب",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Attendances.Add(attendance);
                    totalCreated++;
                }

                if (studentsWithoutAttendance.Any())
                {
                    try
                    {
                        await context.SaveChangesAsync();
                        _logger.LogInformation("Created {Count} absent records for Halaqa {HalaqaId}",
                            studentsWithoutAttendance.Count, halaqa.Id);
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_Attendances_StudentId_Date") == true)
                    {
                        _logger.LogDebug("Some students in Halaqa {HalaqaId} already have attendance records (duplicate key)", halaqa.Id);
                        context.ChangeTracker.Clear();
                    }
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

