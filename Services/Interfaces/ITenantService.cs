namespace KhairAPI.Services.Interfaces
{
    /// <summary>
    /// Service for managing the current tenant/association context in a request.
    /// </summary>
    public interface ITenantService
    {
        /// <summary>
        /// Gets the current association ID for the request.
        /// Null indicates no tenant context (e.g., during migrations or system operations).
        /// </summary>
        int? CurrentAssociationId { get; }

        /// <summary>
        /// Gets the current association entity.
        /// </summary>
        Models.Entities.Association? CurrentAssociation { get; }

        /// <summary>
        /// Sets the tenant context for the current request.
        /// </summary>
        /// <param name="associationId">The association ID to set as current tenant.</param>
        void SetTenant(int associationId);
    }
}
