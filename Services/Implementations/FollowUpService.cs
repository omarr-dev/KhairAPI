using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    public class FollowUpService : IFollowUpService
    {
        private readonly AppDbContext _context;
        private const double LinesPerPage = 15.0;

        public FollowUpService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FollowUpResponseDto> GetFollowUpDataAsync(DateTime date, int? teacherId = null, List<int>? supervisedHalaqaIds = null)
        {
            // 1. Load hierarchy: Halaqat → HalaqaTeachers → Teacher, StudentHalaqat → Student
            var halaqaQuery = _context.Halaqat
                .Include(h => h.HalaqaTeachers)
                    .ThenInclude(ht => ht.Teacher)
                .Include(h => h.StudentHalaqat)
                    .ThenInclude(sh => sh.Student)
                .AsSplitQuery()
                .AsNoTracking()
                .Where(h => h.IsActive);

            // Role filtering
            if (teacherId.HasValue)
            {
                halaqaQuery = halaqaQuery.Where(h => h.HalaqaTeachers.Any(ht => ht.TeacherId == teacherId.Value));
            }
            else if (supervisedHalaqaIds != null)
            {
                halaqaQuery = halaqaQuery.Where(h => supervisedHalaqaIds.Contains(h.Id));
            }

            var halaqat = await halaqaQuery.OrderBy(h => h.Name).ToListAsync();

            // Collect all student IDs and teacher IDs for batch queries
            var allStudentIds = new HashSet<int>();
            var allTeacherIds = new HashSet<int>();

            foreach (var h in halaqat)
            {
                foreach (var sh in h.StudentHalaqat.Where(sh => sh.IsActive))
                {
                    allStudentIds.Add(sh.StudentId);
                }
                foreach (var ht in h.HalaqaTeachers)
                {
                    allTeacherIds.Add(ht.TeacherId);
                }
            }

            // 2. Batch load student attendance for the date
            var studentAttendances = await _context.Attendances
                .AsNoTracking()
                .Where(a => a.Date == date && allStudentIds.Contains(a.StudentId))
                .ToListAsync();

            var studentAttendanceMap = studentAttendances
                .GroupBy(a => (a.StudentId, a.HalaqaId))
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Status
                );

            // 3. Batch load teacher attendance for the date
            var teacherAttendances = await _context.TeacherAttendances
                .AsNoTracking()
                .Where(a => a.Date == date && allTeacherIds.Contains(a.TeacherId))
                .ToListAsync();

            var teacherAttendanceMap = teacherAttendances
                .GroupBy(a => (a.TeacherId, a.HalaqaId))
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Status
                );

            // 4. Batch load progress records for the date
            var progressRecords = await _context.ProgressRecords
                .AsNoTracking()
                .Where(p => p.Date == date && allStudentIds.Contains(p.StudentId))
                .ToListAsync();

            var progressMap = progressRecords
                .GroupBy(p => (p.StudentId, p.Type))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.NumberLines)
                );

            // 5. Batch load student targets
            var studentTargets = await _context.StudentTargets
                .AsNoTracking()
                .Where(t => allStudentIds.Contains(t.StudentId))
                .ToDictionaryAsync(t => t.StudentId);

            // Build the response
            var response = new FollowUpResponseDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Halaqat = new List<FollowUpHalaqaDto>()
            };

            var totalStudentStats = new FollowUpAttendanceStatsDto();
            var totalTeacherStats = new FollowUpAttendanceStatsDto();
            var totalAchievement = new FollowUpAchievementDto();

            foreach (var halaqa in halaqat)
            {
                var halaqaDto = new FollowUpHalaqaDto
                {
                    Id = halaqa.Id,
                    Name = halaqa.Name,
                    Teachers = new List<FollowUpTeacherDto>()
                };

                var halaqaStudentStats = new FollowUpAttendanceStatsDto();
                var halaqaTeacherStats = new FollowUpAttendanceStatsDto();
                var halaqaAchievement = new FollowUpAchievementDto();

                // Get active students in this halaqa, grouped by teacher
                var activeStudentHalaqat = halaqa.StudentHalaqat.Where(sh => sh.IsActive).ToList();

                // For teacher role: only show their students
                if (teacherId.HasValue)
                {
                    activeStudentHalaqat = activeStudentHalaqat.Where(sh => sh.TeacherId == teacherId.Value).ToList();
                }

                var studentsByTeacher = activeStudentHalaqat
                    .GroupBy(sh => sh.TeacherId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var ht in halaqa.HalaqaTeachers)
                {
                    // For teacher role: skip other teachers
                    if (teacherId.HasValue && ht.TeacherId != teacherId.Value)
                        continue;

                    var teacherDto = new FollowUpTeacherDto
                    {
                        Id = ht.Teacher.Id,
                        FullName = ht.Teacher.FullName,
                        Students = new List<FollowUpStudentDto>()
                    };

                    // Teacher attendance
                    var teacherAttStatus = teacherAttendanceMap.TryGetValue((ht.TeacherId, halaqa.Id), out var tStatus)
                        ? MapAttendanceStatus(tStatus)
                        : "not_recorded";
                    teacherDto.AttendanceStatus = teacherAttStatus;

                    var teacherStudentStats = new FollowUpAttendanceStatsDto();
                    var teacherAchievement = new FollowUpAchievementDto();

                    // Process students for this teacher
                    if (studentsByTeacher.TryGetValue(ht.TeacherId, out var teacherStudents))
                    {
                        foreach (var sh in teacherStudents)
                        {
                            var student = sh.Student;
                            var studentDto = new FollowUpStudentDto
                            {
                                Id = student.Id,
                                FullName = student.FullName
                            };

                            // Student attendance
                            var studentAttStatus = studentAttendanceMap.TryGetValue((student.Id, halaqa.Id), out var sStatus)
                                ? MapAttendanceStatus(sStatus)
                                : "not_recorded";
                            studentDto.AttendanceStatus = studentAttStatus;

                            // Student achievement
                            var studentAchievement = BuildStudentAchievement(student.Id, progressMap, studentTargets);
                            studentDto.Achievement = studentAchievement;

                            teacherDto.Students.Add(studentDto);

                            // Accumulate teacher stats
                            teacherStudentStats.Total++;
                            switch (studentAttStatus)
                            {
                                case "present": teacherStudentStats.Present++; break;
                                case "absent": teacherStudentStats.Absent++; break;
                                default: teacherStudentStats.NotRecorded++; break;
                            }

                            AccumulateAchievement(teacherAchievement, studentAchievement);
                        }
                    }

                    teacherDto.StudentStats = teacherStudentStats;
                    teacherDto.Achievement = teacherAchievement;
                    teacherDto.Students = teacherDto.Students.OrderBy(s => s.FullName).ToList();

                    halaqaDto.Teachers.Add(teacherDto);

                    // Accumulate halaqa stats
                    halaqaTeacherStats.Total++;
                    switch (teacherAttStatus)
                    {
                        case "present": halaqaTeacherStats.Present++; break;
                        case "absent": halaqaTeacherStats.Absent++; break;
                        default: halaqaTeacherStats.NotRecorded++; break;
                    }

                    AccumulateStats(halaqaStudentStats, teacherStudentStats);
                    AccumulateAchievement(halaqaAchievement, teacherAchievement);
                }

                halaqaDto.StudentStats = halaqaStudentStats;
                halaqaDto.TeacherStats = halaqaTeacherStats;
                halaqaDto.Achievement = halaqaAchievement;
                halaqaDto.Teachers = halaqaDto.Teachers.OrderBy(t => t.FullName).ToList();

                response.Halaqat.Add(halaqaDto);

                // Accumulate totals
                AccumulateStats(totalStudentStats, halaqaStudentStats);
                AccumulateStats(totalTeacherStats, halaqaTeacherStats);
                AccumulateAchievement(totalAchievement, halaqaAchievement);
            }

            response.TotalStudentStats = totalStudentStats;
            response.TotalTeacherStats = totalTeacherStats;
            response.TotalAchievement = totalAchievement;

            return response;
        }

        private static string MapAttendanceStatus(AttendanceStatus status) => status switch
        {
            AttendanceStatus.Present => "present",
            AttendanceStatus.Absent => "absent",
            AttendanceStatus.Late => "present", // Late counts as present for follow-up
            _ => "not_recorded"
        };

        private static FollowUpAchievementDto BuildStudentAchievement(
            int studentId,
            Dictionary<(int StudentId, ProgressType Type), double> progressMap,
            Dictionary<int, StudentTarget> studentTargets)
        {
            var achievement = new FollowUpAchievementDto();

            // Get achieved values (lines for memorization, convert to pages for revision/consolidation)
            var memLines = progressMap.TryGetValue((studentId, ProgressType.Memorization), out var ml) ? ml : 0;
            var revLines = progressMap.TryGetValue((studentId, ProgressType.Revision), out var rl) ? rl : 0;
            var conLines = progressMap.TryGetValue((studentId, ProgressType.Consolidation), out var cl) ? cl : 0;

            achievement.Memorization.Achieved = Math.Round(memLines, 1);
            achievement.Revision.Achieved = Math.Round(revLines / LinesPerPage, 1);
            achievement.Consolidation.Achieved = Math.Round(conLines / LinesPerPage, 1);

            // Get targets
            if (studentTargets.TryGetValue(studentId, out var target))
            {
                achievement.Memorization.Target = target.MemorizationLinesTarget ?? 0;
                achievement.Revision.Target = target.RevisionPagesTarget ?? 0;
                achievement.Consolidation.Target = target.ConsolidationPagesTarget ?? 0;
            }

            return achievement;
        }

        private static void AccumulateAchievement(FollowUpAchievementDto accumulator, FollowUpAchievementDto source)
        {
            accumulator.Memorization.Achieved += source.Memorization.Achieved;
            accumulator.Memorization.Target += source.Memorization.Target;
            accumulator.Revision.Achieved += source.Revision.Achieved;
            accumulator.Revision.Target += source.Revision.Target;
            accumulator.Consolidation.Achieved += source.Consolidation.Achieved;
            accumulator.Consolidation.Target += source.Consolidation.Target;
        }

        private static void AccumulateStats(FollowUpAttendanceStatsDto accumulator, FollowUpAttendanceStatsDto source)
        {
            accumulator.Total += source.Total;
            accumulator.Present += source.Present;
            accumulator.Absent += source.Absent;
            accumulator.NotRecorded += source.NotRecorded;
        }
    }
}
