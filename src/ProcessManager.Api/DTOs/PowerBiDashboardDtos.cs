using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

public record PowerBiDashboardCreateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required, StringLength(2000, MinimumLength = 1)] string EmbedUrl,
    [StringLength(1000)] string? Description,
    int SortOrder
);

public record PowerBiDashboardUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required, StringLength(2000, MinimumLength = 1)] string EmbedUrl,
    [StringLength(1000)] string? Description,
    int SortOrder
);

public record PowerBiDashboardResponseDto(
    Guid Id,
    string Name,
    string EmbedUrl,
    string? Description,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? CreatedBy,
    string? UpdatedBy
);
