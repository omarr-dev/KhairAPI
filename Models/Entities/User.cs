using System;
using System.ComponentModel.DataAnnotations;
using KhairAPI.Core.Attributes;

namespace KhairAPI.Models.Entities
{
    public class User : ITenantEntity
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

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation property
        public Teacher? Teacher { get; set; }
        
        /// <summary>
        /// Halaqa assignments for HalaqaSupervisor role users
        /// </summary>
        public ICollection<HalaqaSupervisorAssignment> HalaqaAssignments { get; set; } = new List<HalaqaSupervisorAssignment>();
    }

    public enum UserRole
    {
        Teacher,
        Supervisor,
        /// <summary>
        /// Limited supervisor role with access scoped to specific halaqas
        /// </summary>
        HalaqaSupervisor
    }
}

