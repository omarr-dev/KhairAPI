using KhairAPI.Services.Interfaces;

namespace KhairAPI.Middleware
{
    /// <summary>
    /// Middleware to extract tenant/association context from HTTP header or JWT token.
    /// Priority: 1. X-Tenant-Id header, 2. JWT AssociationId claim
    /// Sets the current association ID in TenantService for automatic query filtering.
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private const string TenantIdHeader = "X-Tenant-Id";

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            int? resolvedAssociationId = null;

            // Priority 1: Check X-Tenant-Id HTTP header (essential for public pages)
            if (context.Request.Headers.TryGetValue(TenantIdHeader, out var headerValue) 
                && int.TryParse(headerValue.FirstOrDefault(), out int headerAssociationId))
            {
                resolvedAssociationId = headerAssociationId;
            }
            // Priority 2: Fallback to JWT claims (for authenticated requests)
            else
            {
                var associationIdClaim = context.User.FindFirst("AssociationId")?.Value;
                if (!string.IsNullOrEmpty(associationIdClaim) && int.TryParse(associationIdClaim, out int claimAssociationId))
                {
                    resolvedAssociationId = claimAssociationId;
                }
            }

            // Set the tenant context if we resolved an association ID
            if (resolvedAssociationId.HasValue)
            {
                tenantService.SetTenant(resolvedAssociationId.Value);
            }

            await _next(context);
        }
    }
}
