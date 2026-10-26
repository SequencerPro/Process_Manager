using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Deleting a StepTemplate must also remove the files behind its images and
/// image content blocks, the same as DeleteImage and DeleteContentBlock do.
/// </summary>
public class StepTemplateDeleteImageTests : IntegrationTestBase, IClassFixture<FakeImageStorageFactory>
{
    private readonly FakeImageStorage _storage;

    public StepTemplateDeleteImageTests(FakeImageStorageFactory factory) : base(factory)
    {
        _storage = factory.Storage;
    }

    [Fact]
    public async Task Delete_TemplateWithImages_DeletesStoredFiles()
    {
        var (kind, grade) = await CreateKindWithGrade("STDI-001", "Delete Image Kind");
        var step = await CreateTransformStep("STDI-XF-01", "Delete Image Step",
            kind.Id, grade.Id, kind.Id, grade.Id);

        var image = await UploadAsync<StepTemplateImageResponseDto>($"/api/steptemplates/{step.Id}/images");
        var block = await UploadAsync<StepTemplateContentResponseDto>($"/api/steptemplates/{step.Id}/content/image");

        var response = await Client.DeleteAsync($"/api/steptemplates/{step.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains($"uploads/steptemplates/{image.FileName}", _storage.Deleted);
        Assert.Contains($"uploads/step-template-content/{block.FileName}", _storage.Deleted);
    }

    [Fact]
    public async Task Delete_TemplateUsedInProcess_KeepsStoredFiles()
    {
        var (kind, grade) = await CreateKindWithGrade("STDI-002", "Delete Image Kind 2");
        var step = await CreateTransformStep("STDI-XF-02", "Used Image Step",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var image = await UploadAsync<StepTemplateImageResponseDto>($"/api/steptemplates/{step.Id}/images");
        var process = await CreateProcess("STDI-PROC-02", "Uses Image Step");
        await AddProcessStep(process.Id, step.Id, 1);

        var response = await Client.DeleteAsync($"/api/steptemplates/{step.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.DoesNotContain($"uploads/steptemplates/{image.FileName}", _storage.Deleted);
    }

    private async Task<T> UploadAsync<T>(string url)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(file, "File", "photo.png");

        var response = await Client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }
}

/// <summary>
/// Test factory that swaps the disk-backed image storage for an in-memory fake,
/// so tests can see which files the API asked to delete.
/// </summary>
public class FakeImageStorageFactory : TestWebApplicationFactory
{
    public FakeImageStorage Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.AddSingleton<IImageStorageService>(Storage));
    }
}

public class FakeImageStorage : IImageStorageService
{
    public ConcurrentBag<string> Deleted { get; } = new();

    public Task<(string fileName, string relativePath)> SaveAsync(IFormFile file, string subfolder)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        return Task.FromResult((fileName, $"uploads/{subfolder}/{fileName}"));
    }

    public Task DeleteAsync(string relativePath)
    {
        Deleted.Add(relativePath);
        return Task.CompletedTask;
    }
}
