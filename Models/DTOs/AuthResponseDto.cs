namespace KhairAPI.Models.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? TeacherId { get; set; }
        /// <summary>
        /// Only populated for HalaqaSupervisor role - list of halaqa IDs they can manage
        /// </summary>
        public List<int>? SupervisedHalaqaIds { get; set; }
    }

    public class HalaqaSupervisorAssignmentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
