using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;

namespace KhairAPI.Controllers
{
    /// <summary>
    /// Controller for resolving association information by subdomain.
    /// This is a public endpoint used by the frontend to fetch tenant info before login.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AssociationResolverController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AssociationResolverController> _logger;

        public AssociationResolverController(AppDbContext context, ILogger<AssociationResolverController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Resolves an association by its subdomain.
        /// Returns basic association info (Id, Name, LogoUrl) for the frontend to display.
        /// </summary>
        /// <param name="subdomain">The subdomain to resolve (e.g., "khair" for khair.maarij.sa)</param>
        /// <returns>Basic association information or 404 if not found</returns>
        [HttpGet("/api/resolve")]
        public async Task<ActionResult<AssociationResolverDto>> ResolveBySubdomain([FromQuery] string subdomain)
        {
            if (string.IsNullOrWhiteSpace(subdomain))
            {
                return BadRequest(new { message = "Subdomain is required" });
            }

            // Normalize subdomain to lowercase for case-insensitive matching
            var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();

            var association = await _context.Associations
                .AsNoTracking()
                .Where(a => a.Subdomain.ToLower() == normalizedSubdomain && a.IsActive)
                .Select(a => new AssociationResolverDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    DisplayName = a.DisplayName,
                    LogoUrl = a.Logo,
                    Favicon = a.Favicon,
                    PrimaryColor = a.PrimaryColor,
                    SecondaryColor = a.SecondaryColor
                })
                .FirstOrDefaultAsync();

            if (association == null)
            {
                _logger.LogWarning("Association not found for subdomain: {Subdomain}", normalizedSubdomain);
                return NotFound(new { message = "Association not found" });
            }

            _logger.LogInformation("Resolved association {AssociationId} for subdomain: {Subdomain}",
                association.Id, normalizedSubdomain);

            return Ok(association);
        }
    }
}
