using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IJwtService jwtService, IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            // Format and validate phone number
            var formattedPhone = PhoneNumberValidator.Format(loginDto.PhoneNumber);
            if (formattedPhone == null)
                return null;

            var user = await _context.Users
                .Include(u => u.Teacher)
                .AsSplitQuery()
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone && u.IsActive);

            if (user == null)
                return null;

            // TODO: Add OTP verification here

            return GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            // Format and validate phone number
            var formattedPhone = PhoneNumberValidator.Format(registerDto.PhoneNumber);
            if (formattedPhone == null)
                return null;

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == formattedPhone))
                return null;

            var user = new User
            {
                PhoneNumber = formattedPhone,
                FullName = registerDto.FullName,
                Role = registerDto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (registerDto.Role == UserRole.Teacher)
            {
                var teacher = new Teacher
                {
                    UserId = user.Id,
                    FullName = registerDto.FullName,
                    PhoneNumber = formattedPhone,
                    Qualification = registerDto.Qualification,
                    JoinDate = DateTime.UtcNow
                };

                _context.Teachers.Add(teacher);
                await _context.SaveChangesAsync();
            }

            user = await _context.Users
                .Include(u => u.Teacher)
                .AsSplitQuery()
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            return GenerateAuthResponse(user!);
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Refresh token functionality needs to be implemented with proper storage");
        }

        public async Task<bool> RevokeTokenAsync(int userId)
        {
            await Task.CompletedTask;
            return true;
        }

        private AuthResponseDto GenerateAuthResponse(User user)
        {
            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "1440");

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                User = new UserDto
                {
                    Id = user.Id,
                    PhoneNumber = user.PhoneNumber,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    TeacherId = user.Teacher?.Id
                }
            };
        }
    }
}

