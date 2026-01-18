using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    /// <summary>
    /// Tracks daily achievement against targets for historical reporting.
    /// Records what the target was and what was actually achieved on each day.
    /// </summary>
    public class TargetAchievement : ITenantEntity
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        /// <summary>
        /// The date this achievement record is for
        /// </summary>
        [Required]
        public DateTime Date { get; set; }

        // Targets at time of recording (nullable - captures what was set)
        public int? MemorizationLinesTarget { get; set; }
        public int? RevisionPagesTarget { get; set; }
        public int? ConsolidationPagesTarget { get; set; }

        // Actual achievements (calculated from ProgressRecords for the day)
        public int MemorizationLinesAchieved { get; set; }
        public int RevisionPagesAchieved { get; set; }
        public int ConsolidationPagesAchieved { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public Student Student { get; set; } = null!;
    }
}
