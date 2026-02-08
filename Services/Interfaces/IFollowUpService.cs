using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IFollowUpService
    {
        Task<FollowUpResponseDto> GetFollowUpDataAsync(DateTime date, int? teacherId = null, List<int>? supervisedHalaqaIds = null);
    }
}
