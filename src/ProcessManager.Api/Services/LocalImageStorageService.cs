namespace ProcessManager.Api.Services;

public class LocalImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalImageStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<(string fileName, string storageKey)> SaveAsync(IFormFile file, string subfolder)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", subfolder);
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);

        return (fileName, $"{subfolder}/{fileName}");
    }

    public async Task SaveStreamAsync(Stream content, string storageKey, string contentType)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, "uploads", storageKey.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs);
    }

    public Task DeleteAsync(string storageKey)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<Stream?> GetStreamAsync(string storageKey)
    {
        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    private string ResolvePath(string storageKey)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        // storageKey is "subfolder/fileName", files live under wwwroot/uploads/
        return Path.Combine(webRoot, "uploads", storageKey.Replace('/', Path.DirectorySeparatorChar));
    }
}
