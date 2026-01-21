using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    /// <summary>
    /// Represents a HalaqaSupervisor's assignment to manage a specific halaqa.
    /// A HalaqaSupervisor can be assigned to multiple halaqas.
    /// </summary>
    public class HalaqaSupervisorAssignment : ITenantEntity
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int HalaqaId { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Halaqa? Halaqa { get; set; }
    }
}
