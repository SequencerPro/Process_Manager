using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CeMatricesController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    public CeMatricesController(ProcessManagerDbContext db) => _db = db;

    // ─── CeMatrix CRUD ────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CeMatrixSummaryDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] Guid? processStepId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.CeMatrices
            .Include(m => m.ProcessStep).ThenInclude(s => s.StepTemplate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Name.Contains(search));

        if (processStepId.HasValue)
            query = query.Where(m => m.ProcessStepId == processStepId.Value);

        var totalCount = await query.CountAsync();

        var matrices = await query
            .OrderBy(m => m.ProcessStep.Sequence).ThenBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new CeMatrixSummaryDto(
                m.Id, m.Name, m.Description,
                m.ProcessStepId,
                m.ProcessStep.NameOverride ?? m.ProcessStep.StepTemplate.Name,
                m.ProcessStep.Sequence,
                m.Inputs.Count, m.Outputs.Count,
                m.CreatedAt, m.UpdatedAt))
            .ToListAsync();

        return new PaginatedResponse<CeMatrixSummaryDto>(matrices, totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CeMatrixResponseDto>> GetById(Guid id)
    {
        var matrix = await LoadMatrix(id);
        if (matrix is null) return NotFound();
        return MapToDto(matrix);
    }

    [HttpPost]
    public async Task<ActionResult<CeMatrixResponseDto>> Create(CeMatrixCreateDto dto)
    {
        var step = await _db.ProcessSteps
            .Include(s => s.StepTemplate)
                .ThenInclude(t => t.Ports)
            .FirstOrDefaultAsync(s => s.Id == dto.ProcessStepId);

        if (step is null) return BadRequest("ProcessStep not found.");

        var matrix = new CeMatrix
        {
            ProcessStepId = dto.ProcessStepId,
            Name          = dto.Name,
            Description   = dto.Description,
        };
        _db.CeMatrices.Add(matrix);
        await _db.SaveChangesAsync();

        // Auto-populate inputs from step's input ports
        var inputPorts = step.StepTemplate.Ports
            .Where(p => p.Direction == PortDirection.Input)
            .OrderBy(p => p.Name)
            .ToList();
        for (var i = 0; i < inputPorts.Count; i++)
        {
            _db.CeInputs.Add(new CeInput
            {
                CeMatrixId = matrix.Id,
                Name       = inputPorts[i].Name,
                Category   = CeInputCategory.PortInput,
                PortId     = inputPorts[i].Id,
                SortOrder  = i + 1,
            });
        }

        // Auto-populate outputs from step's output ports
        var outputPorts = step.StepTemplate.Ports
            .Where(p => p.Direction == PortDirection.Output)
            .OrderBy(p => p.Name)
            .ToList();
        for (var i = 0; i < outputPorts.Count; i++)
        {
            _db.CeOutputs.Add(new CeOutput
            {
                CeMatrixId = matrix.Id,
                Name       = outputPorts[i].Name,
                Category   = CeOutputCategory.PortOutput,
                PortId     = outputPorts[i].Id,
                Importance = 5,
                SortOrder  = i + 1,
            });
        }

        await _db.SaveChangesAsync();

        var result = await LoadMatrix(matrix.Id);
        return CreatedAtAction(nameof(GetById), new { id = matrix.Id }, MapToDto(result!));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CeMatrixResponseDto>> Update(Guid id, CeMatrixUpdateDto dto)
    {
        var matrix = await _db.CeMatrices.FindAsync(id);
        if (matrix is null) return NotFound();

        matrix.Name        = dto.Name;
        matrix.Description = dto.Description;
        await _db.SaveChangesAsync();

        var result = await LoadMatrix(id);
        return Ok(MapToDto(result!));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var matrix = await _db.CeMatrices.FindAsync(id);
        if (matrix is null) return NotFound();
        _db.CeMatrices.Remove(matrix);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Inputs ───────────────────────────────────────────────────────────

    [HttpPost("{matrixId:guid}/inputs")]
    public async Task<ActionResult<CeMatrixResponseDto>> AddInput(
        Guid matrixId, CeInputCreateDto dto)
    {
        var matrix = await _db.CeMatrices.FindAsync(matrixId);
        if (matrix is null) return NotFound();

        _db.CeInputs.Add(new CeInput
        {
            CeMatrixId = matrixId,
            Name       = dto.Name,
            Category   = dto.Category,
            PortId     = dto.PortId,
            SortOrder  = dto.SortOrder,
        });
        await _db.SaveChangesAsync();

        var result = await LoadMatrix(matrixId);
        return Ok(MapToDto(result!));
    }

    [HttpPut("{matrixId:guid}/inputs/{inputId:guid}")]
    public async Task<ActionResult<CeMatrixResponseDto>> UpdateInput(
        Guid matrixId, Guid inputId, CeInputUpdateDto dto)
    {
        var input = await _db.CeInputs
            .FirstOrDefaultAsync(i => i.Id == inputId && i.CeMatrixId == matrixId);
        if (input is null) return NotFound();

        input.Name      = dto.Name;
        input.Category  = dto.Category;
        input.SortOrder = dto.SortOrder;
        await _db.SaveChangesAsync();

        var result = await LoadMatrix(matrixId);
        return Ok(MapToDto(result!));
    }

    [HttpDelete("{matrixId:guid}/inputs/{inputId:guid}")]
    public async Task<ActionResult<CeMatrixResponseDto>> DeleteInput(Guid matrixId, Guid inputId)
    {
        var input = await _db.CeInputs
            .FirstOrDefaultAsync(i => i.Id == inputId && i.CeMatrixId == matrixId);
        if (input is null) return NotFound();
        _db.CeInputs.Remove(input);
        await _db.SaveChangesAsync();

        var result = await LoadMatrix(matrixId);
        return Ok(MapToDto(result!));
    }

    // ─── Outputs ──────────────────────────────────────────────────────────

    [HttpPost("{matrixId:guid}/outputs")]
    public async Task<ActionResult<CeMatrixResponseDto>> AddOutput(
        Guid matrixId, CeOutputCreateDto dto)
    {
        var matrix = await _db.CeMatrices.FindAsync(matrixId);
        if (matrix is null) return NotFound();

        _db.CeOutputs.Add(new CeOutput
        {
            CeMatrixId = matrixId,
            Name       = dto.Name,
            Category   = dto.Category,
            PortId     = dto.PortId,
            Importance = dto.Importance,
            SortOrder  = dto.SortOrder,
        });
        await _db.SaveChangesAsync();

        var result = await LoadMatrix(matrixId);
        return Ok(MapToDto(result!));
    }

    [HttpPut("{matrixId:guid}/outputs/{outputId:guid}")]
    public async Task<ActionResult<CeMatrixResponseDto>> UpdateOutput(
        Guid matrixId, Guid outputId, CeOutputUpdateDto dto)
    {
        var output = await _db.CeOutputs
            .FirstOrDefaultAsync(o => o.Id == outputId && o.CeMatrixId == matrixId);
        if (output is null) return NotFound();

        output.Name       = dto.Name;
        output.Category   = dto.Category;
        output.Importance = dto.Importance;
        output.SortOrder  = dto.SortOrder;
        await _db.SaveChangesAsync();

        var result = await LoadMatrix(matrixId);
        return Ok(MapToDto(result!));
    }

    [HttpDelete("{matrixId:guid}/outputs/{outputId:guid}")]
    public async Task<ActionResult<CeMatrixResponseDto>> DeleteOutput(Guid matrixId, Guid outputId)
    {
        var output = await _db.CeOutputs
            .FirstOrDefaultAsync(o => o.Id == outputId && o.CeMatrixId == matrixId);
        if (output is null) return NotFound();
        _db.CeOutputs.Remove(output);
        await _db.SaveChangesAsync();

        var result = await LoadMatrix(matrixId);
        return Ok(MapToDto(result!));
    }

    // ─── Correlations ─────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a correlation score for an input/output pair.
    /// Creates a new CeCorrelation if none exists; updates the score if it does.
    /// </summary>
    [HttpPut("{matrixId:guid}/correlations")]
    public async Task<ActionResult<CeMatrixResponseDto>> UpsertCorrelation(
        Guid matrixId, CeCorrelationUpsertDto dto)
    {
        // Validate score
        int[] validScores = [0, 1, 3, 9];
        if (!validScores.Contains(dto.Score))
            return BadRequest("Score must be 0, 1, 3, or 9.");

        // Validate input and output belong to this matrix
        var inputExists  = await _db.CeInputs.AnyAsync(i => i.Id == dto.CeInputId  && i.CeMatrixId == matrixId);
        var outputExists = await _db.CeOutputs.AnyAsync(o => o.Id == dto.CeOutputId && o.CeMatrixId == matrixId);
        if (!inputExists || !outputExists)
            return BadRequest("CeInput and CeOutput must both belong to this matrix.");

        var existing = await _db.CeCorrelations
            .FirstOrDefaultAsync(c => c.CeInputId == dto.CeInputId && c.CeOutputId == dto.CeOutputId);

        if (existing is not null)
        {
            existing.Score = dto.Score;
        }
        else
        {
            _db.CeCorrelations.Add(new CeCorrelation
            {
                CeInputId  = dto.CeInputId,
                CeOutputId = dto.CeOutputId,
                Score      = dto.Score,
            });
        }
        await _db.SaveChangesAsync();

        var result = await LoadMatrix(matrixId);
        return Ok(MapToDto(result!));
    }

    // ─── CSV export ───────────────────────────────────────────────────────

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id)
    {
        var matrix = await LoadMatrix(id);
        if (matrix is null) return NotFound();

        var dto = MapToDto(matrix);
        var csv = BuildCsv(dto);
        return File(System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"ce-matrix-{matrix.Name.Replace(" ", "_")}.csv");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private async Task<CeMatrix?> LoadMatrix(Guid id) =>
        await _db.CeMatrices
            .Include(m => m.ProcessStep).ThenInclude(s => s.StepTemplate)
            .Include(m => m.Inputs.OrderBy(i => i.SortOrder)).ThenInclude(i => i.Port)
            .Include(m => m.Outputs.OrderBy(o => o.SortOrder)).ThenInclude(o => o.Port)
            .Include(m => m.Correlations)
            .FirstOrDefaultAsync(m => m.Id == id);

    private static CeMatrixResponseDto MapToDto(CeMatrix m)
    {
        // Compute priority scores for each input
        var corrByInput = m.Correlations
            .GroupBy(c => c.CeInputId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var outputImportance = m.Outputs.ToDictionary(o => o.Id, o => o.Importance);

        var inputs = m.Inputs.OrderBy(i => i.SortOrder).Select(i =>
        {
            var corrs = corrByInput.GetValueOrDefault(i.Id, []);
            var score = corrs.Sum(c => c.Score * outputImportance.GetValueOrDefault(c.CeOutputId, 0));
            return new CeInputResponseDto(
                i.Id, i.CeMatrixId, i.Name, i.Category.ToString(),
                i.PortId, i.Port?.Name,
                i.SortOrder, score,
                i.CreatedAt, i.UpdatedAt);
        }).ToList();

        var outputs = m.Outputs.OrderBy(o => o.SortOrder).Select(o => new CeOutputResponseDto(
            o.Id, o.CeMatrixId, o.Name, o.Category.ToString(),
            o.PortId, o.Port?.Name,
            o.Importance, o.SortOrder,
            o.CreatedAt, o.UpdatedAt)).ToList();

        var correlations = m.Correlations.Select(c => new CeCorrelationResponseDto(
            c.Id, c.CeInputId, c.CeOutputId, c.Score)).ToList();

        return new CeMatrixResponseDto(
            m.Id, m.Name, m.Description,
            m.ProcessStepId,
            m.ProcessStep?.NameOverride ?? m.ProcessStep?.StepTemplate?.Name ?? "Unknown",
            m.ProcessStep?.Sequence ?? 0,
            m.CreatedAt, m.UpdatedAt,
            inputs, outputs, correlations);
    }

    private static string BuildCsv(CeMatrixResponseDto m)
    {
        var sb = new System.Text.StringBuilder();

        // Header row: "Input" + one column per output
        sb.Append("Input,Category,Priority Score");
        foreach (var o in m.Outputs)
            sb.Append($",{o.Name} (Importance={o.Importance})");
        sb.AppendLine();

        // Data rows
        var corrLookup = m.Correlations.ToDictionary(c => (c.CeInputId, c.CeOutputId), c => c.Score);
        foreach (var i in m.Inputs.OrderByDescending(x => x.PriorityScore))
        {
            sb.Append($"{i.Name},{i.Category},{i.PriorityScore}");
            foreach (var o in m.Outputs)
            {
                var score = corrLookup.GetValueOrDefault((i.Id, o.Id), 0);
                sb.Append($",{score}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
