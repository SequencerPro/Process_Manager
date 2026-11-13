extern alias Web;

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;
using Web::ProcessManager.Web.Services;
using StepTemplateDetail = Web::ProcessManager.Web.Components.Pages.StepTemplates.StepTemplateDetail;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The Step Template port form's Kind dropdown must offer every Kind, not just the first
/// page the Kinds endpoint returns.
/// </summary>
public class StepTemplatePortKindListTests : IntegrationTestBase
{
    // One more than the page size the port form used to request.
    private const int KindCount = 101;

    public StepTemplatePortKindListTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAllKindsAsync_ReturnsKindsBeyondTheFirstPage()
    {
        var created = await CreateKindsSortingLast();
        var api = new ApiClient(Client, JsonOptions);

        var all = await api.GetAllKindsAsync();

        Assert.NotNull(all);
        var ids = all!.Items.Select(k => k.Id).ToList();
        Assert.All(created, k => Assert.Contains(k.Id, ids));
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(all.Items.Count, all.TotalCount);
    }

    [Fact]
    public async Task PortForm_OffersKindsBeyondTheFirst100()
    {
        var created = await CreateKindsSortingLast();
        var (kind, grade) = await CreateKindWithGrade($"PL-{Guid.NewGuid():N}"[..20], "Port list kind");
        var st = await CreateTransformStep($"ST-{Guid.NewGuid():N}"[..20], "Port list template",
            kind.Id, grade.Id, kind.Id, grade.Id);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));

        var page = await renderer.OpenAddPortForm(st.Id);

        var kinds = (PaginatedResponse<KindResponseDto>)GetField(page, "_kinds")!;
        // Kinds are listed by Name, so the last one created sorts after at least 100 others.
        Assert.Contains(kinds.Items, k => k.Id == created[^1].Id);
    }

    /// <summary>Creates <see cref="KindCount"/> Kinds whose names sort after any other test's Kinds.</summary>
    private async Task<List<KindResponseDto>> CreateKindsSortingLast()
    {
        var pfx = Guid.NewGuid().ToString("N")[..8];
        var kinds = new List<KindResponseDto>();
        for (var i = 0; i < KindCount; i++)
            kinds.Add(await CreateKind($"ZZ-{pfx}-{i:D3}", $"zzz {pfx} {i:D3}"));
        return kinds;
    }

    private static object? GetField(IComponent page, string name) =>
        page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);

    private sealed class TestRenderer : Renderer
    {
        public TestRenderer(ApiClient api) : this(new ServiceCollection()
            .AddSingleton(api)
            .AddSingleton<ToastService>()
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .AddSingleton<IJSRuntime, NoJSRuntime>()
            .BuildServiceProvider()) { }

        private TestRenderer(IServiceProvider services) : base(services, NullLoggerFactory.Instance) { }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        /// <summary>Renders the page and opens the Add Input Port form, waiting for its Kind list.</summary>
        public async Task<IComponent> OpenAddPortForm(Guid stepTemplateId)
        {
            var page = InstantiateComponent(typeof(StepTemplateDetail));
            var pageId = AssignRootComponentId(page);
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = stepTemplateId });
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(pageId, parameters));

            await Dispatcher.InvokeAsync(() =>
                page.GetType().GetMethod("ShowPortForm", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(page, new object[] { PortDirection.Input }));

            // ShowPortForm starts LoadLookups without awaiting it.
            for (var i = 0; i < 200; i++)
            {
                if (await Dispatcher.InvokeAsync(() => GetField(page, "_kinds") is not null)) return page;
                await Task.Delay(25);
            }
            throw new TimeoutException("The port form's Kind list never loaded.");
        }

        // The page declares @rendermode InteractiveServer; this host renders it in place.
        protected override IComponent ResolveComponentForRenderMode(
            Type componentType, int? parentComponentId,
            IComponentActivator componentActivator, IComponentRenderMode renderMode) =>
            componentActivator.CreateInstance(componentType);

        protected override void HandleException(Exception exception) => throw exception;

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/");

        protected override void NavigateToCore(string uri, NavigationOptions options) { }
    }

    /// <summary>The page's InputFile components call JS after each render; these tests don't need any result.</summary>
    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    }
}
