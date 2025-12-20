using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Models.DTOs;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUserService;

        public AuthController(IAuthService authService, ICurrentUserService currentUserService)
        {
            _authService = authService;
            _currentUserService = currentUserService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);

            if (result == null)
                return Unauthorized(new { message = AppConstants.ErrorMessages.InvalidCredentials });

            return Ok(result);
        }

        [HttpPost("register")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);

            if (result == null)
                return BadRequest(new { message = AppConstants.ErrorMessages.EmailAlreadyExists });

            return CreatedAtAction(nameof(GetCurrentUser), new { }, result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            if (!_currentUserService.UserId.HasValue)
                return Unauthorized();

            var teacherId = await _currentUserService.GetTeacherIdAsync();

            var userDto = new UserDto
            {
                Id = _currentUserService.UserId.Value,
                Email = _currentUserService.Email ?? "",
                FullName = _currentUserService.FullName ?? "",
                Role = _currentUserService.Role ?? "",
                TeacherId = teacherId
            };

            return Ok(userDto);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);

                if (result == null)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.InvalidToken });

                return Ok(result);
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new { message = "وظيفة تحديث الرمز قيد التطوير" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            if (!_currentUserService.UserId.HasValue)
                return Unauthorized();

            await _authService.RevokeTokenAsync(_currentUserService.UserId.Value);

            return Ok(new { message = AppConstants.SuccessMessages.LogoutSuccess });
        }
    }

    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
