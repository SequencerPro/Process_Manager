using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

/// <summary>
/// Public, unauthenticated signup endpoint (M2). Provisions a new tenant, its first
/// admin user, default vocabulary, feature flags, and onboarding state in a single
/// atomic operation, then returns a JWT the caller uses to start the onboarding wizard.
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicSignupController : ControllerBase
{
    private static readonly Regex SubdomainRx = new("^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$", RegexOptions.Compiled);

    private readonly ProcessManagerDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITenantContext _tenantContext;
    private readonly JwtTokenService _jwt;

    public PublicSignupController(
        ProcessManagerDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITenantContext tenantContext,
        JwtTokenService jwt)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantContext = tenantContext;
        _jwt = jwt;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<PublicSignupResultDto>> Signup([FromBody] PublicSignupDto dto)
    {
        // ── Validation ────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            return BadRequest("Company name is required.");
        if (string.IsNullOrWhiteSpace(dto.Subdomain))
            return BadRequest("Subdomain is required.");
        if (string.IsNullOrWhiteSpace(dto.AdminUserName))
            return BadRequest("Admin user name is required.");
        if (!new EmailAddressAttribute().IsValid(dto.AdminEmail))
            return BadRequest("Admin email is invalid.");
        if (string.IsNullOrWhiteSpace(dto.AdminPassword) || dto.AdminPassword.Length < 8)
            return BadRequest("Admin password must be at least 8 characters.");

        var normalizedSubdomain = dto.Subdomain.Trim().ToLowerInvariant();
        if (!SubdomainRx.IsMatch(normalizedSubdomain))
            return BadRequest("Subdomain must be 3–63 chars, lowercase alphanumerics or hyphens, not starting or ending with a hyphen.");

        if (await _db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Subdomain == normalizedSubdomain))
            return Conflict($"Subdomain '{normalizedSubdomain}' is already in use.");

        // Username collision check — Identity UserName is unique globally (we do not scope by tenant).
        if (await _userManager.FindByNameAsync(dto.AdminUserName) is not null)
            return Conflict($"User name '{dto.AdminUserName}' is already taken.");

        // Ensure roles exist (idempotent)
        foreach (var role in new[] { "Admin", "Engineer", "Participant" })
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        // ── Provision tenant ──────────────────────────────────────────────────
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Subdomain = normalizedSubdomain,
            Name = dto.CompanyName.Trim(),
            Status = TenantStatus.Trial,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        // Everything below must be stamped with the new tenant's Id — use the
        // tenant context scope so the interceptor stamps all inserts correctly.
        using (_tenantContext.BeginScope(tenant.Id))
        {
            // Admin user
            var adminUser = new ApplicationUser
            {
                UserName = dto.AdminUserName.Trim(),
                Email = dto.AdminEmail.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(dto.AdminDisplayName) ? null : dto.AdminDisplayName.Trim(),
                TenantId = tenant.Id
            };
            var createResult = await _userManager.CreateAsync(adminUser, dto.AdminPassword);
            if (!createResult.Succeeded)
            {
                // Roll back the tenant row to keep things clean.
                _db.Tenants.Remove(tenant);
                await _db.SaveChangesAsync();
                return BadRequest(createResult.Errors.Select(e => e.Description));
            }
            await _userManager.AddToRoleAsync(adminUser, "Admin");

            // Feature flags — MVP surface by default.
            var flags = new TenantFeatureFlags
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                ShowAdvancedModules = false,
                ShowQualityTools = true,
                ShowProductionTools = false,
                ShowWarehouseTools = false,
                ShowTrainingTools = false
            };
            _db.TenantFeatureFlags.Add(flags);

            // Onboarding state
            var state = new TenantOnboardingState
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Industry = dto.Industry,
                CurrentStep = 0,
                SignupAt = DateTime.UtcNow
            };
            _db.TenantOnboardingStates.Add(state);

            // Industry-appropriate default vocabulary.
            var vocab = new DomainVocabulary
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = DataSeeder.ResolveDefaultVocabularyName(dto.Industry),
                IsActive = true,
                TermKind = DataSeeder.ResolveDefaultKindLabel(dto.Industry)
            };
            _db.DomainVocabularies.Add(vocab);

            await _db.SaveChangesAsync();

            // Issue a token for the new admin so the UI can proceed straight to /onboarding.
            var token = _jwt.Generate(adminUser, "Admin");
            return Ok(new PublicSignupResultDto(tenant.Id, tenant.Subdomain, token));
        }
    }
}
