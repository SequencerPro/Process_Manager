using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Each image uploaded to a StepTemplate gets a SortOrder after every existing
/// image, even when an earlier image has been deleted.
/// </summary>
public class StepTemplateImageSortOrderTests
    : IntegrationTestBase, IClassFixture<StepTemplateImageSortOrderTests.Factory>
{
    public StepTemplateImageSortOrderTests(Factory factory) : base(factory) { }

    [Fact]
    public async Task UploadImage_AfterDeletingEarlierImage_GetsUniqueSortOrder()
    {
        var (kind, grade) = await CreateKindWithGrade("STISO-001", "Image Sort Kind");
        var step = await CreateTransformStep("STISO-XF-01", "Image Sort Step",
            kind.Id, grade.Id, kind.Id, grade.Id);

        var first = await UploadImageAsync(step.Id);
        await UploadImageAsync(step.Id);
        await UploadImageAsync(step.Id);
        (await Client.DeleteAsync($"/api/steptemplates/{step.Id}/images/{first.Id}")).EnsureSuccessStatusCode();

        var added = await UploadImageAsync(step.Id);

        var images = (await Client.GetFromJsonAsync<StepTemplateResponseDto>(
            $"/api/steptemplates/{step.Id}", JsonOptions))!.Images;
        Assert.Equal(3, images.Count);
        Assert.Equal(3, images.Select(i => i.SortOrder).Distinct().Count());
        Assert.Equal(images.Max(i => i.SortOrder), added.SortOrder);
    }

    [Fact]
    public async Task UploadImage_Sequentially_NumbersFromZero()
    {
        var (kind, grade) = await CreateKindWithGrade("STISO-002", "Image Sort Kind 2");
        var step = await CreateTransformStep("STISO-XF-02", "Image Sort Step 2",
            kind.Id, grade.Id, kind.Id, grade.Id);

        var first = await UploadImageAsync(step.Id);
        var second = await UploadImageAsync(step.Id);

        Assert.Equal(0, first.SortOrder);
        Assert.Equal(1, second.SortOrder);
    }

    private async Task<StepTemplateImageResponseDto> UploadImageAsync(Guid stepTemplateId)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(file, "File", "photo.png");

        var response = await Client.PostAsync($"/api/steptemplates/{stepTemplateId}/images", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StepTemplateImageResponseDto>(JsonOptions))!;
    }

    /// <summary>
    /// Swaps the disk-backed image storage for a no-op so these tests don't write files.
    /// </summary>
    public class Factory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IImageStorageService, NoOpImageStorage>());
        }
    }

    private class NoOpImageStorage : IImageStorageService
    {
        public Task<(string fileName, string relativePath)> SaveAsync(IFormFile file, string subfolder)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            return Task.FromResult((fileName, $"uploads/{subfolder}/{fileName}"));
        }

        public Task DeleteAsync(string relativePath) => Task.CompletedTask;
    }
}
