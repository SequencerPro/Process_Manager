namespace ProcessManager.Domain.Entities;

/// <summary>
/// An image attached to a StepTemplate (e.g., work instruction photos).
/// </summary>
public class StepTemplateImage : BaseEntity
{
    public Guid StepTemplateId { get; set; }
    public StepTemplate StepTemplate { get; set; } = null!;

    /// <summary>GUID-based file name on disk (e.g., "a1b2c3d4.jpg").</summary>
    public string FileName { get; set; } = "";

    /// <summary>Original name provided by the uploader.</summary>
    public string OriginalFileName { get; set; } = "";

    /// <summary>MIME type (e.g., "image/jpeg").</summary>
    public string MimeType { get; set; } = "";

    /// <summary>Display order.</summary>
    public int SortOrder { get; set; }
}
