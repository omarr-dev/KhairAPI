using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Services.Implementations
{
    /// <summary>
    /// Service for managing HalaqaSupervisor assignments to halaqas
    /// </summary>
    public class HalaqaSupervisorService : IHalaqaSupervisorService
    {
        private readonly AppDbContext _context;
        private readonly ITenantService _tenantService;

        public HalaqaSupervisorService(AppDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<HalaqaSupervisorAssignmentDto> AssignToHalaqaAsync(int userId, int halaqaId)
        {
            // Verify user exists and is a HalaqaSupervisor
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("المستخدم غير موجود");

            if (user.Role != UserRole.HalaqaSupervisor)
                throw new InvalidOperationException("المستخدم ليس مشرف حلقة");

            // Verify halaqa exists
            var halaqa = await _context.Halaqat
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == halaqaId);

            if (halaqa == null)
                throw new InvalidOperationException(AppConstants.ErrorMessages.HalaqaNotFound);

            // Check if assignment already exists
            var existingAssignment = await _context.HalaqaSupervisorAssignments
                .FirstOrDefaultAsync(a => a.UserId == userId && a.HalaqaId == halaqaId);

            if (existingAssignment != null)
            {
                // Reactivate if inactive
                if (!existingAssignment.IsActive)
                {
                    existingAssignment.IsActive = true;
                    existingAssignment.AssignedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                
                return new HalaqaSupervisorAssignmentDto
                {
                    Id = existingAssignment.Id,
                    UserId = userId,
                    UserFullName = user.FullName,
                    HalaqaId = halaqaId,
                    HalaqaName = halaqa.Name,
                    AssignedDate = existingAssignment.AssignedDate,
                    IsActive = existingAssignment.IsActive
                };
            }

            // Create new assignment
            var assignment = new HalaqaSupervisorAssignment
            {
                UserId = userId,
                HalaqaId = halaqaId,
                AssignedDate = DateTime.UtcNow,
                IsActive = true,
                AssociationId = _tenantService.CurrentAssociationId ?? throw new InvalidOperationException("لم يتم تحديد الجمعية")
            };

            _context.HalaqaSupervisorAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return new HalaqaSupervisorAssignmentDto
            {
                Id = assignment.Id,
                UserId = userId,
                UserFullName = user.FullName,
                HalaqaId = halaqaId,
                HalaqaName = halaqa.Name,
                AssignedDate = assignment.AssignedDate,
                IsActive = assignment.IsActive
            };
        }

        public async Task<bool> RemoveFromHalaqaAsync(int userId, int halaqaId)
        {
            var assignment = await _context.HalaqaSupervisorAssignments
                .FirstOrDefaultAsync(a => a.UserId == userId && a.HalaqaId == halaqaId);

            if (assignment == null)
                return false;

            // Soft delete - set as inactive
            assignment.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<HalaqaDto>> GetAssignedHalaqasAsync(int userId)
        {
            return await _context.HalaqaSupervisorAssignments
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.IsActive)
                .Select(a => new HalaqaDto
                {
                    Id = a.Halaqa!.Id,
                    Name = a.Halaqa.Name,
                    Location = a.Halaqa.Location,
                    TimeSlot = a.Halaqa.TimeSlot,
                    ActiveDays = a.Halaqa.ActiveDays,
                    IsActive = a.Halaqa.IsActive
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<UserDto>> GetSupervisorsForHalaqaAsync(int halaqaId)
        {
            return await _context.HalaqaSupervisorAssignments
                .AsNoTracking()
                .Where(a => a.HalaqaId == halaqaId && a.IsActive)
                .Select(a => new UserDto
                {
                    Id = a.User!.Id,
                    FullName = a.User.FullName,
                    PhoneNumber = a.User.PhoneNumber,
                    Role = a.User.Role.ToString()
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<UserDto>> GetAllHalaqaSupervisorsAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.HalaqaSupervisor && u.IsActive)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role.ToString(),
                    SupervisedHalaqaIds = u.HalaqaAssignments
                        .Where(a => a.IsActive)
                        .Select(a => a.HalaqaId)
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task<UserDto?> UpdateHalaqaSupervisorAsync(int userId, UpdateUserDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.Role == UserRole.HalaqaSupervisor);

            if (user == null)
                return null;

            // Format and validate the phone number (same rules as registration)
            var formattedPhone = PhoneNumberValidator.Format(dto.PhoneNumber);
            if (formattedPhone == null)
                throw new InvalidOperationException(AppConstants.ErrorMessages.InvalidPhoneNumber);

            // Ensure the phone number isn't already used by another user
            if (await _context.Users.AnyAsync(u => u.PhoneNumber == formattedPhone && u.Id != userId))
                throw new InvalidOperationException(AppConstants.ErrorMessages.PhoneNumberAlreadyExists);

            user.FullName = dto.FullName;
            user.PhoneNumber = formattedPhone;
            await _context.SaveChangesAsync();

            var supervisedHalaqaIds = await _context.HalaqaSupervisorAssignments
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.IsActive)
                .Select(a => a.HalaqaId)
                .ToListAsync();

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                SupervisedHalaqaIds = supervisedHalaqaIds
            };
        }

        public async Task<bool> DeactivateHalaqaSupervisorAsync(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.Role == UserRole.HalaqaSupervisor);

            if (user == null)
                return false;

            user.IsActive = false;

            // Deactivate all of their active halaqa assignments
            var assignments = await _context.HalaqaSupervisorAssignments
                .Where(a => a.UserId == userId && a.IsActive)
                .ToListAsync();

            foreach (var assignment in assignments)
                assignment.IsActive = false;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
