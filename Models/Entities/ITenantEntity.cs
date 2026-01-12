namespace KhairAPI.Models.Entities
{
    /// <summary>
    /// Interface for entities that belong to a specific tenant/association.
    /// Ensures all tenant-scoped entities have AssociationId for data isolation.
    /// </summary>
    public interface ITenantEntity
    {
        int AssociationId { get; set; }
        Association? Association { get; }
    }
}
