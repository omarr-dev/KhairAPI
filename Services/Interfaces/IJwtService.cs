using KhairAPI.Models.Entities;

namespace KhairAPI.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        /// <summary>
        /// Issues a token for a student (self-service portal). Students have no User row,
        /// so they authenticate directly against the Student entity.
        /// </summary>
        string GenerateTokenForStudent(Student student);
        string GenerateRefreshToken();
        int? ValidateToken(string token);
    }
}

