namespace ProcessManager.Domain.Entities;

/// <summary>
/// A configurable product model for the BoM configurator (e.g. "Sequencer RX-6").
/// The model itself is just an identity + head pointer; the actual product
/// definition lives in immutable <see cref="ConfiguratorModelRevision"/> rows so
/// every change is revision-controlled (ISO 10007 configuration management).
/// </summary>
public class ConfiguratorModel : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>The highest revision number saved for this model.</summary>
    public int CurrentRevision { get; set; }

    public List<ConfiguratorModelRevision> Revisions { get; set; } = new();
}

/// <summary>
/// One immutable revision of a configurator model. <see cref="DefinitionJson"/>
/// is the full serialized <c>ProductPlatform</c> (groups, options, BoM,
/// attributes, calculated attributes, rules, compliance block). Revisions are
/// never edited in place — saving changes always appends a new revision.
/// </summary>
public class ConfiguratorModelRevision : BaseEntity
{
    public Guid ConfiguratorModelId { get; set; }
    public ConfiguratorModel? Model { get; set; }

    /// <summary>1-based, monotonically increasing per model.</summary>
    public int Revision { get; set; }

    /// <summary>Author's note describing what changed in this revision.</summary>
    public string? Notes { get; set; }

    /// <summary>Serialized <c>ProcessManager.Domain.Configurator.ProductPlatform</c>.</summary>
    public string DefinitionJson { get; set; } = string.Empty;
}
