using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        /// <summary>
        /// Logs a student in by National ID within the current tenant. Returns null if no student
        /// matches; throws InvalidOperationException if the NID is ambiguous (duplicate within tenant).
        /// </summary>
        Task<AuthResponseDto?> StudentLoginAsync(StudentLoginDto loginDto);
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(int userId);
    }
}

