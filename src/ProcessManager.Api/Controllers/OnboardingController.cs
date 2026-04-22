using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

/// <summary>
/// Authenticated endpoints that drive the first-run onboarding wizard (M2):
/// reading and updating the per-tenant <see cref="TenantOnboardingState"/>,
/// skipping with sample-content seeding, and managing the module feature flags.
/// </summary>
[ApiController]
[Authorize]
[Route("api/onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly ITenantContext _tenantContext;

    public OnboardingController(ProcessManagerDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    // ── GET api/onboarding/industries ────────────────────────────────────────

    [AllowAnonymous]
    [HttpGet("industries")]
    public ActionResult<IEnumerable<OnboardingIndustryOptionDto>> GetIndustries()
    {
        return Ok(new[]
        {
            new OnboardingIndustryOptionDto(OnboardingIndustry.CNC,     "CNC Machining", "Milling, turning, grinding, EDM — part-centric with dimensional inspection."),
            new OnboardingIndustryOptionDto(OnboardingIndustry.PCBA,    "PCB Assembly",  "SMT, wave solder, inspection — panel/board centric with test coverage."),
            new OnboardingIndustryOptionDto(OnboardingIndustry.Medical, "Medical Device","Regulated manufacture with full traceability and documentation."),
            new OnboardingIndustryOptionDto(OnboardingIndustry.General, "General",       "Any repetitive manufacturing operation — a neutral starting point.")
        });
    }

    // ── GET api/onboarding/state ─────────────────────────────────────────────

    [HttpGet("state")]
    public async Task<ActionResult<OnboardingStateDto>> GetState()
    {
        var state = await _db.TenantOnboardingStates.FirstOrDefaultAsync();
        if (state is null)
        {
            // Tenants that existed before M2 have no row. Lazily create one so the UI
            // always has something to render.
            state = new TenantOnboardingState
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.CurrentTenantId,
                Industry = OnboardingIndustry.General,
                CurrentStep = 0,
                SignupAt = DateTime.UtcNow,
                // Tenants that pre-exist the wizard are considered "already onboarded";
                // otherwise a freshly-logged-in legacy admin would be pushed into the wizard.
                CompletedAt = DateTime.UtcNow
            };
            _db.TenantOnboardingStates.Add(state);
            await _db.SaveChangesAsync();
        }

        return Ok(MapState(state));
    }

    // ── PATCH api/onboarding/state ───────────────────────────────────────────

    [HttpPatch("state")]
    public async Task<ActionResult<OnboardingStateDto>> UpdateState([FromBody] UpdateOnboardingStepDto dto)
    {
        var state = await _db.TenantOnboardingStates.FirstOrDefaultAsync();
        if (state is null) return NotFound("Onboarding state not found for current tenant.");

        if (dto.CurrentStep < 0 || dto.CurrentStep > 5)
            return BadRequest("CurrentStep must be between 0 and 5.");

        state.CurrentStep = dto.CurrentStep;
        if (dto.FirstKindId is not null) state.FirstKindId = dto.FirstKindId;
        if (dto.FirstStepTemplateId is not null) state.FirstStepTemplateId = dto.FirstStepTemplateId;
        if (dto.FirstProcessId is not null) state.FirstProcessId = dto.FirstProcessId;
        if (dto.FirstJobId is not null) state.FirstJobId = dto.FirstJobId;

        // Step 5 is the "finished" sentinel.
        if (dto.CurrentStep == 5 && state.CompletedAt is null)
            state.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapState(state));
    }

    // ── POST api/onboarding/skip ─────────────────────────────────────────────

    [HttpPost("skip")]
    public async Task<ActionResult<OnboardingStateDto>> Skip([FromBody] SkipOnboardingDto dto)
    {
        var state = await _db.TenantOnboardingStates.FirstOrDefaultAsync();
        if (state is null) return NotFound("Onboarding state not found for current tenant.");

        state.SkippedAt = DateTime.UtcNow;
        state.CompletedAt = DateTime.UtcNow;
        state.CurrentStep = 5;

        if (dto.SeedSample)
        {
            var ids = await DataSeeder.SeedSampleProcessAsync(_db, state.Industry, _tenantContext.CurrentTenantId);
            state.FirstKindId ??= ids.KindId;
            state.FirstStepTemplateId ??= ids.StepTemplateId;
            state.FirstProcessId ??= ids.ProcessId;
        }

        await _db.SaveChangesAsync();
        return Ok(MapState(state));
    }

    // ── POST api/onboarding/seed-sample ──────────────────────────────────────

    [HttpPost("seed-sample")]
    public async Task<ActionResult<OnboardingStateDto>> SeedSample()
    {
        var state = await _db.TenantOnboardingStates.FirstOrDefaultAsync();
        if (state is null) return NotFound("Onboarding state not found for current tenant.");

        var ids = await DataSeeder.SeedSampleProcessAsync(_db, state.Industry, _tenantContext.CurrentTenantId);
        state.FirstKindId ??= ids.KindId;
        state.FirstStepTemplateId ??= ids.StepTemplateId;
        state.FirstProcessId ??= ids.ProcessId;
        await _db.SaveChangesAsync();
        return Ok(MapState(state));
    }

    // ── GET api/onboarding/feature-flags ─────────────────────────────────────

    [HttpGet("feature-flags")]
    public async Task<ActionResult<TenantFeatureFlagsDto>> GetFlags()
    {
        var flags = await GetOrCreateFlagsAsync();
        return Ok(MapFlags(flags));
    }

    // ── PUT api/onboarding/feature-flags (Admin only) ────────────────────────

    [Authorize(Roles = "Admin")]
    [HttpPut("feature-flags")]
    public async Task<ActionResult<TenantFeatureFlagsDto>> UpdateFlags([FromBody] TenantFeatureFlagsDto dto)
    {
        var flags = await GetOrCreateFlagsAsync();
        flags.ShowAdvancedModules = dto.ShowAdvancedModules;
        flags.ShowQualityTools = dto.ShowQualityTools;
        flags.ShowProductionTools = dto.ShowProductionTools;
        flags.ShowWarehouseTools = dto.ShowWarehouseTools;
        flags.ShowTrainingTools = dto.ShowTrainingTools;
        await _db.SaveChangesAsync();
        return Ok(MapFlags(flags));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<TenantFeatureFlags> GetOrCreateFlagsAsync()
    {
        var flags = await _db.TenantFeatureFlags.FirstOrDefaultAsync();
        if (flags is null)
        {
            flags = new TenantFeatureFlags
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.CurrentTenantId,
                // Legacy tenants that pre-exist the flags table see the full UI so we
                // do not silently hide modules they were already using.
                ShowAdvancedModules = true,
                ShowQualityTools = true,
                ShowProductionTools = true,
                ShowWarehouseTools = true,
                ShowTrainingTools = true
            };
            _db.TenantFeatureFlags.Add(flags);
            await _db.SaveChangesAsync();
        }
        return flags;
    }

    private static OnboardingStateDto MapState(TenantOnboardingState s) =>
        new(s.Id, s.Industry, s.CurrentStep,
            s.CompletedAt is not null,
            s.SkippedAt is not null,
            s.FirstKindId, s.FirstStepTemplateId, s.FirstProcessId, s.FirstJobId,
            s.SignupAt, s.CompletedAt, s.SkippedAt, s.FirstJobCompletedAt);

    private static TenantFeatureFlagsDto MapFlags(TenantFeatureFlags f) =>
        new(f.ShowAdvancedModules, f.ShowQualityTools, f.ShowProductionTools, f.ShowWarehouseTools, f.ShowTrainingTools);
}
