using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;

namespace KhairAPI.Services.Interfaces
{
    /// <summary>
    /// Service for managing HalaqaSupervisor assignments to halaqas
    /// </summary>
    public interface IHalaqaSupervisorService
    {
        /// <summary>
        /// Assigns a HalaqaSupervisor to a halaqa
        /// </summary>
        Task<HalaqaSupervisorAssignmentDto> AssignToHalaqaAsync(int userId, int halaqaId);

        /// <summary>
        /// Removes a HalaqaSupervisor from a halaqa
        /// </summary>
        Task<bool> RemoveFromHalaqaAsync(int userId, int halaqaId);

        /// <summary>
        /// Gets all halaqas assigned to a specific HalaqaSupervisor
        /// </summary>
        Task<IEnumerable<HalaqaDto>> GetAssignedHalaqasAsync(int userId);

        /// <summary>
        /// Gets all HalaqaSupervisors assigned to a specific halaqa
        /// </summary>
        Task<IEnumerable<UserDto>> GetSupervisorsForHalaqaAsync(int halaqaId);

        /// <summary>
        /// Gets all HalaqaSupervisor users in the association
        /// </summary>
        Task<IEnumerable<UserDto>> GetAllHalaqaSupervisorsAsync();
    }
}
