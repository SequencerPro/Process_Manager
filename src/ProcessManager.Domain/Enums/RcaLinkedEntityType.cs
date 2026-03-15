namespace ProcessManager.Domain.Enums;

/// <summary>
/// The type of entity that triggered the RCA (Ishikawa or 5 Whys).
/// </summary>
public enum RcaLinkedEntityType
{
    Manual,
    NonConformance,
    PfmeaFailureMode
}
