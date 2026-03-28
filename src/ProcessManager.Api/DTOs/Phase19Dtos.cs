using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ��─────────────────── Phase 19 — Warehouse Management ────────────────────

// ── Storage Location DTOs ────────────────────────────────────────────────

public record CreateStorageLocationDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(50)] string? Zone = null,
    [StringLength(50)] string? Aisle = null,
    [StringLength(50)] string? Bay = null,
    [StringLength(50)] string? Bin = null,
    Guid? ParentId = null,
    [StringLength(2000)] string? Description = null
);

public record UpdateStorageLocationDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(50)] string? Zone = null,
    [StringLength(50)] string? Aisle = null,
    [StringLength(50)] string? Bay = null,
    [StringLength(50)] string? Bin = null,
    Guid? ParentId = null,
    [StringLength(2000)] string? Description = null,
    bool IsActive = true
);

public record StorageLocationResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Zone,
    string? Aisle,
    string? Bay,
    string? Bin,
    Guid? ParentId,
    string? ParentCode,
    string? Description,
    bool IsActive,
    int ItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record StorageLocationDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? Zone,
    string? Aisle,
    string? Bay,
    string? Bin,
    Guid? ParentId,
    string? ParentCode,
    string? Description,
    bool IsActive,
    int ItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<StorageLocationResponseDto> ChildLocations,
    List<OnHandSummaryDto> OnHandItems,
    List<InventoryTransactionResponseDto> RecentTransactions
);

// ── Inventory Transaction DTOs ───────────────────────────────────────────

public record CreateInventoryTransactionDto(
    [Required] string TransactionType,
    Guid ItemId,
    Guid? FromLocationId = null,
    Guid? ToLocationId = null,
    [Range(0.0001, double.MaxValue)] decimal Quantity = 1,
    [StringLength(2000)] string? Notes = null
);

public record InventoryTransactionResponseDto(
    Guid Id,
    string TransactionType,
    Guid ItemId,
    string? ItemSerialNumber,
    string KindCode,
    string KindName,
    Guid? FromLocationId,
    string? FromLocationCode,
    Guid? ToLocationId,
    string? ToLocationCode,
    decimal Quantity,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Notes,
    DateTime TransactedAt,
    string TransactedByUserId
);

// ── On-Hand DTOs ─────────────────────────────────────────────────────────

public record OnHandSummaryDto(
    Guid KindId,
    string KindCode,
    string KindName,
    string? UnitOfMeasure,
    Guid? LocationId,
    string? LocationCode,
    string? LocationName,
    int QuantityOnHand,
    decimal? ReorderThreshold,
    decimal? ReorderQuantity
);

// ── PickList DTOs ────────────────────────────────────────────────────────

public record PickListSummaryDto(
    Guid Id,
    Guid JobId,
    string JobCode,
    string Status,
    int LineCount,
    int ShortShippedCount,
    DateTime GeneratedAt
);

public record PickListResponseDto(
    Guid Id,
    Guid JobId,
    string JobCode,
    string Status,
    DateTime GeneratedAt,
    string GeneratedByUserId,
    int LineCount,
    List<PickListLineResponseDto> Lines
);

public record PickListLineResponseDto(
    Guid Id,
    Guid KindId,
    string KindCode,
    string KindName,
    string? UnitOfMeasure,
    Guid? ItemId,
    string? ItemSerialNumber,
    Guid? SourceLocationId,
    string? SourceLocationCode,
    decimal RequiredQuantity,
    decimal PickedQuantity,
    decimal ConsumedQuantity,
    string Status
);

public record PickLineDto(
    Guid ItemId,
    Guid SourceLocationId,
    [Range(0.0001, double.MaxValue)] decimal PickedQuantity
);

public record ConsumeLineDto(
    [Range(0.0001, double.MaxValue)] decimal ConsumedQuantity
);

// ── Dashboard DTOs ───────────────────────────────────────────────────────

public record WarehouseDashboardDto(
    int TotalLocations,
    int TotalItemsOnHand,
    int LowStockKindCount,
    int PendingPickLists,
    List<LowStockKindDto> LowStockKinds,
    List<InventoryTransactionResponseDto> RecentTransactions
);

public record LowStockKindDto(
    Guid KindId,
    string KindCode,
    string KindName,
    int OnHand,
    decimal ReorderThreshold,
    decimal? ReorderQuantity,
    string? UnitOfMeasure
);

// ── Job Receipt DTOs ─────────────────────────────────────────────────────

public record ReceiveItemsToWarehouseDto(
    [Required, MinLength(1)] List<ReceiveItemDto> Items
);

public record ReceiveItemDto(
    Guid ItemId,
    Guid StorageLocationId
);
