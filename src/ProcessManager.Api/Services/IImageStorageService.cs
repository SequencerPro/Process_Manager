namespace ProcessManager.Api.Services;

public interface IImageStorageService
{
    /// <summary>
    /// Saves an uploaded file and returns the stored file name and its storage key (subfolder/fileName).
    /// </summary>
    Task<(string fileName, string storageKey)> SaveAsync(IFormFile file, string subfolder);

    /// <summary>
    /// Deletes a previously saved file by its storage key (e.g., "kind-models/abc.stp").
    /// </summary>
    Task DeleteAsync(string storageKey);

    /// <summary>
    /// Returns a readable stream for the file, or null if not found.
    /// Caller is responsible for disposing the stream.
    /// </summary>
    Task<Stream?> GetStreamAsync(string storageKey);

    /// <summary>
    /// Saves a raw stream under an explicit storage key (subfolder/fileName).
    /// Used for server-generated artefacts such as converted glTF models where
    /// the caller — not the uploader — controls the file name.
    /// </summary>
    Task SaveStreamAsync(Stream content, string storageKey, string contentType);
}
