using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;

        public AttendanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AttendanceRecordDto> CreateAttendanceAsync(CreateAttendanceDto dto)
        {
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == dto.StudentId && a.Date.Date == dto.Date.Date);

            if (existingAttendance != null)
            {
                existingAttendance.Status = dto.Status;
                existingAttendance.Notes = dto.Notes;
            }
            else
            {
                var attendance = new Attendance
                {
                    StudentId = dto.StudentId,
                    HalaqaId = dto.HalaqaId,
                    Date = dto.Date.Date,
                    Status = dto.Status,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Attendances.Add(attendance);
            }

            await _context.SaveChangesAsync();

            var savedAttendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Halaqa)
                .FirstOrDefaultAsync(a => a.StudentId == dto.StudentId && a.Date.Date == dto.Date.Date);

            return MapToDto(savedAttendance!);
        }

        public async Task<bool> CreateBulkAttendanceAsync(BulkAttendanceDto dto)
        {
            var date = dto.Date.Date;

            var existingAttendance = await _context.Attendances
                .Where(a => a.HalaqaId == dto.HalaqaId && a.Date == date)
                .ToDictionaryAsync(a => a.StudentId, a => a);

            foreach (var studentAttendance in dto.Attendance)
            {
                if (existingAttendance.TryGetValue(studentAttendance.StudentId, out var existing))
                {
                    existing.Status = studentAttendance.Status;
                    existing.Notes = studentAttendance.Notes;
                }
                else
                {
                    var attendance = new Attendance
                    {
                        StudentId = studentAttendance.StudentId,
                        HalaqaId = dto.HalaqaId,
                        Date = date,
                        Status = studentAttendance.Status,
                        Notes = studentAttendance.Notes,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Attendances.Add(attendance);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AttendanceSummaryDto> GetAttendanceByDateAsync(int halaqaId, DateTime date)
        {
            var targetDate = date.Date;

            var halaqa = await _context.Halaqat.FindAsync(halaqaId);
            if (halaqa == null)
            {
                throw new InvalidOperationException("الحلقة غير موجودة");
            }

            var students = await _context.StudentHalaqat
                .Include(sh => sh.Student)
                .Where(sh => sh.HalaqaId == halaqaId && sh.IsActive)
                .Select(sh => sh.Student)
                .ToListAsync();

            var attendanceRecords = await _context.Attendances
                .Include(a => a.Student)
                .Where(a => a.HalaqaId == halaqaId && a.Date == targetDate)
                .ToListAsync();

            var attendanceDict = attendanceRecords.ToDictionary(a => a.StudentId, a => a);

            var summary = new AttendanceSummaryDto
            {
                Date = targetDate,
                HalaqaId = halaqaId,
                HalaqaName = halaqa.Name,
                TotalStudents = students.Count,
                Present = 0,
                Absent = 0,
                Late = 0,
                Records = new List<AttendanceRecordDto>()
            };

            foreach (var student in students)
            {
                AttendanceRecordDto record;

                if (attendanceDict.TryGetValue(student.Id, out var attendance))
                {
                    record = MapToDto(attendance);

                    switch (attendance.Status)
                    {
                        case AttendanceStatus.Present:
                            summary.Present++;
                            break;
                        case AttendanceStatus.Absent:
                            summary.Absent++;
                            break;
                        case AttendanceStatus.Late:
                            summary.Late++;
                            break;
                    }
                }
                else
                {
                    record = new AttendanceRecordDto
                    {
                        Id = 0,
                        StudentId = student.Id,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        HalaqaId = halaqaId,
                        HalaqaName = halaqa.Name,
                        Date = targetDate,
                        Status = "غائب",
                        Notes = null,
                        CreatedAt = DateTime.UtcNow
                    };
                    summary.Absent++;
                }

                summary.Records.Add(record);
            }

            return summary;
        }

        public async Task<IEnumerable<AttendanceRecordDto>> GetStudentAttendanceAsync(int studentId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Halaqa)
                .Where(a => a.StudentId == studentId);

            if (fromDate.HasValue)
                query = query.Where(a => a.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(a => a.Date <= toDate.Value.Date);

            var records = await query.OrderByDescending(a => a.Date).ToListAsync();
            return records.Select(MapToDto);
        }

        public async Task<StudentAttendanceSummaryDto> GetStudentAttendanceSummaryAsync(int studentId, DateTime fromDate, DateTime toDate)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                throw new InvalidOperationException("الطالب غير موجود");
            }

            var attendance = await _context.Attendances
                .Where(a => a.StudentId == studentId && a.Date >= fromDate.Date && a.Date <= toDate.Date)
                .ToListAsync();

            return new StudentAttendanceSummaryDto
            {
                StudentId = studentId,
                StudentName = $"{student.FirstName} {student.LastName}",
                TotalDays = attendance.Count,
                PresentDays = attendance.Count(a => a.Status == AttendanceStatus.Present),
                AbsentDays = attendance.Count(a => a.Status == AttendanceStatus.Absent),
                LateDays = attendance.Count(a => a.Status == AttendanceStatus.Late)
            };
        }

        public async Task<bool> UpdateAttendanceAsync(int id, AttendanceStatus status, string? notes)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
                return false;

            attendance.Status = status;
            attendance.Notes = notes;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAttendanceAsync(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
                return false;

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return true;
        }

        private AttendanceRecordDto MapToDto(Attendance attendance)
        {
            return new AttendanceRecordDto
            {
                Id = attendance.Id,
                StudentId = attendance.StudentId,
                StudentName = attendance.Student != null ? $"{attendance.Student.FirstName} {attendance.Student.LastName}" : "",
                HalaqaId = attendance.HalaqaId,
                HalaqaName = attendance.Halaqa?.Name ?? "",
                Date = attendance.Date,
                Status = attendance.Status switch
                {
                    AttendanceStatus.Present => "حاضر",
                    AttendanceStatus.Absent => "غائب",
                    AttendanceStatus.Late => "متأخر",
                    _ => ""
                },
                Notes = attendance.Notes,
                CreatedAt = attendance.CreatedAt
            };
        }
    }
}

