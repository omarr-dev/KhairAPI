using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface ITeacherService
    {
        Task<IEnumerable<TeacherDto>> GetAllTeachersAsync();
        /// <summary>
        /// Get teachers assigned to any of the specified halaqas (for HalaqaSupervisors)
        /// </summary>
        Task<IEnumerable<TeacherDto>> GetTeachersByHalaqasAsync(List<int> halaqaIds);
        Task<PaginatedResponse<TeacherDto>> GetTeachersPaginatedAsync(TeacherFilterDto filter);
        Task<TeacherDto?> GetTeacherByIdAsync(int id);
        Task<IEnumerable<TeacherDto>> GetTeachersByHalaqaAsync(int halaqaId);
        Task<TeacherDto> CreateTeacherAsync(CreateTeacherDto dto);
        Task<bool> UpdateTeacherAsync(int id, UpdateTeacherDto dto);
        Task<bool> DeleteTeacherAsync(int id);
        Task<bool> AssignTeacherToHalaqaAsync(int teacherId, int halaqaId, bool isPrimary = false);
        Task<IEnumerable<TeacherHalaqaDto>> GetTeacherHalaqatAsync(int teacherId);
        Task<bool> RemoveTeacherFromHalaqaAsync(int teacherId, int halaqaId);
    }
}
