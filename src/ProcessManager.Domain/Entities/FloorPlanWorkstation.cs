using ProcessManager.Domain.Enums;
using ProcessManager.Domain.Services;

namespace ProcessManager.Domain.Entities;

public class FloorPlanWorkstation : BaseEntity
{
    public Guid FloorPlanId { get; set; }
    public string PlacementId { get; set; } = "";
    public Guid? EquipmentId { get; set; }
    public Guid? OrgUnitId { get; set; }
    public Guid? StorageLocationId { get; set; }

    // ── Per-placement CAD model (Phase 37) ──
    /// <summary>Stored file name of the raw uploaded model (any allowed format).</summary>
    public string? ModelFileName { get; set; }
    /// <summary>Original file name as uploaded, for download.</summary>
    public string? ModelOriginalFileName { get; set; }
    /// <summary>MIME type of the raw uploaded model.</summary>
    public string? ModelMimeType { get; set; }
    /// <summary>Stored file name of the web-ready glTF produced by conversion (null until converted).</summary>
    public string? ConvertedModelFileName { get; set; }
    /// <summary>Lifecycle of the server-side CAD→glTF conversion.</summary>
    public ModelConversionStatus ConversionStatus { get; set; } = ModelConversionStatus.None;
    /// <summary>Error message from the last failed conversion, if any.</summary>
    public string? ConversionError { get; set; }

    // ── Model fit transform (so a CAD model lands on its footprint) ──
    public double ModelScale { get; set; } = 1.0;
    public double ModelYaw { get; set; } = 0.0;
    public double ModelOffsetX { get; set; } = 0.0;
    public double ModelOffsetY { get; set; } = 0.0;
    public double ModelOffsetZ { get; set; } = 0.0;

    /// <summary>True when a web-ready model (uploaded mesh or converted glTF) can be rendered.</summary>
    public bool HasRenderableModel =>
        ConversionStatus == ModelConversionStatus.NotRequired
        || ConversionStatus == ModelConversionStatus.Converted;

    // Navigation
    public FloorPlan FloorPlan { get; set; } = null!;
    public Equipment? Equipment { get; set; }
    public OrgUnit? OrgUnit { get; set; }
    public StorageLocation? StorageLocation { get; set; }
    public ICollection<FloorPlanWorkstationProcess> Processes { get; set; } = new List<FloorPlanWorkstationProcess>();
    public ICollection<FloorPlanWorkstationTool> Tools { get; set; } = new List<FloorPlanWorkstationTool>();
}
