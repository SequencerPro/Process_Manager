using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/powerbi-dashboards")]
public class PowerBiDashboardsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public PowerBiDashboardsController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<PowerBiDashboardResponseDto>>> GetAll()
    {
        var dashboards = await _db.PowerBiDashboards
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync();

        return dashboards.Select(MapToDto).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PowerBiDashboardResponseDto>> GetById(Guid id)
    {
        var dashboard = await _db.PowerBiDashboards.FindAsync(id);
        if (dashboard is null) return NotFound();
        return MapToDto(dashboard);
    }

    [HttpPost]
    public async Task<ActionResult<PowerBiDashboardResponseDto>> Create(PowerBiDashboardCreateDto dto)
    {
        if (await _db.PowerBiDashboards.AnyAsync(d => d.Name == dto.Name))
            return Conflict($"A dashboard with name '{dto.Name}' already exists.");

        var dashboard = new PowerBiDashboard
        {
            Name = dto.Name,
            EmbedUrl = dto.EmbedUrl,
            Description = dto.Description,
            SortOrder = dto.SortOrder
        };

        _db.PowerBiDashboards.Add(dashboard);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dashboard.Id }, MapToDto(dashboard));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PowerBiDashboardResponseDto>> Update(Guid id, PowerBiDashboardUpdateDto dto)
    {
        var dashboard = await _db.PowerBiDashboards.FindAsync(id);
        if (dashboard is null) return NotFound();

        if (await _db.PowerBiDashboards.AnyAsync(d => d.Name == dto.Name && d.Id != id))
            return Conflict($"A dashboard with name '{dto.Name}' already exists.");

        dashboard.Name = dto.Name;
        dashboard.EmbedUrl = dto.EmbedUrl;
        dashboard.Description = dto.Description;
        dashboard.SortOrder = dto.SortOrder;

        await _db.SaveChangesAsync();
        return MapToDto(dashboard);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var dashboard = await _db.PowerBiDashboards.FindAsync(id);
        if (dashboard is null) return NotFound();

        _db.PowerBiDashboards.Remove(dashboard);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static PowerBiDashboardResponseDto MapToDto(PowerBiDashboard d) => new(
        d.Id, d.Name, d.EmbedUrl, d.Description, d.SortOrder,
        d.CreatedAt, d.UpdatedAt, d.CreatedBy, d.UpdatedBy
    );
}
