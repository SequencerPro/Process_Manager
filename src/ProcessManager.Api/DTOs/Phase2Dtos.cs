using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ──────────────────── StepTemplate ────────────────────

public record StepTemplateCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    StepPattern Pattern,
    [Required, MinLength(1)] List<PortCreateDto> Ports
);

public record StepTemplateUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    StepPattern Pattern,
    bool? IsActive = null
);

public record StepTemplateResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    StepPattern Pattern,
    int Version,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<PortResponseDto> Ports
);

// ──────────────────── Port ────────────────────

public record PortCreateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    PortDirection Direction,
    Guid KindId,
    Guid GradeId,
    QuantityRuleMode QtyRuleMode,
    int? QtyRuleN,
    int? QtyRuleMin,
    int? QtyRuleMax,
    [Range(0, int.MaxValue)] int SortOrder
);

public record PortUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    Guid KindId,
    Guid GradeId,
    QuantityRuleMode QtyRuleMode,
    int? QtyRuleN,
    int? QtyRuleMin,
    int? QtyRuleMax,
    [Range(0, int.MaxValue)] int SortOrder
);

public record PortResponseDto(
    Guid Id,
    Guid StepTemplateId,
    string Name,
    PortDirection Direction,
    Guid KindId,
    string KindCode,
    string KindName,
    Guid GradeId,
    string GradeCode,
    string GradeName,
    QuantityRuleMode QtyRuleMode,
    int? QtyRuleN,
    int? QtyRuleMin,
    int? QtyRuleMax,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
