namespace KhairAPI.Models.DTOs
{
    /// <summary>
    /// Minimal id/name pair for dropdowns and filters
    /// </summary>
    public class LookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
