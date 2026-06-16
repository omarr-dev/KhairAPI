using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IHalaqaService
    {
        Task<IEnumerable<HalaqaDto>> GetAllHalaqatAsync(int? teacherId = null, List<int>? supervisedHalaqaIds = null);
        Task<List<LookupDto>> GetHalaqatLookupAsync(int? teacherId = null, List<int>? supervisedHalaqaIds = null);
        Task<PaginatedResponse<HalaqaHierarchyDto>> GetHalaqatHierarchyAsync(HalaqaHierarchyFilterDto filter, List<int>? supervisedHalaqaIds = null);
        Task<List<StudentInHalaqaWithTeacherDto>> GetHalaqaStudentsAsync(int halaqaId);
        Task<HalaqaDto?> GetHalaqaByIdAsync(int id);
        Task<HalaqaDto> CreateHalaqaAsync(CreateHalaqaDto dto);
        Task<bool> UpdateHalaqaAsync(int id, UpdateHalaqaDto dto);
        Task<bool> DeleteHalaqaAsync(int id);
    }
}

