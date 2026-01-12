using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    public class HalaqaTeacher
    {
        public int HalaqaId { get; set; }
        public int TeacherId { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        public bool IsPrimary { get; set; } = false;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public Halaqa Halaqa { get; set; } = null!;
        public Teacher Teacher { get; set; } = null!;
    }
}
