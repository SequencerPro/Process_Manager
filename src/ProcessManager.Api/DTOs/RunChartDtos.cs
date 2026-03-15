using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ──────────────────── RunChartWidget ────────────────────

public record RunChartWidgetResponseDto(
    Guid Id,
    Guid StepTemplateId,
    Guid SourceContentId,
    // Denormalised source info (from the source StepTemplateContent + its StepTemplate)
    Guid SourceStepTemplateId,
    string SourceStepTemplateCode,
    string SourceStepTemplateName,
    string? SourcePromptLabel,
    string? SourceUnits,
    decimal? SourceSpecMin,
    decimal? SourceSpecMax,
    // Widget configuration
    string Label,
    int ChartWindowSize,
    decimal? SpecMin,
    decimal? SpecMax,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RunChartWidgetCreateDto(
    Guid SourceContentId,
    [Required, StringLength(300, MinimumLength = 1)] string Label,
    [Range(5, 500)] int ChartWindowSize = 30,
    decimal? SpecMin = null,
    decimal? SpecMax = null,
    int DisplayOrder = 0
);

public record RunChartWidgetUpdateDto(
    [Required, StringLength(300, MinimumLength = 1)] string Label,
    [Range(5, 500)] int ChartWindowSize,
    decimal? SpecMin,
    decimal? SpecMax,
    int DisplayOrder
);

// ──────────────────── Prompt History ────────────────────

/// <summary>
/// A single data point returned by the prompt history endpoint.
/// </summary>
public record PromptHistoryPointDto(
    DateTime Timestamp,
    double Value,
    bool IsOutOfRange
);
