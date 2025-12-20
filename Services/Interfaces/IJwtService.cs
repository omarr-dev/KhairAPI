using KhairAPI.Models.Entities;

namespace KhairAPI.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
        int? ValidateToken(string token);
    }
}

