using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ProcessManager.Api.DTOs;

// ───── Configurator models (revision-controlled product platforms) ─────

public record ConfiguratorModelSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    int CurrentRevision,
    int RevisionCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? UpdatedBy);

public record ConfiguratorModelDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int CurrentRevision,
    List<ConfiguratorRevisionSummaryDto> Revisions);

public record ConfiguratorRevisionSummaryDto(
    Guid Id,
    int Revision,
    string? Notes,
    DateTime CreatedAt,
    string? CreatedBy);

public record ConfiguratorRevisionDto(
    Guid Id,
    int Revision,
    string? Notes,
    DateTime CreatedAt,
    string? CreatedBy,
    string DefinitionJson);

public record ConfiguratorModelCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    [StringLength(2000)] string? Notes,
    [Required] string DefinitionJson);

public record ConfiguratorRevisionCreateDto(
    [StringLength(2000)] string? Notes,
    [Required] string DefinitionJson);

// ───── Portable export / import envelope ─────
// The definition is embedded as a real JSON object (not an escaped string) so
// the exported .json file is human-readable and diff-able.

public record ConfiguratorModelExportDto(
    string Format,
    int FormatVersion,
    DateTime ExportedAt,
    string Code,
    string Name,
    string? Description,
    int CurrentRevision,
    List<ConfiguratorExportRevisionDto> Revisions)
{
    public const string FormatId = "processmanager.configurator-model";
    public const int Version = 1;
}

public record ConfiguratorExportRevisionDto(
    int Revision,
    string? Notes,
    DateTime CreatedAt,
    string? CreatedBy,
    JsonElement Definition);
