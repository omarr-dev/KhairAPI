using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    public class StudentHalaqa
    {
        public int StudentId { get; set; }
        public int HalaqaId { get; set; }
        public int TeacherId { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public Student Student { get; set; } = null!;
        public Halaqa Halaqa { get; set; } = null!;
        public Teacher Teacher { get; set; } = null!;
    }
}
