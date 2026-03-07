namespace ProcessManager.Api.DTOs;

/// <summary>
/// A single out-of-range prompt response for the alerts feed.
/// </summary>
public record OutOfRangeAlertDto(
    Guid   PromptResponseId,
    Guid   StepExecutionId,
    string JobCode,
    string JobName,
    string ProcessName,
    string StepName,
    string PromptLabel,
    string Value,
    string? OverrideNote,
    string? RespondedBy,
    DateTime RespondedAt
);

/// <summary>Count-only projection used by the NavMenu badge.</summary>
public record AlertCountDto(int Count);
