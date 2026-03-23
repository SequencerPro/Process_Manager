using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Defines what something physically or logically is.
/// A Kind has a set of valid Grades, tracking flags, and extended properties
/// describing sourcing, cost, compliance, and physical characteristics.
/// </summary>
public class Kind : BaseEntity
{
    /// <summary>Short identifier (e.g., "WDG-100").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name (e.g., "Widget").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Detailed description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether individual items get unique IDs.</summary>
    public bool IsSerialized { get; set; }

    /// <summary>Whether items can be grouped into batches.</summary>
    public bool IsBatchable { get; set; }

    // ──────────── Extended Properties ────────────

    /// <summary>Sourcing strategy: Make, Buy, ReferenceDocument, Phantom, Consumable.</summary>
    public KindSourceType SourceType { get; set; } = KindSourceType.Make;

    /// <summary>Unit of measure (e.g. "Each", "Kg", "Meters", "Liters").</summary>
    public string? UnitOfMeasure { get; set; }

    /// <summary>Production cost per unit.</summary>
    public decimal? Cost { get; set; }

    /// <summary>Selling price per unit.</summary>
    public decimal? Price { get; set; }

    /// <summary>Primary vendor name (relevant when SourceType = Buy).</summary>
    public string? VendorName { get; set; }

    /// <summary>Vendor's own part number (relevant when SourceType = Buy).</summary>
    public string? VendorPartNumber { get; set; }

    /// <summary>Procurement or production lead time in days.</summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>Part weight (unit specified by WeightUnit).</summary>
    public decimal? Weight { get; set; }

    /// <summary>Weight unit (e.g. "g", "kg", "lb", "oz").</summary>
    public string? WeightUnit { get; set; }

    /// <summary>RoHS compliance status (e.g. "Compliant", "Non-Compliant", "Exempt", "Unknown").</summary>
    public string? RohsStatus { get; set; }

    /// <summary>Country of origin for trade compliance.</summary>
    public string? CountryOfOrigin { get; set; }

    /// <summary>Drawing or specification revision level.</summary>
    public string? Revision { get; set; }

    /// <summary>Free-form notes.</summary>
    public string? Notes { get; set; }

    // ──────────── 3D Model ────────────

    /// <summary>GUID-based filename of the 3D model stored on disk (STL, OBJ, GLB, GLTF).</summary>
    public string? ModelFileName { get; set; }

    /// <summary>Original filename of the uploaded 3D model.</summary>
    public string? ModelOriginalFileName { get; set; }

    /// <summary>MIME type of the 3D model file.</summary>
    public string? ModelMimeType { get; set; }

    // Navigation properties
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<KindDocument> Documents { get; set; } = new List<KindDocument>();
}
