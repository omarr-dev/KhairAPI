using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    /// <summary>
    /// Controller for resolving association information by subdomain and public registration.
    /// These are public endpoints used by the frontend.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AssociationResolverController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AssociationResolverController> _logger;
        private readonly IConfiguration _configuration;

        public AssociationResolverController(
            AppDbContext context, 
            ILogger<AssociationResolverController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
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

        /// <summary>
        /// Creates a new association with an initial supervisor user.
        /// This is a public endpoint for the landing page registration.
        /// </summary>
        /// <param name="dto">The association creation data</param>
        /// <returns>The created association info with login URL</returns>
        [HttpPost("/api/associations/register")]
        public async Task<ActionResult<CreateAssociationResponseDto>> CreateAssociation([FromBody] CreateAssociationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Normalize subdomain to lowercase
            var normalizedSubdomain = dto.Subdomain.Trim().ToLowerInvariant();

            // Check if subdomain already exists
            var existingAssociation = await _context.Associations
                .IgnoreQueryFilters() // Bypass tenant filter for this check
                .AnyAsync(a => a.Subdomain.ToLower() == normalizedSubdomain);

            if (existingAssociation)
            {
                _logger.LogWarning("Attempted to register with existing subdomain: {Subdomain}", normalizedSubdomain);
                return Conflict(new { message = "الاسم الفرعي للرابط مستخدم بالفعل. يرجى اختيار اسم آخر." });
            }

            // Format and validate phone number
            var formattedPhone = PhoneNumberValidator.Format(dto.ManagerPhoneNumber);
            if (formattedPhone == null)
            {
                return BadRequest(new { message = "رقم الجوال غير صحيح. يجب أن يكون رقم سعودي يبدأ بـ +966 5" });
            }

            // Check if phone number is already registered
            var existingUser = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.PhoneNumber == formattedPhone);

            if (existingUser)
            {
                _logger.LogWarning("Attempted to register with existing phone number: {PhoneNumber}", formattedPhone);
                return Conflict(new { message = "رقم الجوال مسجل بالفعل. يرجى تسجيل الدخول أو استخدام رقم آخر." });
            }

            // Use transaction for atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Create the association
                var association = new Association
                {
                    Name = dto.AssociationName.Trim(),
                    Subdomain = normalizedSubdomain,
                    Description = dto.Description?.Trim(),
                    Country = dto.Country?.Trim(),
                    City = dto.City?.Trim(),
                    Logo = dto.Logo?.Trim(),
                    ManagerName = dto.ManagerName.Trim(),
                    PhoneNumber = formattedPhone,
                    Email = dto.Email?.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Associations.Add(association);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new association: {AssociationId} - {AssociationName} ({Subdomain})",
                    association.Id, association.Name, normalizedSubdomain);

                // Create the supervisor user
                var user = new User
                {
                    FullName = dto.ManagerName.Trim(),
                    PhoneNumber = formattedPhone,
                    Role = UserRole.Supervisor,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    AssociationId = association.Id
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created supervisor user: {UserId} for association: {AssociationId}",
                    user.Id, association.Id);

                await transaction.CommitAsync();

                // Build the login URL
                var baseDomain = _configuration["AppSettings:BaseDomain"] ?? "maarij.sa";
                var loginUrl = $"https://{normalizedSubdomain}.{baseDomain}";

                return CreatedAtAction(
                    nameof(ResolveBySubdomain),
                    new { subdomain = normalizedSubdomain },
                    new CreateAssociationResponseDto
                    {
                        AssociationId = association.Id,
                        AssociationName = association.Name,
                        Subdomain = normalizedSubdomain,
                        Message = "تم إنشاء الجمعية بنجاح. يمكنك الآن تسجيل الدخول باستخدام رقم الجوال المسجل.",
                        LoginUrl = loginUrl
                    });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create association: {AssociationName}", dto.AssociationName);
                return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء الجمعية. يرجى المحاولة مرة أخرى." });
            }
        }
    }
}

