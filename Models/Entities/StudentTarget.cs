using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    /// <summary>
    /// Represents daily targets for a student's memorization, revision, and consolidation.
    /// - Memorization target: measured in lines (سطر)
    /// - Revision target: measured in pages (وجه)
    /// - Consolidation target: measured in pages (وجه)
    /// </summary>
    public class StudentTarget : ITenantEntity
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        /// <summary>
        /// Target lines per day for memorization (حفظ) - سطر
        /// </summary>
        public int? MemorizationLinesTarget { get; set; }

        /// <summary>
        /// Target pages per day for revision (مراجعة) - وجه
        /// </summary>
        public int? RevisionPagesTarget { get; set; }

        /// <summary>
        /// Target pages per day for consolidation (التثبيت) - وجه
        /// </summary>
        public int? ConsolidationPagesTarget { get; set; }

        /// <summary>
        /// Current consecutive days of meeting all set targets
        /// </summary>
        public int CurrentStreak { get; set; } = 0;

        /// <summary>
        /// Longest streak ever achieved 
        /// </summary>
        public int LongestStreak { get; set; } = 0;

        /// <summary>
        /// Date when the streak was last updated (for preventing duplicate updates same day)
        /// </summary>
        public DateTime? LastStreakDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public Student Student { get; set; } = null!;
    }
}
