namespace ProcessManager.Domain.Enums;

public enum CompetencyStatus
{
    /// <summary>Training completed and not yet expired.</summary>
    Current,

    /// <summary>Past the expiry date — re-training required.</summary>
    Expired,

    /// <summary>Superseded by a newer record for the same training process.</summary>
    Superseded
}
