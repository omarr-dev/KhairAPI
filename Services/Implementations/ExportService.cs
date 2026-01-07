using ClosedXML.Excel;
using KhairAPI.Data;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace KhairAPI.Services.Implementations
{
    public class ExportService : IExportService
    {
        private readonly AppDbContext _context;
        private readonly IQuranService _quranService;

        public ExportService(AppDbContext context, IQuranService quranService)
        {
            _context = context;
            _quranService = quranService;
        }

        public async Task<byte[]> ExportStudentsToExcelAsync(int? halaqaId = null, int? teacherId = null)
        {
            var query = _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .AsSplitQuery()
                .AsQueryable();

            if (halaqaId.HasValue)
            {
                query = query.Where(s => s.StudentHalaqat.Any(sh => sh.HalaqaId == halaqaId.Value && sh.IsActive));
            }

            if (teacherId.HasValue)
            {
                query = query.Where(s => s.StudentHalaqat.Any(sh => sh.TeacherId == teacherId.Value && sh.IsActive));
            }

            var students = await query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("الطلاب");
            worksheet.RightToLeft = true;

            var headers = new[] { "الرقم", "الاسم الكامل", "ولي الأمر", "هاتف ولي الأمر", "الحلقة", "المعلم", "اتجاه الحفظ", "السورة الحالية", "الآية", "الأجزاء المحفوظة", "تاريخ التسجيل" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 2;
            foreach (var student in students)
            {
                var activeAssignment = student.StudentHalaqat.FirstOrDefault(sh => sh.IsActive);
                var surahName = _quranService.GetSurahByNumber(student.CurrentSurahNumber)?.Name ?? "";
                var direction = student.MemorizationDirection == MemorizationDirection.Forward ? "من الفاتحة" : "من الناس";

                worksheet.Cell(row, 1).Value = student.Id;
                worksheet.Cell(row, 2).Value = $"{student.FirstName} {student.LastName}";
                worksheet.Cell(row, 3).Value = student.GuardianName ?? "";
                worksheet.Cell(row, 4).Value = student.GuardianPhone ?? "";
                worksheet.Cell(row, 5).Value = activeAssignment?.Halaqa?.Name ?? "";
                worksheet.Cell(row, 6).Value = activeAssignment?.Teacher?.FullName ?? "";
                worksheet.Cell(row, 7).Value = direction;
                worksheet.Cell(row, 8).Value = surahName;
                worksheet.Cell(row, 9).Value = student.CurrentVerse;
                worksheet.Cell(row, 10).Value = Math.Round(student.JuzMemorized, 2);
                worksheet.Cell(row, 11).Value = student.CreatedAt.ToString("yyyy-MM-dd");
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportTeachersToExcelAsync(int? halaqaId = null)
        {
            var query = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.HalaqaTeachers)
                    .ThenInclude(ht => ht.Halaqa)
                .Include(t => t.StudentHalaqat)
                .AsSplitQuery()
                .AsQueryable();

            if (halaqaId.HasValue)
            {
                query = query.Where(t => t.HalaqaTeachers.Any(ht => ht.HalaqaId == halaqaId.Value));
            }

            var teachers = await query.OrderBy(t => t.FullName).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("المعلمين");
            worksheet.RightToLeft = true;

            var headers = new[] { "الرقم", "الاسم الكامل", "البريد الإلكتروني", "رقم الهاتف", "المؤهل", "عدد الحلقات", "عدد الطلاب", "الحلقات", "تاريخ الانضمام" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 2;
            foreach (var teacher in teachers)
            {
                var halaqatNames = string.Join(", ", teacher.HalaqaTeachers.Select(ht => ht.Halaqa.Name));

                worksheet.Cell(row, 1).Value = teacher.Id;
                worksheet.Cell(row, 2).Value = teacher.FullName;
                worksheet.Cell(row, 3).Value = teacher.User?.PhoneNumber ?? "";
                worksheet.Cell(row, 4).Value = teacher.PhoneNumber ?? "";
                worksheet.Cell(row, 5).Value = teacher.Qualification ?? "";
                worksheet.Cell(row, 6).Value = teacher.HalaqaTeachers.Count;
                worksheet.Cell(row, 7).Value = teacher.StudentHalaqat.Count(sh => sh.IsActive);
                worksheet.Cell(row, 8).Value = halaqatNames;
                worksheet.Cell(row, 9).Value = teacher.JoinDate.ToString("yyyy-MM-dd");
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportAttendanceReportToExcelAsync(DateTime fromDate, DateTime toDate, int? halaqaId = null)
        {
            var query = _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Halaqa)
                .AsSplitQuery()
                .Where(a => a.Date >= fromDate && a.Date <= toDate);

            if (halaqaId.HasValue)
            {
                query = query.Where(a => a.HalaqaId == halaqaId.Value);
            }

            var attendances = await query.OrderBy(a => a.Date).ThenBy(a => a.Student.FirstName).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("تقرير الحضور");
            worksheet.RightToLeft = true;

            var headers = new[] { "التاريخ", "الطالب", "الحلقة", "الحالة", "ملاحظات" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 2;
            foreach (var attendance in attendances)
            {
                var status = attendance.Status switch
                {
                    AttendanceStatus.Present => "حاضر",
                    AttendanceStatus.Absent => "غائب",
                    AttendanceStatus.Late => "متأخر",
                    _ => "غير محدد"
                };

                worksheet.Cell(row, 1).Value = attendance.Date.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 2).Value = $"{attendance.Student.FirstName} {attendance.Student.LastName}";
                worksheet.Cell(row, 3).Value = attendance.Halaqa?.Name ?? "";
                worksheet.Cell(row, 4).Value = status;
                worksheet.Cell(row, 5).Value = attendance.Notes ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            var summarySheet = workbook.Worksheets.Add("ملخص");
            summarySheet.RightToLeft = true;

            summarySheet.Cell(1, 1).Value = "ملخص تقرير الحضور";
            summarySheet.Cell(1, 1).Style.Font.Bold = true;
            summarySheet.Cell(2, 1).Value = $"الفترة: {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}";

            summarySheet.Cell(4, 1).Value = "الحالة";
            summarySheet.Cell(4, 2).Value = "العدد";
            summarySheet.Cell(4, 3).Value = "النسبة";
            summarySheet.Row(4).Style.Font.Bold = true;

            var total = attendances.Count;
            var present = attendances.Count(a => a.Status == AttendanceStatus.Present);
            var absent = attendances.Count(a => a.Status == AttendanceStatus.Absent);
            var late = attendances.Count(a => a.Status == AttendanceStatus.Late);

            summarySheet.Cell(5, 1).Value = "حاضر";
            summarySheet.Cell(5, 2).Value = present;
            summarySheet.Cell(5, 3).Value = total > 0 ? $"{(double)present / total * 100:F1}%" : "0%";

            summarySheet.Cell(6, 1).Value = "غائب";
            summarySheet.Cell(6, 2).Value = absent;
            summarySheet.Cell(6, 3).Value = total > 0 ? $"{(double)absent / total * 100:F1}%" : "0%";

            summarySheet.Cell(7, 1).Value = "متأخر";
            summarySheet.Cell(7, 2).Value = late;
            summarySheet.Cell(7, 3).Value = total > 0 ? $"{(double)late / total * 100:F1}%" : "0%";

            summarySheet.Cell(8, 1).Value = "الإجمالي";
            summarySheet.Cell(8, 2).Value = total;
            summarySheet.Row(8).Style.Font.Bold = true;

            summarySheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportHalaqaPerformanceToExcelAsync(DateTime fromDate, DateTime toDate)
        {
            var halaqat = await _context.Halaqat
                .Where(h => h.IsActive)
                .Include(h => h.StudentHalaqat)
                .Include(h => h.HalaqaTeachers)
                .AsSplitQuery()
                .ToListAsync();

            var attendances = await _context.Attendances
                .Where(a => a.Date >= fromDate && a.Date <= toDate)
                .ToListAsync();

            var progressRecords = await _context.ProgressRecords
                .Where(p => p.Date >= fromDate && p.Date <= toDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("أداء الحلقات");
            worksheet.RightToLeft = true;

            var headers = new[] { "الحلقة", "عدد الطلاب", "عدد المعلمين", "نسبة الحضور", "عدد التسميعات", "حفظ جديد", "مراجعة", "إجمالي الآيات" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 2;
            foreach (var halaqa in halaqat.OrderBy(h => h.Name))
            {
                var halaqaAttendance = attendances.Where(a => a.HalaqaId == halaqa.Id).ToList();
                var halaqaProgress = progressRecords.Where(p => p.HalaqaId == halaqa.Id).ToList();

                var attendanceRate = halaqaAttendance.Any()
                    ? (double)halaqaAttendance.Count(a => a.Status == AttendanceStatus.Present) / halaqaAttendance.Count * 100
                    : 0;

                var memorization = halaqaProgress.Count(p => p.Type == ProgressType.Memorization);
                var revision = halaqaProgress.Count(p => p.Type == ProgressType.Revision);
                var totalVerses = halaqaProgress.Sum(p => p.ToVerse - p.FromVerse + 1);

                worksheet.Cell(row, 1).Value = halaqa.Name;
                worksheet.Cell(row, 2).Value = halaqa.StudentHalaqat.Count(sh => sh.IsActive);
                worksheet.Cell(row, 3).Value = halaqa.HalaqaTeachers.Count;
                worksheet.Cell(row, 4).Value = $"{attendanceRate:F1}%";
                worksheet.Cell(row, 5).Value = halaqaProgress.Count;
                worksheet.Cell(row, 6).Value = memorization;
                worksheet.Cell(row, 7).Value = revision;
                worksheet.Cell(row, 8).Value = totalVerses;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportTeacherPerformanceToExcelAsync(DateTime fromDate, DateTime toDate)
        {
            var teachers = await _context.Teachers
                .Include(t => t.StudentHalaqat)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("أداء المعلمين");
            worksheet.RightToLeft = true;

            var headers = new[] { "المعلم", "عدد الطلاب", "نسبة حضور الطلاب", "عدد التسميعات", "حفظ جديد", "مراجعة", "متوسط الجودة" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 2;
            foreach (var teacher in teachers.OrderBy(t => t.FullName))
            {
                var studentIds = teacher.StudentHalaqat
                    .Where(sh => sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .ToList();

                if (!studentIds.Any()) continue;

                var studentAttendance = await _context.Attendances
                    .Where(a => a.Date >= fromDate && a.Date <= toDate && studentIds.Contains(a.StudentId))
                    .ToListAsync();

                var teacherProgress = await _context.ProgressRecords
                    .Where(p => p.Date >= fromDate && p.Date <= toDate && p.TeacherId == teacher.Id)
                    .ToListAsync();

                var attendanceRate = studentAttendance.Any()
                    ? (double)studentAttendance.Count(a => a.Status == AttendanceStatus.Present) / studentAttendance.Count * 100
                    : 0;

                var avgQuality = teacherProgress.Any()
                    ? teacherProgress.Average(p => 4 - (int)p.Quality)
                    : 0;

                var qualityText = avgQuality switch
                {
                    >= 3.5 => "ممتاز",
                    >= 2.5 => "جيد جداً",
                    >= 1.5 => "جيد",
                    >= 0.5 => "مقبول",
                    _ => "غير محدد"
                };

                worksheet.Cell(row, 1).Value = teacher.FullName;
                worksheet.Cell(row, 2).Value = studentIds.Count;
                worksheet.Cell(row, 3).Value = $"{attendanceRate:F1}%";
                worksheet.Cell(row, 4).Value = teacherProgress.Count;
                worksheet.Cell(row, 5).Value = teacherProgress.Count(p => p.Type == ProgressType.Memorization);
                worksheet.Cell(row, 6).Value = teacherProgress.Count(p => p.Type == ProgressType.Revision);
                worksheet.Cell(row, 7).Value = qualityText;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportTeacherAttendanceReportAsync(int year, int month)
        {
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

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("حضور المعلمين");
            worksheet.RightToLeft = true;

            worksheet.Cell(1, 1).Value = $"تقرير حضور المعلمين - {monthName} {year}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 7).Merge();

            var headers = new[] { "م", "المعلم", "الهاتف", "الأيام المتوقعة", "أيام الحضور", "أيام الغياب", "أيام التأخر", "نسبة الحضور" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                worksheet.Cell(3, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 4;
            int totalExpectedDays = 0;
            int totalPresentDays = 0;
            int totalAbsentDays = 0;
            int totalLateDays = 0;

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

                var attendanceRate = expectedDays > 0
                    ? (double)(presentDays + lateDays) / expectedDays * 100
                    : 0;

                worksheet.Cell(row, 1).Value = row - 3;
                worksheet.Cell(row, 2).Value = teacher.FullName;
                worksheet.Cell(row, 3).Value = teacher.PhoneNumber ?? "";
                worksheet.Cell(row, 4).Value = expectedDays;
                worksheet.Cell(row, 5).Value = presentDays + lateDays;
                worksheet.Cell(row, 6).Value = absentDays;
                worksheet.Cell(row, 7).Value = lateDays;
                worksheet.Cell(row, 8).Value = $"{attendanceRate:F1}%";

                if (absentDays > 0)
                {
                    worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.LightCoral;
                }

                totalExpectedDays += expectedDays;
                totalPresentDays += presentDays + lateDays;
                totalAbsentDays += absentDays;
                totalLateDays += lateDays;

                row++;
            }

            worksheet.Cell(row, 1).Value = "";
            worksheet.Cell(row, 2).Value = "الإجمالي";
            worksheet.Cell(row, 3).Value = "";
            worksheet.Cell(row, 4).Value = totalExpectedDays;
            worksheet.Cell(row, 5).Value = totalPresentDays;
            worksheet.Cell(row, 6).Value = totalAbsentDays;
            worksheet.Cell(row, 7).Value = totalLateDays;
            worksheet.Cell(row, 8).Value = totalExpectedDays > 0
                ? $"{(double)totalPresentDays / totalExpectedDays * 100:F1}%"
                : "0%";
            worksheet.Row(row).Style.Font.Bold = true;
            worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightGray;

            var dataRange = worksheet.Range(3, 1, row, 8);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.Columns().AdjustToContents();

            var notesSheet = workbook.Worksheets.Add("ملاحظات");
            notesSheet.RightToLeft = true;
            notesSheet.Cell(1, 1).Value = "ملاحظات هامة:";
            notesSheet.Cell(1, 1).Style.Font.Bold = true;
            notesSheet.Cell(2, 1).Value = "• أيام الحضور تشمل أيام التأخر (لا يُخصم من الراتب)";
            notesSheet.Cell(3, 1).Value = "• أيام الغياب هي الأيام التي يُخصم عليها من الراتب";
            notesSheet.Cell(4, 1).Value = "• الأيام المتوقعة محسوبة بناءً على أيام نشاط الحلقات المخصصة للمعلم";
            notesSheet.Cell(5, 1).Value = $"• تاريخ التقرير: {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
            notesSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
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
    }
}

