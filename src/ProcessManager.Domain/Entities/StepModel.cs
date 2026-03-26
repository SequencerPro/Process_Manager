namespace ProcessManager.Domain.Entities;

/// <summary>
/// A 3D model file attached to a StepTemplate (one-to-one).
/// </summary>
public class StepModel : BaseEntity
{
    public Guid StepTemplateId { get; set; }

    /// <summary>GUID-based filename stored on disk (e.g. STL, OBJ, GLB, GLTF).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Original filename as uploaded by the user.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>MIME type of the model file.</summary>
    public string MimeType { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    public string? UploadedByUserId { get; set; }

    // Navigation
    public StepTemplate StepTemplate { get; set; } = null!;
}
