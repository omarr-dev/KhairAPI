using System.Collections.Generic;

namespace KhairAPI.Models.DTOs
{
    /// <summary>
    /// Generic paginated response wrapper
    /// </summary>
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }

    /// <summary>
    /// Student filter parameters
    /// </summary>
    public class StudentFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? HalaqaId { get; set; }
        /// <summary>
        /// For filtering by multiple halaqas (used by HalaqaSupervisors)
        /// </summary>
        public List<int>? HalaqaIds { get; set; }
        public int? TeacherId { get; set; }
        public string? SortBy { get; set; } = "name"; // name, juz, createdAt
        public string? SortOrder { get; set; } = "asc"; // asc, desc
    }

    /// <summary>
    /// Teacher filter parameters
    /// </summary>
    public class TeacherFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? HalaqaId { get; set; }
        /// <summary>
        /// For filtering by multiple halaqas (used by HalaqaSupervisors)
        /// </summary>
        public List<int>? HalaqaIds { get; set; }
        public string? SortBy { get; set; } = "name"; // name, studentsCount, halaqatCount
        public string? SortOrder { get; set; } = "asc"; // asc, desc
    }
}


