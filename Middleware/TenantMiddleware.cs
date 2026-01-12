using KhairAPI.Services.Interfaces;

namespace KhairAPI.Middleware
{
    /// <summary>
    /// Middleware to extract tenant/association context from JWT token.
    /// Sets the current association ID in TenantService for automatic query filtering.
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            // Extract AssociationId from JWT claims
            var associationIdClaim = context.User.FindFirst("AssociationId")?.Value;

            if (!string.IsNullOrEmpty(associationIdClaim) && int.TryParse(associationIdClaim, out int associationId))
            {
                // Set the tenant context for this request
                tenantService.SetTenant(associationId);
            }

            await _next(context);
        }
    }
}
