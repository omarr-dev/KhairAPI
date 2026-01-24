using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IStatisticsService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync(int? teacherId = null, List<int>? halaqaFilter = null);
        
        /// <summary>
        /// Gets report statistics for a given date range
        /// </summary>
        /// <param name="dateRange">Date range type: "week", "month", or "custom"</param>
        /// <param name="halaqaId">Optional halaqa filter</param>
        /// <param name="teacherId">Optional teacher filter</param>
        /// <param name="halaqaFilter">Optional list of halaqa IDs for supervisor filtering</param>
        /// <param name="customFromDate">Custom start date (required when dateRange is "custom")</param>
        /// <param name="customToDate">Custom end date (required when dateRange is "custom")</param>
        Task<ReportStatsDto> GetReportStatsAsync(
            string dateRange, 
            int? halaqaId = null, 
            int? teacherId = null, 
            List<int>? halaqaFilter = null,
            DateTime? customFromDate = null,
            DateTime? customToDate = null);
        
        Task<SystemWideStatsDto> GetSystemWideStatsAsync();
        Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(List<int>? halaqaFilter = null);
        Task<List<HalaqaRankingDto>> GetHalaqaRankingAsync(int days = 7, int limit = 10, List<int>? halaqaFilter = null);
        Task<List<HalaqaRankingDto>> GetTopHalaqatAsync();
        Task<List<TeacherRankingDto>> GetTeacherRankingAsync(int days = 7, int limit = 10, List<int>? halaqaFilter = null);
        Task<List<AtRiskStudentDto>> GetAtRiskStudentsAsync(int limit = 20, List<int>? halaqaFilter = null);
        Task<List<AtRiskStudentDto>> GetTeacherAtRiskStudentsAsync(int teacherId, int limit = 10);
        
        /// <summary>
        /// Gets target adoption overview statistics.
        /// تغطية نظام الأهداف - إحصائيات تبني نظام الأهداف
        /// </summary>
        /// <param name="filter">Filter parameters for the overview</param>
        Task<TargetAdoptionOverviewDto> GetTargetAdoptionOverviewAsync(TargetAdoptionFilterDto filter);

        /// <summary>
        /// Gets daily achievement statistics showing aggregated progress vs targets.
        /// إنجاز اليوم - إحصائيات الإنجاز اليومي المجمّعة
        /// 
        /// Returns:
        /// - Aggregated targets and achievements for memorization (lines), revision (pages), consolidation (pages)
        /// - Week summary showing days where targets were met
        /// </summary>
        /// <param name="filter">Filter parameters including role-based filtering and date range</param>
        Task<DailyAchievementStatsDto> GetDailyAchievementStatsAsync(DailyAchievementFilterDto filter);

        /// <summary>
        /// Gets the streak leaderboard showing students with longest consecutive progress days.
        /// أطول سلاسل الإنجاز
        /// 
        /// A streak is counted based on consecutive halaqa active days with at least one progress record.
        /// 
        /// Access control:
        /// - Teachers: See students in their halaqat only
        /// - HalaqaSupervisors: See students in their assigned halaqat
        /// - Supervisors: See all students, can filter by halaqa
        /// </summary>
        /// <param name="filter">Filter parameters including role-based access and optional halaqa filter</param>
        Task<StreakLeaderboardDto> GetStreakLeaderboardAsync(StreakLeaderboardFilterDto filter);
    }
}

