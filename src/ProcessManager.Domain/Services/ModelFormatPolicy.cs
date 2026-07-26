namespace ProcessManager.Domain.Services;

/// <summary>
/// Conversion status for an uploaded workstation CAD model (Phase 37).
/// </summary>
public enum ModelConversionStatus
{
    /// <summary>No model uploaded.</summary>
    None = 0,

    /// <summary>Upload is already a web-ready mesh format; no conversion needed.</summary>
    NotRequired = 1,

    /// <summary>A CAD format (STEP/IGES) awaiting server-side tessellation to glTF.</summary>
    Pending = 2,

    /// <summary>Conversion in progress.</summary>
    Converting = 3,

    /// <summary>Conversion finished; a web-ready glTF/glb is available.</summary>
    Converted = 4,

    /// <summary>Conversion failed; the raw upload is retained for retry/download.</summary>
    Failed = 5
}

/// <summary>
/// Classification of an uploaded model file.
/// </summary>
public sealed record ModelFormatClassification(
    string Extension,
    bool IsSupported,
    bool IsWebReady,
    bool NeedsConversion)
{
    /// <summary>The conversion status a freshly-uploaded file of this format should start in.</summary>
    public ModelConversionStatus InitialStatus =>
        !IsSupported ? ModelConversionStatus.None
        : IsWebReady ? ModelConversionStatus.NotRequired
        : ModelConversionStatus.Pending;
}

/// <summary>
/// Pure-domain policy describing which 3D model formats the platform accepts,
/// which can be rendered directly in the browser, and which require server-side
/// STEP→glTF conversion before they're web-ready. Single source of truth shared
/// by upload endpoints and the conversion pipeline so the rules can't drift.
/// </summary>
public static class ModelFormatPolicy
{
    /// <summary>Mesh formats three.js can load directly — no conversion.</summary>
    private static readonly HashSet<string> WebReady =
        new(StringComparer.OrdinalIgnoreCase) { ".glb", ".gltf", ".stl", ".obj" };

    /// <summary>B-rep CAD formats requiring tessellation (OpenCascade) to a mesh.</summary>
    private static readonly HashSet<string> CadNeedingConversion =
        new(StringComparer.OrdinalIgnoreCase) { ".step", ".stp", ".iges", ".igs" };

    /// <summary>The web-ready format CAD files are converted into.</summary>
    public const string ConvertedExtension = ".glb";
    public const string ConvertedMimeType = "model/gltf-binary";

    public static IReadOnlyCollection<string> AllowedExtensions =>
        WebReady.Concat(CadNeedingConversion).OrderBy(x => x).ToList();

    public static bool IsAllowed(string? fileNameOrExt) =>
        Classify(fileNameOrExt).IsSupported;

    /// <summary>Classify a filename or bare extension (with or without leading dot).</summary>
    public static ModelFormatClassification Classify(string? fileNameOrExt)
    {
        var ext = NormalizeExtension(fileNameOrExt);
        var webReady = WebReady.Contains(ext);
        var cad = CadNeedingConversion.Contains(ext);
        return new ModelFormatClassification(
            Extension: ext,
            IsSupported: webReady || cad,
            IsWebReady: webReady,
            NeedsConversion: cad);
    }

    /// <summary>
    /// MIME type for a stored model by extension. Falls back to a generic binary
    /// type for anything unrecognised.
    /// </summary>
    public static string MimeTypeFor(string? fileNameOrExt) => NormalizeExtension(fileNameOrExt) switch
    {
        ".glb" => "model/gltf-binary",
        ".gltf" => "model/gltf+json",
        ".stl" => "model/stl",
        ".obj" => "model/obj",
        ".step" or ".stp" => "application/step",
        ".iges" or ".igs" => "application/iges",
        _ => "application/octet-stream"
    };

    private static string NormalizeExtension(string? fileNameOrExt)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrExt)) return string.Empty;
        var s = fileNameOrExt.Trim();
        var dot = s.LastIndexOf('.');
        var ext = dot >= 0 ? s[dot..] : "." + s;
        return ext.ToLowerInvariant();
    }
}
