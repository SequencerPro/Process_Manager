namespace ProcessManager.Api.Services;

public interface IImageStorageService
{
    /// <summary>
    /// Saves an uploaded file and returns the stored file name and its relative URL path.
    /// </summary>
    Task<(string fileName, string relativePath)> SaveAsync(IFormFile file, string subfolder);

    /// <summary>
    /// Deletes a previously saved file by its relative path (e.g., "uploads/steptemplates/abc.jpg").
    /// </summary>
    Task DeleteAsync(string relativePath);
}
