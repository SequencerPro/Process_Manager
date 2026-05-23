namespace ProcessManager.Api.Services;

/// <summary>
/// Result of a server-side CAD→glTF conversion.
/// </summary>
public sealed record StepConversionResult(bool Success, string? ConvertedStorageKey, string? Error)
{
    public static StepConversionResult Ok(string convertedStorageKey) => new(true, convertedStorageKey, null);
    public static StepConversionResult Fail(string error) => new(false, null, error);
}

/// <summary>
/// Converts an uploaded B-rep CAD file (STEP/IGES) into a web-ready glTF binary
/// (.glb) so heavy assemblies tessellate once on the server instead of in every
/// operator's browser (Phase 37).
///
/// The conversion <i>engine</i> is environment-specific (OpenCascade is native),
/// so it sits behind this interface. The orchestration around it — status
/// transitions, storage, fallback — lives in the controller and is unit/integration
/// tested with a fake implementation. The production default shells out to an
/// external converter configured via <c>ModelConversion:ConverterCommand</c>.
/// </summary>
public interface IStepConversionService
{
    /// <summary>
    /// Convert the file stored at <paramref name="sourceStorageKey"/> to a .glb,
    /// store it, and return its storage key. Implementations must not throw for
    /// expected failures — return <see cref="StepConversionResult.Fail"/> instead.
    /// </summary>
    Task<StepConversionResult> ConvertToGlbAsync(string sourceStorageKey, CancellationToken ct = default);
}
