using Hangfire.Dashboard;

namespace KhairAPI
{
    /// <summary>
    /// Authorization filter for Hangfire Dashboard.
    /// In development, allows all access.
    /// In production, should be restricted to supervisors only.
    /// </summary>
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // Allow access to dashboard in all environments
            return true;
        }
    }
}

