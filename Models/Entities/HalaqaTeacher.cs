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
        
        // Navigation properties
        public Halaqa Halaqa { get; set; } = null!;
        public Teacher Teacher { get; set; } = null!;
    }
}
