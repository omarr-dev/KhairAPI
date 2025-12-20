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
        Task<int?> GetTeacherIdAsync();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;
        private int? _teacherId;
        private bool _teacherIdLoaded;

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

        public bool IsSupervisor => Role == "Supervisor";

        public bool IsTeacher => Role == "Teacher";

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
    }
}

