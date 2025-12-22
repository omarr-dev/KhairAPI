using System;
using System.ComponentModel.DataAnnotations;
using KhairAPI.Core.Attributes;

namespace KhairAPI.Models.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        [Required]
        [SaudiPhoneNumber]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Teacher? Teacher { get; set; }
    }

    public enum UserRole
    {
        Teacher,
        Supervisor
    }
}
