using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Services.Implementations
{
    public class TeacherAttendanceService : ITeacherAttendanceService
    {
        private readonly AppDbContext _context;
        private readonly ITenantService _tenantService;

        public TeacherAttendanceService(AppDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<TodayTeacherAttendanceResponseDto> GetTodayAttendanceAsync()
        {
            var today = DateTime.UtcNow.Date;
            var dayOfWeek = (int)today.DayOfWeek;
            var arabicDayName = AppConstants.ArabicDayNames.GetDayName(today.DayOfWeek);

            var halaqat = await _context.Halaqat
                .Where(h => h.IsActive)
                .Include(h => h.HalaqaTeachers)
                    .ThenInclude(ht => ht.Teacher)
                .AsSplitQuery()
                .OrderBy(h => h.Name)
                .ToListAsync();

            var todayAttendance = await _context.TeacherAttendances
                .Where(ta => ta.Date.Date == today)
                .ToListAsync();

            var halaqatDto = new List<HalaqaTeachersAttendanceDto>();
            var totalTeachers = 0;
            var presentCount = 0;
            var absentCount = 0;
            var lateCount = 0;

            foreach (var halaqa in halaqat)
            {
                var isActiveToday = IsHalaqaActiveToday(halaqa.ActiveDays, dayOfWeek);

                var teachersDto = new List<TeacherWithAttendanceDto>();

                foreach (var halaqaTeacher in halaqa.HalaqaTeachers)
                {
                    var teacher = halaqaTeacher.Teacher;
                    var attendance = todayAttendance
                        .FirstOrDefault(ta => ta.TeacherId == teacher.Id && ta.HalaqaId == halaqa.Id);

                    teachersDto.Add(new TeacherWithAttendanceDto
                    {
                        TeacherId = teacher.Id,
                        TeacherName = teacher.FullName,
                        PhoneNumber = teacher.PhoneNumber,
                        AttendanceId = attendance?.Id,
                        Status = attendance?.Status,
                        Notes = attendance?.Notes
                    });

                    if (isActiveToday)
                    {
                        totalTeachers++;
                        if (attendance != null)
                        {
                            switch (attendance.Status)
                            {
                                case AttendanceStatus.Present:
                                    presentCount++;
                                    break;
                                case AttendanceStatus.Absent:
                                    absentCount++;
                                    break;
                                case AttendanceStatus.Late:
                                    lateCount++;
                                    break;
                            }
                        }
                    }
                }

                halaqatDto.Add(new HalaqaTeachersAttendanceDto
                {
                    HalaqaId = halaqa.Id,
                    HalaqaName = halaqa.Name,
                    Location = halaqa.Location,
                    TimeSlot = halaqa.TimeSlot,
                    IsActiveToday = isActiveToday,
                    Teachers = teachersDto
                });
            }

            return new TodayTeacherAttendanceResponseDto
            {
                Date = today,
                DayName = arabicDayName,
                TotalTeachers = totalTeachers,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                LateCount = lateCount,
                Halaqat = halaqatDto
            };
        }

        public async Task<bool> SaveBulkAttendanceAsync(BulkTeacherAttendanceDto dto)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            var today = DateTime.UtcNow.Date;
            var dayOfWeek = (int)today.DayOfWeek;

            var entriesByHalaqa = dto.Attendance.GroupBy(a => a.HalaqaId);

            // 1. Validate all halaqat in bulk
            var targetHalaqaIds = entriesByHalaqa.Select(g => g.Key).ToList();
            var halaqat = await _context.Halaqat
                .Where(h => targetHalaqaIds.Contains(h.Id))
                .ToListAsync();

            if (halaqat.Count != targetHalaqaIds.Count)
            {
                throw new KeyNotFoundException(AppConstants.ErrorMessages.HalaqaNotFound);
            }

            foreach (var halaqa in halaqat)
            {
                if (!IsHalaqaActiveToday(halaqa.ActiveDays, dayOfWeek))
                {
                    throw new InvalidOperationException($"الحلقة '{halaqa.Name}' غير نشطة اليوم");
                }
            }

            // 2. Load all HalaqaTeachers assignments in bulk to validate
            var teacherIds = dto.Attendance.Select(a => a.TeacherId).Distinct().ToList();
            var assignments = await _context.HalaqaTeachers
                .Where(ht => targetHalaqaIds.Contains(ht.HalaqaId) && teacherIds.Contains(ht.TeacherId))
                .ToListAsync();

            var assignmentLookup = assignments
                .ToLookup(ht => new { ht.TeacherId, ht.HalaqaId });

            // 3. Load all existing attendance records for today in bulk
            var existingAttendances = await _context.TeacherAttendances
                .Where(ta => targetHalaqaIds.Contains(ta.HalaqaId) &&
                             teacherIds.Contains(ta.TeacherId) &&
                             ta.Date.Date == today)
                .ToListAsync();

            var attendanceLookup = existingAttendances
                .ToDictionary(ta => new { ta.TeacherId, ta.HalaqaId });

            // 4. Process entries
            foreach (var entry in dto.Attendance)
            {
                if (!assignmentLookup.Contains(new { entry.TeacherId, entry.HalaqaId }))
                {
                    throw new InvalidOperationException($"المعلم ذو الرقم {entry.TeacherId} غير معين في الحلقة {entry.HalaqaId}");
                }

                if (attendanceLookup.TryGetValue(new { entry.TeacherId, entry.HalaqaId }, out var existing))
                {
                    existing.Status = entry.Status;
                    existing.Notes = entry.Notes;
                }
                else
                {
                    var newAttendance = new TeacherAttendance
                    {
                        TeacherId = entry.TeacherId,
                        HalaqaId = entry.HalaqaId,
                        Date = today,
                        Status = entry.Status,
                        Notes = entry.Notes,
                        CreatedAt = DateTime.UtcNow,
                        AssociationId = _tenantService.CurrentAssociationId.Value
                    };
                    _context.TeacherAttendances.Add(newAttendance);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAttendanceAsync(int id, UpdateTeacherAttendanceDto dto)
        {
            var attendance = await _context.TeacherAttendances.FindAsync(id);
            if (attendance == null)
                return false;

            attendance.Status = dto.Status;
            attendance.Notes = dto.Notes;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAttendanceAsync(int id)
        {
            var attendance = await _context.TeacherAttendances.FindAsync(id);
            if (attendance == null)
                return false;

            _context.TeacherAttendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<TeacherAttendanceRecordDto>> GetTeacherAttendanceHistoryAsync(
            int teacherId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
                throw new KeyNotFoundException(AppConstants.ErrorMessages.TeacherNotFound);

            var query = _context.TeacherAttendances
                .Where(ta => ta.TeacherId == teacherId)
                .Include(ta => ta.Halaqa)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(ta => ta.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(ta => ta.Date <= toDate.Value.Date);
            }

            return await query
                .OrderByDescending(ta => ta.Date)
                .Select(ta => new TeacherAttendanceRecordDto
                {
                    Id = ta.Id,
                    TeacherId = ta.TeacherId,
                    TeacherName = teacher.FullName,
                    HalaqaId = ta.HalaqaId,
                    HalaqaName = ta.Halaqa.Name,
                    Date = ta.Date,
                    Status = GetStatusArabicName(ta.Status),
                    Notes = ta.Notes,
                    CreatedAt = ta.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<MonthlyAttendanceReportDto> GetMonthlyReportAsync(int year, int month)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentException(AppConstants.ErrorMessages.InvalidMonth);
            }

            if (year < 2020 || year > 2100)
            {
                throw new ArgumentException(AppConstants.ErrorMessages.InvalidYear);
            }

            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);
            var monthName = AppConstants.ArabicMonthNames.GetMonthName(month);

            var teachers = await _context.Teachers
                .Include(t => t.HalaqaTeachers)
                    .ThenInclude(ht => ht.Halaqa)
                .AsSplitQuery()
                .Where(t => t.HalaqaTeachers.Any())
                .OrderBy(t => t.FullName)
                .ToListAsync();

            var attendanceRecords = await _context.TeacherAttendances
                .Where(ta => ta.Date >= startDate && ta.Date <= endDate)
                .ToListAsync();

            var teacherSummaries = new List<TeacherMonthlySummaryDto>();

            foreach (var teacher in teachers)
            {
                var expectedDays = CalculateExpectedWorkingDays(
                    teacher.HalaqaTeachers.Select(ht => ht.Halaqa).ToList(),
                    startDate,
                    endDate
                );

                var teacherAttendance = attendanceRecords
                    .Where(ta => ta.TeacherId == teacher.Id)
                    .ToList();

                var presentDays = teacherAttendance.Count(ta => ta.Status == AttendanceStatus.Present);
                var lateDays = teacherAttendance.Count(ta => ta.Status == AttendanceStatus.Late);
                var absentDays = teacherAttendance.Count(ta => ta.Status == AttendanceStatus.Absent);

                teacherSummaries.Add(new TeacherMonthlySummaryDto
                {
                    TeacherId = teacher.Id,
                    TeacherName = teacher.FullName,
                    PhoneNumber = teacher.PhoneNumber,
                    ExpectedDays = expectedDays,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LateDays = lateDays
                });
            }

            return new MonthlyAttendanceReportDto
            {
                Year = year,
                Month = month,
                MonthName = monthName,
                TotalTeachers = teacherSummaries.Count,
                TotalExpectedDays = teacherSummaries.Sum(t => t.ExpectedDays),
                TotalPresentDays = teacherSummaries.Sum(t => t.PresentDays + t.LateDays),
                TotalAbsentDays = teacherSummaries.Sum(t => t.AbsentDays),
                Teachers = teacherSummaries
            };
        }

        private static int CalculateExpectedWorkingDays(List<Halaqa> halaqat, DateTime startDate, DateTime endDate)
        {
            var activeDaysSet = new HashSet<int>();

            foreach (var halaqa in halaqat)
            {
                if (string.IsNullOrEmpty(halaqa.ActiveDays))
                {
                    for (int i = 0; i <= 6; i++)
                    {
                        activeDaysSet.Add(i);
                    }
                }
                else
                {
                    var days = halaqa.ActiveDays.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var day in days)
                    {
                        if (int.TryParse(day.Trim(), out int dayNum))
                        {
                            activeDaysSet.Add(dayNum);
                        }
                    }
                }
            }

            int workingDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (activeDaysSet.Contains((int)date.DayOfWeek))
                {
                    workingDays++;
                }
            }

            return workingDays;
        }

        private static bool IsHalaqaActiveToday(string? activeDays, int dayOfWeek)
        {
            if (string.IsNullOrEmpty(activeDays))
            {
                return true;
            }

            var days = activeDays.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return days.Any(d => int.TryParse(d.Trim(), out int day) && day == dayOfWeek);
        }

        private static string GetStatusArabicName(AttendanceStatus status)
        {
            return status switch
            {
                AttendanceStatus.Present => "حاضر",
                AttendanceStatus.Absent => "غائب",
                AttendanceStatus.Late => "متأخر",
                _ => "غير محدد"
            };
        }
    }
}

