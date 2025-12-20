using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IStatisticsService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync(int? teacherId = null);
        Task<ReportStatsDto> GetReportStatsAsync(string dateRange, int? halaqaId = null, int? teacherId = null);
        Task<SystemWideStatsDto> GetSystemWideStatsAsync();
        Task<SupervisorDashboardDto> GetSupervisorDashboardAsync();
        Task<List<HalaqaRankingDto>> GetHalaqaRankingAsync(int days = 7, int limit = 10);
        Task<List<HalaqaRankingDto>> GetTopHalaqatAsync();
        Task<List<TeacherRankingDto>> GetTeacherRankingAsync(int days = 7, int limit = 10);
        Task<List<AtRiskStudentDto>> GetAtRiskStudentsAsync(int limit = 20);
        Task<List<AtRiskStudentDto>> GetTeacherAtRiskStudentsAsync(int teacherId, int limit = 10);
        Task<List<AttendanceTrendDto>> GetAttendanceTrendsAsync(int days = 30);
        Task<List<ProgressTrendDto>> GetProgressTrendsAsync(int days = 30);
    }
}

