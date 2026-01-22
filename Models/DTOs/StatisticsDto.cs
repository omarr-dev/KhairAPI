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
}

