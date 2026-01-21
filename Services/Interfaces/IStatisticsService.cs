using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IStatisticsService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync(int? teacherId = null, List<int>? halaqaFilter = null);
        Task<ReportStatsDto> GetReportStatsAsync(string dateRange, int? halaqaId = null, int? teacherId = null, List<int>? halaqaFilter = null);
        Task<SystemWideStatsDto> GetSystemWideStatsAsync();
        Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(List<int>? halaqaFilter = null);
        Task<List<HalaqaRankingDto>> GetHalaqaRankingAsync(int days = 7, int limit = 10, List<int>? halaqaFilter = null);
        Task<List<HalaqaRankingDto>> GetTopHalaqatAsync();
        Task<List<TeacherRankingDto>> GetTeacherRankingAsync(int days = 7, int limit = 10, List<int>? halaqaFilter = null);
        Task<List<AtRiskStudentDto>> GetAtRiskStudentsAsync(int limit = 20, List<int>? halaqaFilter = null);
        Task<List<AtRiskStudentDto>> GetTeacherAtRiskStudentsAsync(int teacherId, int limit = 10);
    }
}

