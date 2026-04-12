using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.DTOs;

// ── Floor Plan ──

public record FloorPlanCreateDto(string Code, string Name, string? Description);

public record FloorPlanUpdateDto(string Name, string? Description);

public record FloorPlanLayoutSaveDto(string LayoutJson);

public record FloorPlanSummaryDto(
    Guid Id, string Code, string Name, string? Description,
    int Version, FloorPlanStatus Status, bool IsActive,
    string? ThumbnailBase64,
    int WorkstationCount, int InventoryLocationCount,
    DateTime CreatedAt, DateTime UpdatedAt, string? CreatedBy);

public record FloorPlanDetailDto(
    Guid Id, string Code, string Name, string? Description,
    int Version, FloorPlanStatus Status, bool IsActive,
    string LayoutJson, string? ThumbnailBase64,
    List<FloorPlanWorkstationDto> Workstations,
    List<FloorPlanInventoryLocationDto> InventoryLocations,
    DateTime CreatedAt, DateTime UpdatedAt, string? CreatedBy);

// ── Workstation ──

public record FloorPlanWorkstationCreateDto(
    string PlacementId, Guid? EquipmentId, Guid? OrgUnitId, Guid? StorageLocationId);

public record FloorPlanWorkstationUpdateDto(
    Guid? EquipmentId, Guid? OrgUnitId, Guid? StorageLocationId);

public record FloorPlanWorkstationDto(
    Guid Id, string PlacementId,
    Guid? EquipmentId, string? EquipmentCode, string? EquipmentName,
    Guid? OrgUnitId, string? OrgUnitCode, string? OrgUnitName,
    Guid? StorageLocationId, string? StorageLocationCode,
    List<FloorPlanWorkstationProcessDto> Processes,
    List<FloorPlanWorkstationToolDto> Tools);

// ── Workstation Process ──

public record FloorPlanWorkstationProcessCreateDto(Guid ProcessId, int SortOrder = 0);

public record FloorPlanWorkstationProcessDto(
    Guid Id, Guid ProcessId, string ProcessCode, string ProcessName, int SortOrder);

// ── Workstation Tool ──

public record FloorPlanWorkstationToolCreateDto(Guid KindId, int Quantity = 1, string? Notes = null);

public record FloorPlanWorkstationToolUpdateDto(int Quantity, string? Notes);

public record FloorPlanWorkstationToolDto(
    Guid Id, Guid KindId, string KindCode, string KindName, int Quantity, string? Notes);

// ── Inventory Location ──

public record FloorPlanInventoryLocationCreateDto(string PlacementId, Guid StorageLocationId);

public record FloorPlanInventoryLocationDto(
    Guid Id, string PlacementId,
    Guid StorageLocationId, string StorageLocationCode, string? StorageLocationName);

// ── Material Flow Analysis ──

public record MaterialFlowRequestDto(bool IncludeEmptyLocations = false);

public record MaterialFlowResultDto(
    List<MaterialFlowLineDto> Flows,
    List<UnresolvedMaterialDto> Unresolved);

public record MaterialFlowLineDto(
    string WorkstationPlacementId, string WorkstationLabel,
    Guid KindId, string KindCode, string KindName,
    string SourceLocationPlacementId, string SourceLocationLabel, string SourceLocationCode,
    int OnHandQuantity, double DistanceMm, double DistanceM,
    PointDto FromPoint, PointDto ToPoint);

public record UnresolvedMaterialDto(
    string WorkstationPlacementId, Guid KindId, string KindCode, string KindName, string Reason);

public record PointDto(double X, double Y);
