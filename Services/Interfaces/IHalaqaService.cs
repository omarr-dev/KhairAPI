using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IHalaqaService
    {
        Task<IEnumerable<HalaqaDto>> GetAllHalaqatAsync(int? teacherId = null, List<int>? supervisedHalaqaIds = null);
        Task<IEnumerable<HalaqaHierarchyDto>> GetHalaqatHierarchyAsync(List<int>? supervisedHalaqaIds = null);
        Task<HalaqaDto?> GetHalaqaByIdAsync(int id);
        Task<HalaqaDto> CreateHalaqaAsync(CreateHalaqaDto dto);
        Task<bool> UpdateHalaqaAsync(int id, UpdateHalaqaDto dto);
        Task<bool> DeleteHalaqaAsync(int id);
    }
}

