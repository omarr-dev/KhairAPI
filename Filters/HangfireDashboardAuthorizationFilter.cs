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
            var httpContext = context.GetHttpContext();
            
            // In development, allow access to the dashboard
            var env = httpContext.RequestServices.GetService<IWebHostEnvironment>();
            if (env?.IsDevelopment() == true)
            {
                return true;
            }
            
            // In production, require authentication and supervisor role
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }
            
            // Check if user is a supervisor
            return httpContext.User.IsInRole("Supervisor");
        }
    }
}

