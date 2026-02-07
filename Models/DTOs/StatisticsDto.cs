namespace KhairAPI.Models.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalHalaqat { get; set; }
        public int ActiveHalaqat { get; set; }
        public double AverageAttendanceRate { get; set; }
        public int TodayMemorization { get; set; }
        public int TodayRevision { get; set; }
        public int TodayAttendance { get; set; }
    }

    public class ReportStatsDto
    {
        public int TotalStudents { get; set; }
        public double AverageAttendance { get; set; }
        public int WeeklyMemorization { get; set; }
        public double AverageQuality { get; set; }
        public List<DailyChartDataDto> ProgressData { get; set; } = new();
        public List<DailyChartDataDto> AttendanceData { get; set; } = new();
        public List<StudentPerformanceDto> TopStudents { get; set; } = new();
        public List<QualityDistributionDto> QualityDistribution { get; set; } = new();
    }

    public class DailyChartDataDto
    {
        public string Date { get; set; } = string.Empty;
        public int Memorization { get; set; }
        public int Revision { get; set; }
        public double Rate { get; set; }
    }

    public class StudentPerformanceDto
    {
        public string Name { get; set; } = string.Empty;
        public int Progress { get; set; }
        public double Quality { get; set; }
    }

    public class QualityDistributionDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    // Supervisor Dashboard DTOs
    public class SupervisorDashboardDto
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalHalaqat { get; set; }
        public double TodayAttendanceRate { get; set; }
        public int TodayMemorization { get; set; }
        public int TodayRevision { get; set; }
        public int StudentsAtRisk { get; set; }
        public List<HalaqaRankingDto> TopHalaqat { get; set; } = new();
        public List<TeacherRankingDto> TopTeachers { get; set; } = new();
        public List<AtRiskStudentDto> AtRiskStudents { get; set; } = new();
    }

    public class HalaqaRankingDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
        public double AttendanceRate { get; set; }
        public int WeeklyProgress { get; set; } // Number of progress records this week
        public double Score { get; set; } // Combined performance score
    }

    public class TeacherRankingDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public double StudentAttendanceRate { get; set; }
        public int WeeklyProgress { get; set; }
        public double AverageQuality { get; set; }
        public double Score { get; set; } // Combined performance score
    }

    public class AtRiskStudentDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string HalaqaName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public double AttendanceRate { get; set; }
        public int DaysSinceLastProgress { get; set; }
        public int ConsecutiveAbsences { get; set; }
    }

    /// <summary>
    /// System-wide statistics for motivation section (all halaqat combined)
    /// </summary>
    public class SystemWideStatsDto
    {
        // Today's stats
        public int TodayVersesMemorized { get; set; }
        public int TodayVersesReviewed { get; set; }
        public int TodayStudentsActive { get; set; }
        public int TotalStudents { get; set; }
        
        // Comparison with yesterday
        public int VersesMemorizedChange { get; set; } // +/- vs yesterday
        public int VersesReviewedChange { get; set; }
        public int StudentsActiveChange { get; set; }
        
        // Current week stats (cumulative)
        public int WeekVersesMemorized { get; set; }
        public int WeekVersesReviewed { get; set; }
        public int WeekStudentsActive { get; set; } // Unique students with progress this week
    }

    /// <summary>
    /// Target Adoption Overview statistics - shows coverage of target system
    /// تغطية نظام الأهداف
    /// </summary>
    public class TargetAdoptionOverviewDto
    {
        /// <summary>
        /// Overall percentage of students with targets defined
        /// النسبة المئوية للطلاب الذين لديهم أهداف
        /// </summary>
        public double CoveragePercentage { get; set; }

        /// <summary>
        /// Number of students with targets
        /// عدد الطلاب الذين لديهم أهداف
        /// </summary>
        public int StudentsWithTargets { get; set; }

        /// <summary>
        /// Total number of students in scope
        /// إجمالي عدد الطلاب
        /// </summary>
        public int TotalStudents { get; set; }

        /// <summary>
        /// Percentage change compared to last week (+/-)
        /// نسبة التغيير عن الأسبوع الماضي
        /// </summary>
        public double WeeklyChangePercentage { get; set; }

        /// <summary>
        /// Number of halaqat with at least one student having targets vs total halaqat
        /// عدد الحلقات التي لديها أهداف / إجمالي الحلقات
        /// </summary>
        public HalaqaCoverageDto HalaqaCoverage { get; set; } = new();

        /// <summary>
        /// Number of teachers with at least one student having targets vs total teachers
        /// عدد المعلمين الذين لديهم طلاب بأهداف / إجمالي المعلمين
        /// </summary>
        public TeacherCoverageDto TeacherCoverage { get; set; } = new();

        /// <summary>
        /// Overall activation rate (percentage of targets that are actively being tracked)
        /// نسبة التفعيل - نسبة الأهداف النشطة
        /// </summary>
        public double ActivationRate { get; set; }

        /// <summary>
        /// Breakdown by halaqa (optional, included when viewing specific halaqat)
        /// تفاصيل حسب الحلقة
        /// </summary>
        public List<HalaqaTargetStatsDto> HalaqaBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Halaqa coverage statistics
    /// </summary>
    public class HalaqaCoverageDto
    {
        /// <summary>
        /// Number of halaqat with students having targets
        /// </summary>
        public int HalaqatWithTargets { get; set; }

        /// <summary>
        /// Total halaqat in scope
        /// </summary>
        public int TotalHalaqat { get; set; }
    }

    /// <summary>
    /// Teacher coverage statistics
    /// </summary>
    public class TeacherCoverageDto
    {
        /// <summary>
        /// Number of teachers with students having targets
        /// </summary>
        public int TeachersWithTargets { get; set; }

        /// <summary>
        /// Total teachers in scope
        /// </summary>
        public int TotalTeachers { get; set; }
    }

    /// <summary>
    /// Target statistics per halaqa
    /// </summary>
    public class HalaqaTargetStatsDto
    {
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public int StudentsWithTargets { get; set; }
        public int TotalStudents { get; set; }
        public double CoveragePercentage { get; set; }
    }

    /// <summary>
    /// Parameters for target adoption overview request
    /// </summary>
    public class TargetAdoptionFilterDto
    {
        public int? TeacherId { get; set; }
        public List<int>? SupervisedHalaqaIds { get; set; }
        public int? SelectedHalaqaId { get; set; }
        public int? SelectedTeacherId { get; set; }
        public bool IncludeHalaqaBreakdown { get; set; }
    }

    /// <summary>
    /// Daily achievement statistics showing aggregated progress vs targets
    /// إنجاز اليوم - إحصائيات الإنجاز اليومي المجمّعة
    /// </summary>
    public class DailyAchievementStatsDto
    {
        /// <summary>
        /// Date range start
        /// </summary>
        public DateTime FromDate { get; set; }

        /// <summary>
        /// Date range end
        /// </summary>
        public DateTime ToDate { get; set; }

        /// <summary>
        /// Total students in scope
        /// </summary>
        public int TotalStudents { get; set; }

        /// <summary>
        /// Number of students with targets set
        /// </summary>
        public int StudentsWithTargets { get; set; }

        /// <summary>
        /// Memorization achievement (حفظ) - measured in lines (سطر)
        /// </summary>
        public AchievementCategoryDto Memorization { get; set; } = new();

        /// <summary>
        /// Revision achievement (مراجعة) - measured in pages (وجه)
        /// </summary>
        public AchievementCategoryDto Revision { get; set; } = new();

        /// <summary>
        /// Consolidation achievement (تثبيت) - measured in pages (وجه)
        /// </summary>
        public AchievementCategoryDto Consolidation { get; set; } = new();

        /// <summary>
        /// Week summary showing target achievement per day
        /// ملخص الأسبوع
        /// </summary>
        public WeekSummaryDto WeekSummary { get; set; } = new();
    }

    /// <summary>
    /// Achievement category with target, achieved, and percentage
    /// </summary>
    public class AchievementCategoryDto
    {
        /// <summary>
        /// Amount achieved (lines for memorization, pages for revision/consolidation)
        /// </summary>
        public double Achieved { get; set; }

        /// <summary>
        /// Target amount (sum of all student targets in scope)
        /// </summary>
        public double Target { get; set; }

        /// <summary>
        /// Achievement percentage (capped at 100)
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// Unit label: "سطر" for lines, "وجه" for pages
        /// </summary>
        public string Unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// Week summary showing daily target achievement status
    /// </summary>
    public class WeekSummaryDto
    {
        /// <summary>
        /// Daily achievement status for each day in the range
        /// Each entry: Date and whether targets were met
        /// </summary>
        public List<DayAchievementStatusDto> Days { get; set; } = new();

        /// <summary>
        /// Number of days where overall target was met
        /// </summary>
        public int DaysTargetMet { get; set; }

        /// <summary>
        /// Total days in the range
        /// </summary>
        public int TotalDays { get; set; }
    }

    /// <summary>
    /// Single day achievement status
    /// </summary>
    public class DayAchievementStatusDto
    {
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Whether the aggregated target was met for this day
        /// </summary>
        public bool TargetMet { get; set; }
        
        /// <summary>
        /// Overall achievement percentage for the day
        /// </summary>
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Filter parameters for daily achievement statistics
    /// </summary>
    public class DailyAchievementFilterDto
    {
        public int? TeacherId { get; set; }
        public List<int>? SupervisedHalaqaIds { get; set; }
        public int? SelectedHalaqaId { get; set; }
        public int? SelectedTeacherId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    /// <summary>
    /// Streak Leaderboard - أطول سلاسل الإنجاز
    /// Shows students with the longest consecutive progress streaks
    /// </summary>
    public class StreakLeaderboardDto
    {
        /// <summary>
        /// List of students with their streaks, ordered by streak length descending
        /// </summary>
        public List<StudentStreakDto> Students { get; set; } = new();

        /// <summary>
        /// Total number of students in scope
        /// </summary>
        public int TotalStudentsInScope { get; set; }

        /// <summary>
        /// Number of students with active streaks (≥1 day)
        /// </summary>
        public int StudentsWithActiveStreaks { get; set; }

        /// <summary>
        /// Optional filter applied - halaqa name if filtered by specific halaqa
        /// </summary>
        public string? FilteredByHalaqa { get; set; }
    }

    /// <summary>
    /// Student streak information
    /// معلومات سلسلة الإنجاز للطالب
    /// </summary>
    public class StudentStreakDto
    {
        /// <summary>
        /// Student ID
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// Student full name
        /// </summary>
        public string StudentName { get; set; } = string.Empty;

        /// <summary>
        /// Halaqa name
        /// </summary>
        public string HalaqaName { get; set; } = string.Empty;

        /// <summary>
        /// Halaqa ID
        /// </summary>
        public int HalaqaId { get; set; }

        /// <summary>
        /// Current active streak in days
        /// سلسلة الإنجاز الحالية (عدد الأيام)
        /// </summary>
        public int CurrentStreak { get; set; }

        /// <summary>
        /// Longest streak ever achieved
        /// أطول سلسلة إنجاز تم تحقيقها
        /// </summary>
        public int LongestStreak { get; set; }

        /// <summary>
        /// Whether the streak is currently active (has progress today or on last active day)
        /// </summary>
        public bool IsStreakActive { get; set; }

        /// <summary>
        /// Last progress date
        /// </summary>
        public DateTime? LastProgressDate { get; set; }

        /// <summary>
        /// Rank position in the leaderboard
        /// </summary>
        public int Rank { get; set; }
    }

    /// <summary>
    /// Filter parameters for streak leaderboard
    /// </summary>
    public class StreakLeaderboardFilterDto
    {
        /// <summary>
        /// Teacher ID (for teacher-scoped queries)
        /// </summary>
        public int? TeacherId { get; set; }

        /// <summary>
        /// List of halaqa IDs the user can access (for HalaqaSupervisor)
        /// </summary>
        public List<int>? SupervisedHalaqaIds { get; set; }

        /// <summary>
        /// Optional: Filter to a specific halaqa
        /// </summary>
        public int? SelectedHalaqaId { get; set; }

        /// <summary>
        /// Optional: Filter to a specific teacher (for supervisors)
        /// </summary>
        public int? SelectedTeacherId { get; set; }

        /// <summary>
        /// Number of top students to return (default: 10)
        /// </summary>
        public int Limit { get; set; } = 10;
    }
}

