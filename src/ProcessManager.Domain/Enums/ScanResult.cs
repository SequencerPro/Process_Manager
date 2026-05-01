namespace ProcessManager.Domain.Enums;

public enum ScanResult
{
    Transferred,
    AlreadyAtLocation,
    UnknownBarcode,
    InvalidItemStatus,
    WorkstationInactive,
    Error
}
