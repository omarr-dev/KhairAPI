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

        public async Task<FollowUpResponseDto> GetFollowUpDataAsync(DateTime date, int? teacherId = null, List<int>? supervisedHalaqaIds = null, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            // 1. Matching halaqat (active + role filter), ordered by name — IDs only.
            //    The list page is a slice of these; the Total* stats below cover ALL of them.
            var matchingQuery = _context.Halaqat
                .AsNoTracking()
                .Where(h => h.IsActive);

            if (teacherId.HasValue)
            {
                matchingQuery = matchingQuery.Where(h => h.HalaqaTeachers.Any(ht => ht.TeacherId == teacherId.Value));
            }
            else if (supervisedHalaqaIds != null)
            {
                matchingQuery = matchingQuery.Where(h => supervisedHalaqaIds.Contains(h.Id));
            }

            var matchingHalaqaIds = await matchingQuery
                .OrderBy(h => h.Name)
                .Select(h => h.Id)
                .ToListAsync();

            var totalCount = matchingHalaqaIds.Count;
            var pageHalaqaIds = matchingHalaqaIds
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 1b. Lightweight pairs across ALL matching halaqat (no Student/Teacher entity loads).
            //     Used to compute exact totals without building/serializing thousands of DTOs.
            var studentPairsQuery = _context.StudentHalaqat
                .AsNoTracking()
                .Where(sh => sh.IsActive && matchingHalaqaIds.Contains(sh.HalaqaId));
            if (teacherId.HasValue)
            {
                studentPairsQuery = studentPairsQuery.Where(sh => sh.TeacherId == teacherId.Value);
            }
            var studentPairs = await studentPairsQuery
                .Select(sh => new { sh.StudentId, sh.HalaqaId, sh.TeacherId })
                .ToListAsync();

            var teacherPairsQuery = _context.HalaqaTeachers
                .AsNoTracking()
                .Where(ht => matchingHalaqaIds.Contains(ht.HalaqaId));
            if (teacherId.HasValue)
            {
                teacherPairsQuery = teacherPairsQuery.Where(ht => ht.TeacherId == teacherId.Value);
            }
            var teacherPairs = await teacherPairsQuery
                .Select(ht => new { ht.TeacherId, ht.HalaqaId })
                .ToListAsync();

            var allStudentIds = studentPairs.Select(p => p.StudentId).ToHashSet();
            var allTeacherIds = teacherPairs.Select(p => p.TeacherId).ToHashSet();
            // Mirror the per-halaqa render: a student is only counted under a teacher
            // that actually teaches that halaqa.
            var teacherInHalaqaSet = teacherPairs.Select(p => (p.HalaqaId, p.TeacherId)).ToHashSet();

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

            // Totals across ALL matching halaqat — computed from the lightweight pairs
            // (no DTO building). Per-student accumulation mirrors the page render exactly.
            var totalStudentStats = new FollowUpAttendanceStatsDto();
            var totalTeacherStats = new FollowUpAttendanceStatsDto();
            var totalAchievement = new FollowUpAchievementDto();

            foreach (var sp in studentPairs)
            {
                if (!teacherInHalaqaSet.Contains((sp.HalaqaId, sp.TeacherId)))
                    continue;

                var studentAttStatus = studentAttendanceMap.TryGetValue((sp.StudentId, sp.HalaqaId), out var sStatus)
                    ? MapAttendanceStatus(sStatus)
                    : "not_recorded";

                totalStudentStats.Total++;
                switch (studentAttStatus)
                {
                    case "present": totalStudentStats.Present++; break;
                    case "absent": totalStudentStats.Absent++; break;
                    default: totalStudentStats.NotRecorded++; break;
                }

                AccumulateAchievement(totalAchievement, BuildStudentAchievement(sp.StudentId, progressMap, studentTargets));
            }

            foreach (var tp in teacherPairs)
            {
                var teacherAttStatus = teacherAttendanceMap.TryGetValue((tp.TeacherId, tp.HalaqaId), out var tStatus)
                    ? MapAttendanceStatus(tStatus)
                    : "not_recorded";

                totalTeacherStats.Total++;
                switch (teacherAttStatus)
                {
                    case "present": totalTeacherStats.Present++; break;
                    case "absent": totalTeacherStats.Absent++; break;
                    default: totalTeacherStats.NotRecorded++; break;
                }
            }

            // Build detailed DTOs ONLY for the current page of halaqat.
            var pageHalaqat = await _context.Halaqat
                .Include(h => h.HalaqaTeachers)
                    .ThenInclude(ht => ht.Teacher)
                .Include(h => h.StudentHalaqat)
                    .ThenInclude(sh => sh.Student)
                .AsSplitQuery()
                .AsNoTracking()
                .Where(h => pageHalaqaIds.Contains(h.Id))
                .ToListAsync();

            // Preserve the matching (by-name) order of the page slice
            var pageOrder = pageHalaqaIds
                .Select((id, idx) => (id, idx))
                .ToDictionary(x => x.id, x => x.idx);
            var halaqat = pageHalaqat.OrderBy(h => pageOrder[h.Id]).ToList();

            var response = new FollowUpResponseDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Halaqat = new List<FollowUpHalaqaDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

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
