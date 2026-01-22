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
        /// <param name="teacherId">Optional teacher filter (for teachers viewing their students)</param>
        /// <param name="halaqaFilter">Optional list of halaqa IDs (for HalaqaSupervisors)</param>
        /// <param name="selectedHalaqaId">Optional specific halaqa to view details for</param>
        /// <param name="includeHalaqaBreakdown">Include per-halaqa breakdown in response</param>
        Task<TargetAdoptionOverviewDto> GetTargetAdoptionOverviewAsync(
            int? teacherId = null,
            List<int>? halaqaFilter = null,
            int? selectedHalaqaId = null,
            bool includeHalaqaBreakdown = false);
    }
}

