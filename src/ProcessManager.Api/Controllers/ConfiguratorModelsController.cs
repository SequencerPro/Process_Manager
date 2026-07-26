using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Configurator;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

/// <summary>
/// Revision-controlled storage for BoM-configurator product models. A model is
/// an identity (code/name); its product definition lives in immutable,
/// monotonically numbered revisions. Export produces a portable JSON envelope
/// (full revision history included) that can be imported on another machine.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConfiguratorModelsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public ConfiguratorModelsController(ProcessManagerDbContext db) => _db = db;

    // ───── CRUD ─────

    [HttpGet]
    public async Task<ActionResult<List<ConfiguratorModelSummaryDto>>> GetAll([FromQuery] string? search = null)
    {
        var query = _db.ConfiguratorModels.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Code.Contains(search) || m.Name.Contains(search));

        var models = await query.OrderBy(m => m.Name).ToListAsync();
        var ids = models.Select(m => m.Id).ToList();
        var revCounts = await _db.ConfiguratorModelRevisions
            .Where(r => ids.Contains(r.ConfiguratorModelId))
            .GroupBy(r => r.ConfiguratorModelId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        return models.Select(m => new ConfiguratorModelSummaryDto(
            m.Id, m.Code, m.Name, m.Description, m.IsActive, m.CurrentRevision,
            revCounts.GetValueOrDefault(m.Id), m.CreatedAt, m.UpdatedAt, m.UpdatedBy)).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConfiguratorModelDetailDto>> GetById(Guid id)
    {
        var model = await _db.ConfiguratorModels.FirstOrDefaultAsync(m => m.Id == id);
        if (model is null) return NotFound();

        var revisions = await _db.ConfiguratorModelRevisions
            .Where(r => r.ConfiguratorModelId == id)
            .OrderByDescending(r => r.Revision)
            .Select(r => new ConfiguratorRevisionSummaryDto(r.Id, r.Revision, r.Notes, r.CreatedAt, r.CreatedBy))
            .ToListAsync();

        return new ConfiguratorModelDetailDto(model.Id, model.Code, model.Name, model.Description, model.CurrentRevision, revisions);
    }

    [HttpGet("{id:guid}/revisions/{rev:int}")]
    public async Task<ActionResult<ConfiguratorRevisionDto>> GetRevision(Guid id, int rev)
    {
        var r = await _db.ConfiguratorModelRevisions
            .FirstOrDefaultAsync(r => r.ConfiguratorModelId == id && r.Revision == rev);
        if (r is null) return NotFound();
        return new ConfiguratorRevisionDto(r.Id, r.Revision, r.Notes, r.CreatedAt, r.CreatedBy, r.DefinitionJson);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ConfiguratorModelSummaryDto>> Create(ConfiguratorModelCreateDto dto)
    {
        if (ValidateDefinition(dto.DefinitionJson) is { } error)
            return BadRequest(error);

        if (await _db.ConfiguratorModels.AnyAsync(m => m.Code == dto.Code))
            return Conflict($"A configurator model with code '{dto.Code}' already exists.");

        var model = new ConfiguratorModel
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            CurrentRevision = 1,
        };
        model.Revisions.Add(new ConfiguratorModelRevision
        {
            Revision = 1,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? "Initial revision" : dto.Notes,
            DefinitionJson = dto.DefinitionJson,
        });

        _db.ConfiguratorModels.Add(model);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = model.Id },
            new ConfiguratorModelSummaryDto(model.Id, model.Code, model.Name, model.Description,
                model.IsActive, 1, 1, model.CreatedAt, model.UpdatedAt, model.UpdatedBy));
    }

    [HttpPost("{id:guid}/revisions")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ConfiguratorRevisionSummaryDto>> AddRevision(Guid id, ConfiguratorRevisionCreateDto dto)
    {
        if (ValidateDefinition(dto.DefinitionJson) is { } error)
            return BadRequest(error);

        var model = await _db.ConfiguratorModels.FirstOrDefaultAsync(m => m.Id == id);
        if (model is null) return NotFound();

        model.CurrentRevision += 1;
        var rev = new ConfiguratorModelRevision
        {
            ConfiguratorModelId = id,
            Revision = model.CurrentRevision,
            Notes = dto.Notes,
            DefinitionJson = dto.DefinitionJson,
        };
        _db.ConfiguratorModelRevisions.Add(rev);
        await _db.SaveChangesAsync();

        return new ConfiguratorRevisionSummaryDto(rev.Id, rev.Revision, rev.Notes, rev.CreatedAt, rev.CreatedBy);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var model = await _db.ConfiguratorModels.FirstOrDefaultAsync(m => m.Id == id);
        if (model is null) return NotFound();

        _db.ConfiguratorModels.Remove(model); // revisions cascade
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Portable export / import ─────

    [HttpGet("{id:guid}/export")]
    public async Task<ActionResult<ConfiguratorModelExportDto>> Export(Guid id)
    {
        var model = await _db.ConfiguratorModels.FirstOrDefaultAsync(m => m.Id == id);
        if (model is null) return NotFound();

        var revisions = await _db.ConfiguratorModelRevisions
            .Where(r => r.ConfiguratorModelId == id)
            .OrderBy(r => r.Revision)
            .ToListAsync();

        var exportRevs = revisions.Select(r => new ConfiguratorExportRevisionDto(
            r.Revision, r.Notes, r.CreatedAt, r.CreatedBy,
            JsonDocument.Parse(r.DefinitionJson).RootElement.Clone())).ToList();

        return new ConfiguratorModelExportDto(
            ConfiguratorModelExportDto.FormatId, ConfiguratorModelExportDto.Version,
            DateTime.UtcNow, model.Code, model.Name, model.Description,
            model.CurrentRevision, exportRevs);
    }

    [HttpPost("import")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ConfiguratorModelSummaryDto>> Import(ConfiguratorModelExportDto dto)
    {
        if (dto.Format != ConfiguratorModelExportDto.FormatId)
            return BadRequest($"Unrecognised file format '{dto.Format}'. Expected '{ConfiguratorModelExportDto.FormatId}'.");
        if (dto.FormatVersion > ConfiguratorModelExportDto.Version)
            return BadRequest($"File format version {dto.FormatVersion} is newer than this server supports ({ConfiguratorModelExportDto.Version}).");
        if (dto.Revisions is not { Count: > 0 })
            return BadRequest("The file contains no revisions.");

        var revisions = dto.Revisions.OrderBy(r => r.Revision).ToList();
        foreach (var r in revisions)
        {
            var json = r.Definition.GetRawText();
            if (ValidateDefinition(json) is { } error)
                return BadRequest($"Revision {r.Revision}: {error}");
        }

        // Auto-unique the code if it collides with an existing model.
        var code = dto.Code;
        if (await _db.ConfiguratorModels.AnyAsync(m => m.Code == code))
        {
            var n = 2;
            while (await _db.ConfiguratorModels.AnyAsync(m => m.Code == $"{dto.Code}-{n}")) n++;
            code = $"{dto.Code}-{n}";
        }

        var model = new ConfiguratorModel
        {
            Code = code,
            Name = dto.Name,
            Description = dto.Description,
            CurrentRevision = revisions.Max(r => r.Revision),
        };
        foreach (var r in revisions)
        {
            model.Revisions.Add(new ConfiguratorModelRevision
            {
                Revision = r.Revision,
                Notes = r.Notes,
                DefinitionJson = r.Definition.GetRawText(),
            });
        }

        _db.ConfiguratorModels.Add(model);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = model.Id },
            new ConfiguratorModelSummaryDto(model.Id, model.Code, model.Name, model.Description,
                model.IsActive, model.CurrentRevision, revisions.Count,
                model.CreatedAt, model.UpdatedAt, model.UpdatedBy));
    }

    // ───── Helpers ─────

    /// <summary>Returns an error message if the JSON is not a valid platform definition, else null.</summary>
    private static string? ValidateDefinition(string json)
    {
        try
        {
            var platform = JsonSerializer.Deserialize<ProductPlatform>(json, JsonOpts);
            if (platform is null) return "Definition is empty.";
            if (string.IsNullOrWhiteSpace(platform.Platform.Code)) return "Definition has no platform code.";
            // Zero groups is valid — a blank-slate model the author is still building.
            return null;
        }
        catch (JsonException ex)
        {
            return $"Definition is not valid JSON: {ex.Message}";
        }
    }

    /// <summary>Seeds the built-in Sequencer RX-6 example model if no models exist yet.</summary>
    public static async Task SeedAsync(ProcessManagerDbContext db)
    {
        if (await db.ConfiguratorModels.AnyAsync()) return;

        var platform = RobotPlatformData.Build();
        var model = new ConfiguratorModel
        {
            Code = platform.Platform.Code,
            Name = platform.Platform.Name,
            Description = platform.Platform.Tagline,
            CurrentRevision = 1,
        };
        model.Revisions.Add(new ConfiguratorModelRevision
        {
            Revision = 1,
            Notes = "Initial release from configurator",
            DefinitionJson = JsonSerializer.Serialize(platform, JsonOpts),
        });
        db.ConfiguratorModels.Add(model);
        await db.SaveChangesAsync();
    }
}
