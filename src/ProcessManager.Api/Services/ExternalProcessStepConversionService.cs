using System.Diagnostics;
using ProcessManager.Domain.Services;

namespace ProcessManager.Api.Services;

/// <summary>
/// Default <see cref="IStepConversionService"/> that delegates CAD→glTF
/// tessellation to an external converter process (e.g. a containerised
/// OpenCascade / CAD Exchanger CLI). The command is configured via
/// <c>ModelConversion:ConverterCommand</c> with <c>{input}</c> / <c>{output}</c>
/// placeholders, e.g.:
///   "cad2gltf --input {input} --output {output}"
///
/// When no converter is configured the service returns a clear, non-throwing
/// failure so callers can fall back (the raw upload is retained and the browser
/// can attempt client-side tessellation via occt-import-js).
/// </summary>
public class ExternalProcessStepConversionService : IStepConversionService
{
    private readonly IImageStorageService _storage;
    private readonly IConfiguration _config;
    private readonly ILogger<ExternalProcessStepConversionService> _logger;

    public ExternalProcessStepConversionService(
        IImageStorageService storage,
        IConfiguration config,
        ILogger<ExternalProcessStepConversionService> logger)
    {
        _storage = storage;
        _config = config;
        _logger = logger;
    }

    public async Task<StepConversionResult> ConvertToGlbAsync(string sourceStorageKey, CancellationToken ct = default)
    {
        var commandTemplate = _config["ModelConversion:ConverterCommand"];
        if (string.IsNullOrWhiteSpace(commandTemplate))
            return StepConversionResult.Fail("No CAD converter configured (ModelConversion:ConverterCommand).");

        var source = await _storage.GetStreamAsync(sourceStorageKey);
        if (source is null)
            return StepConversionResult.Fail($"Source model '{sourceStorageKey}' not found in storage.");

        var tempDir = Path.Combine(Path.GetTempPath(), "pm-cad-convert", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, Path.GetFileName(sourceStorageKey));
        var outputPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(sourceStorageKey) + ModelFormatPolicy.ConvertedExtension);

        try
        {
            await using (var fs = File.Create(inputPath))
                await source.CopyToAsync(fs, ct);
            await source.DisposeAsync();

            var command = commandTemplate
                .Replace("{input}", inputPath)
                .Replace("{output}", outputPath);

            var exit = await RunProcessAsync(command, ct);
            if (exit != 0 || !File.Exists(outputPath))
                return StepConversionResult.Fail($"Converter exited {exit} or produced no output.");

            // Store the converted glb alongside the source.
            var folder = sourceStorageKey.Contains('/')
                ? sourceStorageKey[..sourceStorageKey.LastIndexOf('/')]
                : string.Empty;
            await using var glb = File.OpenRead(outputPath);
            var convertedKey = $"{folder}/{Path.GetFileName(outputPath)}".TrimStart('/');
            await _storage.SaveStreamAsync(glb, convertedKey, ModelFormatPolicy.ConvertedMimeType);

            return StepConversionResult.Ok(convertedKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CAD conversion failed for {Key}", sourceStorageKey);
            return StepConversionResult.Fail($"Conversion error: {ex.Message}");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    private static async Task<int> RunProcessAsync(string command, CancellationToken ct)
    {
        var isWindows = OperatingSystem.IsWindows();
        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "/bin/sh",
            Arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi);
        if (proc is null) return -1;
        await proc.WaitForExitAsync(ct);
        return proc.ExitCode;
    }
}
