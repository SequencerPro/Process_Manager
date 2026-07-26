using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Phase 36.3 (T3.4, T3.6) — integration tests for the hardened atomic
/// content-block reorder endpoint and the new promote-to-shared endpoint.
/// </summary>
public class Phase36_BuilderPolishTests : IntegrationTestBase
{
    public Phase36_BuilderPolishTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────── Helpers ────────────

    private async Task<StepTemplateResponseDto> CreateSimpleTemplate(bool shared = true, string? code = null)
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var (kind, grade) = await CreateKindWithGrade($"K-{pfx}", $"Polish Kind {pfx}");

        // Build via DTO so we can control IsShared.
        var dto = new StepTemplateCreateDto(
            code ?? $"ST-{pfx}",
            "Polish Step",
            null,
            StepPattern.Transform,
            new List<PortCreateDto>
            {
                new("In", PortDirection.Input, PortType.Material, kind.Id, grade.Id,
                    QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
                new("Out", PortDirection.Output, PortType.Material, kind.Id, grade.Id,
                    QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            },
            IsShared: shared);

        var resp = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;
    }

    private async Task<Guid> AddTextBlock(Guid templateId, string body)
    {
        var resp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{templateId}/content/text",
            new AddStepTemplateTextBlockDto(body, "Setup"),
            JsonOptions);
        resp.EnsureSuccessStatusCode();
        var block = await resp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(JsonOptions);
        return block!.Id;
    }

    private async Task<List<StepTemplateContentResponseDto>> GetBlocks(Guid templateId)
    {
        return (await Client.GetFromJsonAsync<List<StepTemplateContentResponseDto>>(
            $"/api/steptemplates/{templateId}/content", JsonOptions))!;
    }

    // ──────────── T3.4 — Reorder ────────────

    [Fact]
    public async Task Reorder_FullList_AppliesNewOrderAtomically()
    {
        var template = await CreateSimpleTemplate();
        var id1 = await AddTextBlock(template.Id, "First");
        var id2 = await AddTextBlock(template.Id, "Second");
        var id3 = await AddTextBlock(template.Id, "Third");

        // Reverse the order in one call.
        var resp = await Client.PutAsJsonAsync(
            $"/api/steptemplates/{template.Id}/content/reorder",
            new ReorderStepTemplateContentBlocksDto(new List<Guid> { id3, id2, id1 }),
            JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var blocks = await GetBlocks(template.Id);
        Assert.Equal(3, blocks.Count);
        Assert.Equal(id3, blocks[0].Id);
        Assert.Equal(id2, blocks[1].Id);
        Assert.Equal(id1, blocks[2].Id);

        // Contiguous, no gaps or duplicates.
        Assert.Equal(new[] { 0, 1, 2 }, blocks.Select(b => b.SortOrder));
    }

    [Fact]
    public async Task Reorder_PartialList_Returns400()
    {
        var template = await CreateSimpleTemplate();
        var id1 = await AddTextBlock(template.Id, "First");
        await AddTextBlock(template.Id, "Second");

        // Send only one of the two block IDs — this would corrupt ordering
        // under the old N-PATCH approach. Should be rejected.
        var resp = await Client.PutAsJsonAsync(
            $"/api/steptemplates/{template.Id}/content/reorder",
            new ReorderStepTemplateContentBlocksDto(new List<Guid> { id1 }),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reorder_DuplicateIds_Returns400()
    {
        var template = await CreateSimpleTemplate();
        var id1 = await AddTextBlock(template.Id, "First");
        var id2 = await AddTextBlock(template.Id, "Second");

        var resp = await Client.PutAsJsonAsync(
            $"/api/steptemplates/{template.Id}/content/reorder",
            new ReorderStepTemplateContentBlocksDto(new List<Guid> { id1, id2, id1 }),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reorder_UnknownBlockId_Returns400()
    {
        var template = await CreateSimpleTemplate();
        var id1 = await AddTextBlock(template.Id, "First");

        var resp = await Client.PutAsJsonAsync(
            $"/api/steptemplates/{template.Id}/content/reorder",
            new ReorderStepTemplateContentBlocksDto(new List<Guid> { id1, Guid.NewGuid() }),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ──────────── T3.6 — Promote inline → shared ────────────

    [Fact]
    public async Task Promote_InlineDraftTemplate_FlipsIsSharedTrue()
    {
        var template = await CreateSimpleTemplate(shared: false);
        Assert.False(template.IsShared);

        var resp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{template.Id}/promote-to-shared",
            new PromoteInlineStepTemplateDto(),
            JsonOptions);
        resp.EnsureSuccessStatusCode();

        var promoted = await resp.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.NotNull(promoted);
        Assert.True(promoted!.IsShared);
    }

    [Fact]
    public async Task Promote_AlreadyShared_Returns400()
    {
        var template = await CreateSimpleTemplate(shared: true);

        var resp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{template.Id}/promote-to-shared",
            new PromoteInlineStepTemplateDto(),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Promote_WithCodeOverride_AppliesAndRejectsCollision()
    {
        var inline = await CreateSimpleTemplate(shared: false);
        var existing = await CreateSimpleTemplate(shared: true);

        // Try to promote with a colliding code — should 409.
        var collide = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{inline.Id}/promote-to-shared",
            new PromoteInlineStepTemplateDto(Code: existing.Code),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, collide.StatusCode);

        // Inline must still be unshared after the failed promotion.
        var stillInline = await Client.GetFromJsonAsync<StepTemplateResponseDto>(
            $"/api/steptemplates/{inline.Id}", JsonOptions);
        Assert.False(stillInline!.IsShared);

        // Now promote with a fresh code — should succeed.
        var pfx = Guid.NewGuid().ToString()[..6];
        var newCode = $"PROMO-{pfx}";
        var ok = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{inline.Id}/promote-to-shared",
            new PromoteInlineStepTemplateDto(Code: newCode, Name: "Promoted Step"),
            JsonOptions);
        ok.EnsureSuccessStatusCode();

        var promoted = await ok.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.NotNull(promoted);
        Assert.True(promoted!.IsShared);
        Assert.Equal(newCode, promoted.Code);
        Assert.Equal("Promoted Step", promoted.Name);
    }

    [Fact]
    public async Task Promote_UnknownId_Returns404()
    {
        var resp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{Guid.NewGuid()}/promote-to-shared",
            new PromoteInlineStepTemplateDto(),
            JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
