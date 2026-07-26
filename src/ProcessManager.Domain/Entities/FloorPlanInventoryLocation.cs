using ProcessManager.Domain.Services;

namespace ProcessManager.Domain.Entities;

public class FloorPlanInventoryLocation : BaseEntity
{
    public Guid FloorPlanId { get; set; }
    public string PlacementId { get; set; } = "";
    public Guid StorageLocationId { get; set; }

    // ── Per-placement CAD model (Phase 37) — mirrors FloorPlanWorkstation ──
    public string? ModelFileName { get; set; }
    public string? ModelOriginalFileName { get; set; }
    public string? ModelMimeType { get; set; }
    public string? ConvertedModelFileName { get; set; }
    public ModelConversionStatus ConversionStatus { get; set; } = ModelConversionStatus.None;
    public string? ConversionError { get; set; }

    // Model fit transform
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
    public StorageLocation StorageLocation { get; set; } = null!;

    /// <summary>
    /// Kinds this location is explicitly designated to supply (Phase 37 "designed
    /// flow" mode). Independent of live on-hand stock.
    /// </summary>
    public ICollection<FloorPlanInventoryLocationKind> DesignatedKinds { get; set; }
        = new List<FloorPlanInventoryLocationKind>();
}
