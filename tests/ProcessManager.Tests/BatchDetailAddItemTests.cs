extern alias Web;

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using ProcessManager.Api.DTOs;
using Web::ProcessManager.Web.Services;
using BatchDetail = Web::ProcessManager.Web.Components.Pages.Batches.BatchDetail;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The Batch detail page's "Add Item" picker must only offer Items the API will accept.
/// <c>POST /api/batches/{id}/items/{itemId}</c> rejects an Item whose Kind differs from the Batch's.
/// </summary>
public class BatchDetailAddItemTests : IntegrationTestBase
{
    public BatchDetailAddItemTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task AddItemPicker_OnlyOffersItemsOfTheBatchKind()
    {
        var (job, paintKind, paintGrade, widgetKind, widgetGrade) = await SetupJobWithTwoKinds();
        var paintItem = await CreateItem(job.Id, paintKind.Id, paintGrade.Id);
        await CreateItem(job.Id, widgetKind.Id, widgetGrade.Id, $"SN-{Guid.NewGuid().ToString()[..6]}");
        var batch = await CreateBatch(job.Id, paintKind.Id, paintGrade.Id);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.RenderBatchDetail(batch.Id);

        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "ShowAddItemForm"));

        var offered = GetAvailableItems(page);
        Assert.Equal(new[] { paintItem.Id }, offered.Select(i => i.Id));
    }

    [Fact]
    public async Task AddItemPicker_SelectedItemIsAddedToBatch()
    {
        var (job, paintKind, paintGrade, _, _) = await SetupJobWithTwoKinds();
        var paintItem = await CreateItem(job.Id, paintKind.Id, paintGrade.Id);
        var batch = await CreateBatch(job.Id, paintKind.Id, paintGrade.Id);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.RenderBatchDetail(batch.Id);
        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "ShowAddItemForm"));

        SetField(page, "_addItemId", paintItem.Id.ToString());
        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "SaveAddItem"));

        Assert.Empty(renderer.Toasts.Toasts);
        var items = (List<ItemResponseDto>)GetField(page, "_items")!;
        Assert.Equal(new[] { paintItem.Id }, items.Select(i => i.Id));
    }

    private async Task<(JobResponseDto Job, KindResponseDto PaintKind, GradeResponseDto PaintGrade,
        KindResponseDto WidgetKind, GradeResponseDto WidgetGrade)> SetupJobWithTwoKinds()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var paintKind = await CreateKind($"PAINT-{pfx}", "Paint", isBatchable: true);
        var paintGrade = await CreateGrade(paintKind.Id, "RAW", "Raw Material", isDefault: true);
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        return (job, paintKind, paintGrade, scenario.WidgetKind, scenario.RawGrade);
    }

    private static Task InvokeTask(BatchDetail page, string method) =>
        (Task)typeof(BatchDetail)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(page, null)!;

    private static List<ItemResponseDto> GetAvailableItems(BatchDetail page) =>
        ((PaginatedResponse<ItemResponseDto>)GetField(page, "_availableItems")!).Items;

    private static object? GetField(BatchDetail page, string name) =>
        typeof(BatchDetail).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);

    private static void SetField(BatchDetail page, string name, object value) =>
        typeof(BatchDetail).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, value);

    private sealed class TestRenderer : Renderer
    {
        public TestRenderer(ApiClient api) : this(new ServiceCollection()
            .AddSingleton(api)
            .AddSingleton<ToastService>()
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .AddSingleton<IJSRuntime, NoJSRuntime>()
            .BuildServiceProvider()) { }

        private TestRenderer(IServiceProvider services) : base(services, NullLoggerFactory.Instance)
        {
            Toasts = services.GetRequiredService<ToastService>();
        }

        public ToastService Toasts { get; }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public async Task<BatchDetail> RenderBatchDetail(Guid id)
        {
            var page = (BatchDetail)InstantiateComponent(typeof(BatchDetail));
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(
                AssignRootComponentId(page),
                ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(BatchDetail.Id)] = id })));
            return page;
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
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/batches");

        protected override void NavigateToCore(string uri, NavigationOptions options) { }
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new NotSupportedException("JS interop is not available in this test.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new NotSupportedException("JS interop is not available in this test.");
    }
}
