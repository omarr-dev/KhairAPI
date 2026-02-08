namespace KhairAPI.Models.DTOs
{
    public class FollowUpResponseDto
    {
        public string Date { get; set; } = string.Empty;
        public List<FollowUpHalaqaDto> Halaqat { get; set; } = new();
        public FollowUpAttendanceStatsDto TotalStudentStats { get; set; } = new();
        public FollowUpAttendanceStatsDto TotalTeacherStats { get; set; } = new();
        public FollowUpAchievementDto TotalAchievement { get; set; } = new();
    }

    public class FollowUpHalaqaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<FollowUpTeacherDto> Teachers { get; set; } = new();
        public FollowUpAttendanceStatsDto StudentStats { get; set; } = new();
        public FollowUpAttendanceStatsDto TeacherStats { get; set; } = new();
        public FollowUpAchievementDto Achievement { get; set; } = new();
    }

    public class FollowUpTeacherDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AttendanceStatus { get; set; } = "not_recorded";
        public List<FollowUpStudentDto> Students { get; set; } = new();
        public FollowUpAttendanceStatsDto StudentStats { get; set; } = new();
        public FollowUpAchievementDto Achievement { get; set; } = new();
    }

    public class FollowUpStudentDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AttendanceStatus { get; set; } = "not_recorded";
        public FollowUpAchievementDto Achievement { get; set; } = new();
    }

    public class FollowUpAttendanceStatsDto
    {
        public int Total { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int NotRecorded { get; set; }
    }

    public class FollowUpAchievementDto
    {
        public FollowUpAchievementDataDto Memorization { get; set; } = new();
        public FollowUpAchievementDataDto Revision { get; set; } = new();
        public FollowUpAchievementDataDto Consolidation { get; set; } = new();
    }

    public class FollowUpAchievementDataDto
    {
        public double Achieved { get; set; }
        public double Target { get; set; }
    }
}
