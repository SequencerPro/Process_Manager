using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ProcessManager.Api.Services;

public class AzureBlobStorageService : IImageStorageService
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["Storage:AzureBlob:ConnectionString"]
            ?? throw new InvalidOperationException("Storage:AzureBlob:ConnectionString not configured.");
        var containerName = configuration["Storage:AzureBlob:ContainerName"] ?? "uploads";

        _container = new BlobContainerClient(connectionString, containerName);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<(string fileName, string storageKey)> SaveAsync(IFormFile file, string subfolder)
    {
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var storageKey = $"{subfolder}/{fileName}";

        var blob = _container.GetBlobClient(storageKey);
        using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders
        {
            ContentType = file.ContentType
        });

        return (fileName, storageKey);
    }

    public async Task SaveStreamAsync(Stream content, string storageKey, string contentType)
    {
        var blob = _container.GetBlobClient(storageKey);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
    }

    public async Task DeleteAsync(string storageKey)
    {
        var blob = _container.GetBlobClient(storageKey);
        await blob.DeleteIfExistsAsync();
    }

    public async Task<Stream?> GetStreamAsync(string storageKey)
    {
        var blob = _container.GetBlobClient(storageKey);
        if (!await blob.ExistsAsync())
            return null;

        var response = await blob.DownloadStreamingAsync();
        return response.Value.Content;
    }
}
