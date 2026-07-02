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

        public async Task<TodayTeacherAttendanceResponseDto> GetTodayAttendanceAsync(List<int>? halaqaFilter = null)
        {
            var today = TodayKsa();
            var dayOfWeek = (int)today.DayOfWeek;
            var arabicDayName = AppConstants.ArabicDayNames.GetDayName(today.DayOfWeek);

            var halaqatQuery = _context.Halaqat
                .AsNoTracking()
                .Where(h => h.IsActive);

            // Apply halaqa filter for HalaqaSupervisors
            if (halaqaFilter != null)
            {
                halaqatQuery = halaqatQuery.Where(h => halaqaFilter.Contains(h.Id));
            }

            var halaqat = await halaqatQuery
                .Include(h => h.HalaqaTeachers)
                    .ThenInclude(ht => ht.Teacher)
                .AsSplitQuery()
                .OrderBy(h => h.Name)
                .ToListAsync();

            var todayAttendance = await _context.TeacherAttendances
                .AsNoTracking()
                .Where(ta => ta.Date.Date == today)
                .ToListAsync();

            // Index attendance by (teacher, halaqa) so the per-teacher lookup below is O(1)
            // instead of scanning the whole list for every teacher (O(teachers × records)).
            var attendanceLookup = todayAttendance
                .GroupBy(ta => (ta.TeacherId, ta.HalaqaId))
                .ToDictionary(g => g.Key, g => g.First());

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
                    attendanceLookup.TryGetValue((teacher.Id, halaqa.Id), out var attendance);

                    teachersDto.Add(new TeacherWithAttendanceDto
                    {
                        TeacherId = teacher.Id,
                        TeacherName = teacher.FullName,
                        PhoneNumber = teacher.PhoneNumber,
                        AttendanceId = attendance?.Id,
                        Status = attendance?.Status,
                        CheckInTime = attendance?.CheckInTime,
                        CheckOutTime = attendance?.CheckOutTime,
                        WorkedHours = CalculateWorkedHours(attendance?.CheckInTime, attendance?.CheckOutTime),
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

            var today = TodayKsa();
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

                var isAbsent = entry.Status == AttendanceStatus.Absent;

                if (attendanceLookup.TryGetValue(new { entry.TeacherId, entry.HalaqaId }, out var existing))
                {
                    // Merge times instead of overwriting: an empty incoming time means "unchanged",
                    // so a supervisor saving from a page loaded before a teacher self checked in/out
                    // can't wipe that teacher's recorded times. Absent clears both.
                    var checkIn = isAbsent ? null : entry.CheckInTime ?? existing.CheckInTime;
                    var checkOut = isAbsent ? null : entry.CheckOutTime ?? existing.CheckOutTime;
                    ValidateTimes(checkIn, checkOut);

                    existing.Status = entry.Status;
                    existing.CheckInTime = checkIn;
                    existing.CheckOutTime = checkOut;
                    existing.Notes = entry.Notes;
                }
                else
                {
                    var checkIn = isAbsent ? null : entry.CheckInTime;
                    var checkOut = isAbsent ? null : entry.CheckOutTime;
                    ValidateTimes(checkIn, checkOut);

                    var newAttendance = new TeacherAttendance
                    {
                        TeacherId = entry.TeacherId,
                        HalaqaId = entry.HalaqaId,
                        Date = today,
                        Status = entry.Status,
                        CheckInTime = checkIn,
                        CheckOutTime = checkOut,
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
            // FirstOrDefaultAsync (not FindAsync) so the tenant global query filter applies
            // and a record from another association cannot be modified by id.
            var attendance = await _context.TeacherAttendances.FirstOrDefaultAsync(a => a.Id == id);
            if (attendance == null)
                return false;

            ValidateTimes(dto.CheckInTime, dto.CheckOutTime);

            attendance.Status = dto.Status;
            attendance.CheckInTime = dto.CheckInTime;
            attendance.CheckOutTime = dto.CheckOutTime;
            attendance.Notes = dto.Notes;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAttendanceAsync(int id)
        {
            // FirstOrDefaultAsync (not FindAsync) so the tenant global query filter applies.
            var attendance = await _context.TeacherAttendances.FirstOrDefaultAsync(a => a.Id == id);
            if (attendance == null)
                return false;

            _context.TeacherAttendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<TeacherAttendanceRecordDto>> GetTeacherAttendanceHistoryAsync(
            int teacherId, DateTime? fromDate = null, DateTime? toDate = null, List<int>? halaqaFilter = null)
        {
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
                throw new KeyNotFoundException(AppConstants.ErrorMessages.TeacherNotFound);

            var query = _context.TeacherAttendances
                .Where(ta => ta.TeacherId == teacherId)
                .Include(ta => ta.Halaqa)
                .AsQueryable();

            // Apply halaqa filter for HalaqaSupervisors
            if (halaqaFilter != null)
            {
                query = query.Where(ta => halaqaFilter.Contains(ta.HalaqaId));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(ta => ta.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(ta => ta.Date <= toDate.Value.Date);
            }

            var records = await query
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
                    CheckInTime = ta.CheckInTime,
                    CheckOutTime = ta.CheckOutTime,
                    Notes = ta.Notes,
                    CreatedAt = ta.CreatedAt
                })
                .ToListAsync();

            foreach (var r in records)
            {
                r.WorkedHours = CalculateWorkedHours(r.CheckInTime, r.CheckOutTime);
            }

            return records;
        }

        public async Task<MonthlyAttendanceReportDto> GetMonthlyReportAsync(int year, int month, List<int>? halaqaFilter = null)
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

            var teachersQuery = _context.Teachers
                .Include(t => t.HalaqaTeachers)
                    .ThenInclude(ht => ht.Halaqa)
                .AsSplitQuery()
                .Where(t => t.HalaqaTeachers.Any());

            // Filter teachers by halaqas for HalaqaSupervisors
            if (halaqaFilter != null)
            {
                teachersQuery = teachersQuery.Where(t => t.HalaqaTeachers.Any(ht => halaqaFilter.Contains(ht.HalaqaId)));
            }

            var teachers = await teachersQuery
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
                var totalHours = teacherAttendance
                    .Sum(ta => CalculateWorkedHours(ta.CheckInTime, ta.CheckOutTime) ?? 0);

                teacherSummaries.Add(new TeacherMonthlySummaryDto
                {
                    TeacherId = teacher.Id,
                    TeacherName = teacher.FullName,
                    PhoneNumber = teacher.PhoneNumber,
                    ExpectedDays = expectedDays,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LateDays = lateDays,
                    TotalHours = Math.Round(totalHours, 1)
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
                TotalHours = Math.Round(teacherSummaries.Sum(t => t.TotalHours), 1),
                Teachers = teacherSummaries
            };
        }

        public async Task<TeacherSelfAttendanceStatusDto> GetSelfAttendanceStatusAsync(int teacherId)
        {
            var today = TodayKsa();
            var dayOfWeek = (int)today.DayOfWeek;

            var activeHalaqat = await GetTeacherActiveHalaqatTodayAsync(teacherId, dayOfWeek);

            var halaqatDto = new List<TeacherSelfHalaqaAttendanceDto>();

            if (activeHalaqat.Count > 0)
            {
                var activeHalaqaIds = activeHalaqat.Select(h => h.Id).ToList();
                var todayRecords = await _context.TeacherAttendances
                    .Where(ta => ta.TeacherId == teacherId &&
                                 activeHalaqaIds.Contains(ta.HalaqaId) &&
                                 ta.Date.Date == today)
                    .ToListAsync();

                foreach (var halaqa in activeHalaqat)
                {
                    var record = todayRecords.FirstOrDefault(ta => ta.HalaqaId == halaqa.Id);
                    halaqatDto.Add(new TeacherSelfHalaqaAttendanceDto
                    {
                        HalaqaId = halaqa.Id,
                        HalaqaName = halaqa.Name,
                        TimeSlot = halaqa.TimeSlot,
                        Status = record?.Status,
                        CheckedIn = record?.Status == AttendanceStatus.Present,
                        CheckedOut = record?.CheckOutTime != null,
                        CheckInTime = record?.CheckInTime,
                        CheckOutTime = record?.CheckOutTime
                    });
                }
            }

            return new TeacherSelfAttendanceStatusDto
            {
                Date = today,
                DayName = AppConstants.ArabicDayNames.GetDayName(today.DayOfWeek),
                // Aggregates: "done" only when every active halaqa is handled.
                CheckedIn = halaqatDto.Count > 0 && halaqatDto.All(h => h.CheckedIn),
                CheckedOut = halaqatDto.Count > 0 && halaqatDto.All(h => h.CheckedOut),
                HasActiveHalaqaToday = halaqatDto.Count > 0,
                HalaqatCount = halaqatDto.Count,
                Halaqat = halaqatDto
            };
        }

        public async Task<TeacherSelfCheckInResultDto> SelfCheckInAsync(int teacherId, int halaqaId)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            var today = TodayKsa();
            var dayOfWeek = (int)today.DayOfWeek;

            var activeHalaqaIds = await GetTeacherActiveHalaqaIdsTodayAsync(teacherId, dayOfWeek);
            if (!activeHalaqaIds.Contains(halaqaId))
            {
                throw new InvalidOperationException("هذه الحلقة غير نشطة لك اليوم");
            }

            // A record may already exist (e.g. set by a supervisor) — don't overwrite it.
            var existing = await _context.TeacherAttendances
                .FirstOrDefaultAsync(ta => ta.TeacherId == teacherId &&
                                           ta.HalaqaId == halaqaId &&
                                           ta.Date.Date == today);

            if (existing != null)
            {
                // Don't pretend an absent teacher checked in — tell them the supervisor
                // marked them absent so they can get it corrected.
                if (existing.Status == AttendanceStatus.Absent)
                {
                    throw new InvalidOperationException(AppConstants.ErrorMessages.MarkedAbsentBySupervisor);
                }

                // Present/Late set by a supervisor without a time: stamp the real arrival now.
                var stampedNow = false;
                if (existing.CheckInTime == null && existing.CheckOutTime == null)
                {
                    existing.CheckInTime = NowKsaTime();
                    await _context.SaveChangesAsync();
                    stampedNow = true;
                }

                return new TeacherSelfCheckInResultDto
                {
                    CheckedIn = true,
                    RecordsCreated = 0,
                    Message = stampedNow
                        ? AppConstants.SuccessMessages.TeacherCheckedIn
                        : AppConstants.SuccessMessages.TeacherAlreadyCheckedIn
                };
            }

            _context.TeacherAttendances.Add(new TeacherAttendance
            {
                TeacherId = teacherId,
                HalaqaId = halaqaId,
                Date = today,
                Status = AttendanceStatus.Present,
                CheckInTime = NowKsaTime(),
                CreatedAt = DateTime.UtcNow,
                AssociationId = _tenantService.CurrentAssociationId.Value
            });
            await _context.SaveChangesAsync();

            return new TeacherSelfCheckInResultDto
            {
                CheckedIn = true,
                RecordsCreated = 1,
                Message = AppConstants.SuccessMessages.TeacherCheckedIn
            };
        }

        public async Task<TeacherSelfCheckInResultDto> SelfCheckOutAsync(int teacherId, int halaqaId)
        {
            var today = TodayKsa();
            var dayOfWeek = (int)today.DayOfWeek;

            var activeHalaqaIds = await GetTeacherActiveHalaqaIdsTodayAsync(teacherId, dayOfWeek);
            if (!activeHalaqaIds.Contains(halaqaId))
            {
                throw new InvalidOperationException("هذه الحلقة غير نشطة لك اليوم");
            }

            // The teacher must already be checked in (present) for this halaqa before checking out.
            var record = await _context.TeacherAttendances
                .FirstOrDefaultAsync(ta => ta.TeacherId == teacherId &&
                                           ta.HalaqaId == halaqaId &&
                                           ta.Date.Date == today &&
                                           ta.Status == AttendanceStatus.Present);

            if (record == null)
            {
                throw new InvalidOperationException(AppConstants.ErrorMessages.NotCheckedInYet);
            }

            if (record.CheckOutTime != null)
            {
                return new TeacherSelfCheckInResultDto
                {
                    CheckedIn = true,
                    RecordsCreated = 0,
                    Message = AppConstants.SuccessMessages.TeacherAlreadyCheckedOut
                };
            }

            var nowTime = NowKsaTime();
            // <= not <: times are truncated to minutes, so a same-minute check-out would
            // store checkOut == checkIn — a record ValidateTimes rejects on every later save.
            if (record.CheckInTime != null && nowTime <= record.CheckInTime.Value)
            {
                throw new InvalidOperationException(AppConstants.ErrorMessages.InvalidDepartureTime);
            }

            record.CheckOutTime = nowTime;
            await _context.SaveChangesAsync();

            return new TeacherSelfCheckInResultDto
            {
                CheckedIn = true,
                RecordsCreated = 1,
                Message = AppConstants.SuccessMessages.TeacherCheckedOut
            };
        }

        /// <summary>
        /// Returns the IDs of the teacher's halaqat that are active (scheduled) today.
        /// </summary>
        private async Task<List<int>> GetTeacherActiveHalaqaIdsTodayAsync(int teacherId, int dayOfWeek)
        {
            var halaqat = await GetTeacherActiveHalaqatTodayAsync(teacherId, dayOfWeek);
            return halaqat.Select(h => h.Id).ToList();
        }

        /// <summary>
        /// Returns the teacher's halaqat (id, name, time slot) that are active (scheduled) today.
        /// </summary>
        private async Task<List<(int Id, string Name, string? TimeSlot)>> GetTeacherActiveHalaqatTodayAsync(int teacherId, int dayOfWeek)
        {
            var halaqat = await _context.HalaqaTeachers
                .Where(ht => ht.TeacherId == teacherId && ht.Halaqa.IsActive)
                .Select(ht => new { ht.HalaqaId, ht.Halaqa.Name, ht.Halaqa.TimeSlot, ht.Halaqa.ActiveDays })
                .ToListAsync();

            return halaqat
                .Where(h => IsHalaqaActiveToday(h.ActiveDays, dayOfWeek))
                .GroupBy(h => h.HalaqaId)
                .Select(g => (g.Key, g.First().Name, g.First().TimeSlot))
                .ToList();
        }

        private static int CalculateExpectedWorkingDays(List<Halaqa> halaqat, DateTime startDate, DateTime endDate)
        {
            var activeDaysSet = new HashSet<int>();

            foreach (var halaqa in halaqat)
            {
                activeDaysSet.UnionWith(ActiveDaysHelper.Parse(halaqa.ActiveDays));
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

        private static bool IsHalaqaActiveToday(string? activeDays, int dayOfWeek) =>
            ActiveDaysHelper.IsActiveOn(activeDays, dayOfWeek);

        private static readonly TimeZoneInfo KsaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");

        /// <summary>Current time of day in KSA local time, truncated to minutes.</summary>
        private static TimeOnly NowKsaTime()
        {
            var ksaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, KsaTimeZone);
            return new TimeOnly(ksaNow.Hour, ksaNow.Minute);
        }

        /// <summary>
        /// Today's date in KSA local time. Must match the zone of <see cref="NowKsaTime"/>:
        /// the UTC date flips at 3:00 AM KSA, so using UtcNow.Date put saves made between
        /// midnight and 3 AM on the previous day ("الحلقة غير نشطة اليوم").
        /// </summary>
        private static DateTime TodayKsa() =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, KsaTimeZone).Date;

        /// <summary>Worked hours between arrival and departure, or null if either is missing/invalid.</summary>
        private static double? CalculateWorkedHours(TimeOnly? checkIn, TimeOnly? checkOut)
        {
            if (checkIn == null || checkOut == null)
                return null;

            var diff = checkOut.Value.ToTimeSpan() - checkIn.Value.ToTimeSpan();
            if (diff <= TimeSpan.Zero)
                return null;

            return Math.Round(diff.TotalHours, 2);
        }

        /// <summary>Rejects a departure time that is not strictly after the arrival time.</summary>
        private static void ValidateTimes(TimeOnly? checkIn, TimeOnly? checkOut)
        {
            // A departure time with no arrival time is invalid.
            if (checkOut != null && checkIn == null)
            {
                throw new InvalidOperationException(AppConstants.ErrorMessages.NotCheckedInYet);
            }

            if (checkIn != null && checkOut != null && checkOut.Value <= checkIn.Value)
            {
                throw new InvalidOperationException(AppConstants.ErrorMessages.InvalidDepartureTime);
            }
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

