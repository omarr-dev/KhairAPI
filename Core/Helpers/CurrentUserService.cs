using System.Security.Claims;
using KhairAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace KhairAPI.Core.Helpers
{
    /// <summary>
    /// Service to access current authenticated user information
    /// </summary>
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? PhoneNumber { get; }
        string? FullName { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
        bool IsSupervisor { get; }
        bool IsTeacher { get; }
        /// <summary>
        /// True if the user is a HalaqaSupervisor (limited scope supervisor)
        /// </summary>
        bool IsHalaqaSupervisor { get; }
        /// <summary>
        /// True if user is either Supervisor or HalaqaSupervisor
        /// </summary>
        bool HasSupervisorRole { get; }
        Task<int?> GetTeacherIdAsync();
        /// <summary>
        /// Gets the list of halaqa IDs that a HalaqaSupervisor can manage.
        /// Returns null for full Supervisors (unlimited access).
        /// Returns empty list for Teachers (no supervisor access).
        /// </summary>
        Task<List<int>?> GetSupervisedHalaqaIdsAsync();
        /// <summary>
        /// Checks if the current HalaqaSupervisor has access to a specific halaqa.
        /// Full Supervisors always return true.
        /// Teachers always return false.
        /// </summary>
        Task<bool> CanAccessHalaqaAsync(int halaqaId);
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;
        private int? _teacherId;
        private bool _teacherIdLoaded;
        private List<int>? _supervisedHalaqaIds;
        private bool _supervisedHalaqaIdsLoaded;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public int? UserId
        {
            get
            {
                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(userIdClaim, out var userId) ? userId : null;
            }
        }

        public string? PhoneNumber => User?.FindFirst(ClaimTypes.MobilePhone)?.Value;

        public string? FullName => User?.FindFirst(ClaimTypes.Name)?.Value;

        public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public bool IsSupervisor => Role == AppConstants.Roles.Supervisor;

        public bool IsTeacher => Role == AppConstants.Roles.Teacher;

        public bool IsHalaqaSupervisor => Role == AppConstants.Roles.HalaqaSupervisor;

        public bool HasSupervisorRole => IsSupervisor || IsHalaqaSupervisor;

        public async Task<int?> GetTeacherIdAsync()
        {
            if (_teacherIdLoaded)
                return _teacherId;

            if (!UserId.HasValue)
            {
                _teacherIdLoaded = true;
                return null;
            }

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == UserId.Value);

            _teacherId = teacher?.Id;
            _teacherIdLoaded = true;

            return _teacherId;
        }

        public async Task<List<int>?> GetSupervisedHalaqaIdsAsync()
        {
            // Full supervisors have unlimited access
            if (IsSupervisor)
                return null;

            // Teachers don't have supervisor access
            if (IsTeacher)
                return new List<int>();

            // Cache the result for HalaqaSupervisors
            if (_supervisedHalaqaIdsLoaded)
                return _supervisedHalaqaIds;

            if (!UserId.HasValue || !IsHalaqaSupervisor)
            {
                _supervisedHalaqaIdsLoaded = true;
                _supervisedHalaqaIds = new List<int>();
                return _supervisedHalaqaIds;
            }

            // Fetch assigned halaqa IDs efficiently
            _supervisedHalaqaIds = await _context.HalaqaSupervisorAssignments
                .AsNoTracking()
                .Where(a => a.UserId == UserId.Value && a.IsActive)
                .Select(a => a.HalaqaId)
                .ToListAsync();

            _supervisedHalaqaIdsLoaded = true;
            return _supervisedHalaqaIds;
        }

        public async Task<bool> CanAccessHalaqaAsync(int halaqaId)
        {
            // Full supervisors can access any halaqa
            if (IsSupervisor)
                return true;

            // Teachers don't have halaqa supervisor access
            if (IsTeacher)
                return false;

            var supervisedHalaqaIds = await GetSupervisedHalaqaIdsAsync();
            return supervisedHalaqaIds?.Contains(halaqaId) ?? false;
        }
    }
}
