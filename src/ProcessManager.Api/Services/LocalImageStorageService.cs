namespace ProcessManager.Api.Services;

public class LocalImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalImageStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<(string fileName, string relativePath)> SaveAsync(IFormFile file, string subfolder)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", subfolder);
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);

        return (fileName, $"uploads/{subfolder}/{fileName}");
    }

    public Task DeleteAsync(string relativePath)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, relativePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
