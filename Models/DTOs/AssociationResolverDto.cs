namespace KhairAPI.Models.DTOs
{
    /// <summary>
    /// DTO returned by the association resolver endpoint.
    /// Contains basic association info for the frontend to display before login.
    /// </summary>
    public class AssociationResolverDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? LogoUrl { get; set; }
        public string? Favicon { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
    }
}
