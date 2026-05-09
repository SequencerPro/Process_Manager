using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class ComplaintResponse : BaseEntity
{
    public Guid CustomerComplaintId { get; set; }

    public ComplaintResponseType ResponseType { get; set; } = ComplaintResponseType.Acknowledgment;

    public string Content { get; set; } = string.Empty;

    public string SentByUserId { get; set; } = string.Empty;

    public string SentByDisplayName { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }
}
