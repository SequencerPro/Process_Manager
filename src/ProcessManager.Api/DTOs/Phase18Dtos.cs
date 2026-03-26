namespace ProcessManager.Api.DTOs;

// ──────────────────── Phase 18: Step 3D Model ────────────────────

public record StepModelResponseDto(
    Guid Id,
    Guid StepTemplateId,
    string FileName,
    string OriginalFileName,
    string MimeType,
    DateTime UploadedAt,
    string? UploadedByUserId
);

/// <summary>Sets or clears the Kind whose 3D model is used for this step's 3D viewer.</summary>
public record SetKindModelRefDto(
    Guid? KindId
);
