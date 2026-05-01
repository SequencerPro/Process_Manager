using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── Workstation ─────────────────────────────────────────────────────────────

public record WorkstationResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid FixedLocationId,
    string FixedLocationCode,
    string FixedLocationName,
    bool IsActive,
    int ApiKeyCount,
    DateTime? LastScanAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record WorkstationSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string FixedLocationCode,
    bool IsActive,
    int ApiKeyCount);

public class CreateWorkstationDto
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public Guid FixedLocationId { get; set; }
}

public class UpdateWorkstationDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public Guid FixedLocationId { get; set; }

    public bool IsActive { get; set; } = true;
}

// ── API Key ─────────────────────────────────────────────────────────────────

public record ApiKeyResponseDto(
    Guid Id,
    string KeyPrefix,
    string Name,
    Guid WorkstationId,
    string WorkstationCode,
    bool IsActive,
    DateTime? LastUsedAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt);

public record ApiKeyCreatedDto(
    Guid Id,
    string RawKey,
    string KeyPrefix,
    string Name,
    Guid WorkstationId,
    string WorkstationCode);

public class CreateApiKeyDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid WorkstationId { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class UpdateApiKeyDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? ExpiresAt { get; set; }
}

// ── Scan ────────────────────────────────────────────────────────────────────

public class ScanRequestDto
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Barcode { get; set; } = string.Empty;
}

public record ScanResponseDto(
    string Result,
    Guid? TransactionId,
    ScanItemDto? Item,
    ScanLocationDto? FromLocation,
    ScanLocationDto? ToLocation,
    ScanWorkstationDto? Workstation,
    DateTime TransactedAt,
    string? Error);

public record ScanItemDto(Guid Id, string? Barcode, string? SerialNumber, string KindCode, string KindName);
public record ScanLocationDto(Guid Id, string Code);
public record ScanWorkstationDto(Guid Id, string Code);

// ── Scan Event ──────────────────────────────────────────────────────────────

public record ScanEventResponseDto(
    Guid Id,
    Guid WorkstationId,
    string WorkstationCode,
    string ScannedBarcode,
    Guid? ItemId,
    string? ItemSerialNumber,
    Guid? TransactionId,
    string Result,
    string? ErrorMessage,
    DateTime ScannedAt);
