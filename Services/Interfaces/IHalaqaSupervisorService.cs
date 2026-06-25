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

        /// <summary>
        /// Updates a HalaqaSupervisor's basic details (name and phone number).
        /// Returns the updated user, or null if not found / not a HalaqaSupervisor.
        /// </summary>
        Task<UserDto?> UpdateHalaqaSupervisorAsync(int userId, UpdateUserDto dto);

        /// <summary>
        /// Soft-deletes a HalaqaSupervisor: deactivates the user account and all of
        /// their halaqa assignments. Returns false if not found / not a HalaqaSupervisor.
        /// </summary>
        Task<bool> DeactivateHalaqaSupervisorAsync(int userId);
    }
}
