using Microsoft.Extensions.Caching.Memory;
using KhairAPI.Services.Interfaces;
using KhairAPI.Services.Implementations;

namespace KhairAPI.Core.Extensions
{
    /// <summary>
    /// Caching configuration and helpers for scalability
    /// </summary>
    public static class CachingExtensions
    {
        /// <summary>
        /// Add memory caching services with recommended settings for 2000+ users
        /// </summary>
        public static IServiceCollection AddCachingServices(this IServiceCollection services)
        {
            services.AddMemoryCache(options =>
            {
                // Set size limit to prevent unbounded growth (in cache entry units)
                options.SizeLimit = 10000;
                // Compact 25% of cache when limit is reached
                options.CompactionPercentage = 0.25;
            });

            // Register CacheService as singleton for thread-safe key tracking
            services.AddSingleton<ICacheService, CacheService>();

            return services;
        }
    }

    /// <summary>
    /// Cache keys used throughout the application
    /// </summary>
    public static class CacheKeys
    {
        // Short-lived cache (5 minutes) - for dashboard stats
        public const string DashboardStats = "dashboard_stats_{0}"; // {0} = associationId
        // Medium-lived cache (15 minutes) - for lists
        public const string HalaqaList = "halaqat_{0}"; // {0} = associationId
        public const string TeacherList = "teachers_{0}";
        
        // Long-lived cache (1 hour) - for reference data
        public const string QuranSurahs = "quran_surahs";
        
        // System-wide stats (no association filter)
        public const string SystemWideStats = "system_wide_stats";
        public const string SupervisorDashboard = "supervisor_dashboard_{0}"; // {0} = associationId or "all"
        public const string HalaqaRanking = "halaqa_ranking_{0}_{1}"; // {0} = days, {1} = limit
        public const string TeacherRanking = "teacher_ranking_{0}_{1}"; // {0} = days, {1} = limit
        
        public static string ForAssociation(string pattern, int associationId) 
            => string.Format(pattern, associationId);
            
        public static string ForParams(string pattern, params object[] args)
            => string.Format(pattern, args);
    }

    /// <summary>
    /// Default cache durations
    /// </summary>
    public static class CacheDurations
    {
        public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Medium = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan Long = TimeSpan.FromHours(1);
    }
}
