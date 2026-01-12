using KhairAPI.Services.Interfaces;
using KhairAPI.Models.Entities;

namespace KhairAPI.Services
{
    /// <summary>
    /// Scoped service that holds the current tenant/association context for a request.
    /// This is set from the JWT claims during authentication.
    /// </summary>
    public class TenantService : ITenantService
    {
        private int? _currentAssociationId;
        private Association? _currentAssociation;

        public int? CurrentAssociationId => _currentAssociationId;

        public Association? CurrentAssociation => _currentAssociation;

        public void SetTenant(int associationId)
        {
            _currentAssociationId = associationId;
        }

        public void SetTenantWithAssociation(Association association)
        {
            _currentAssociation = association;
            _currentAssociationId = association.Id;
        }
    }
}
