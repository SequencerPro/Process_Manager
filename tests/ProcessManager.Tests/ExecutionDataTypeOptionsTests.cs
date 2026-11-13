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
using BatchDetail = Web::ProcessManager.Web.Components.Pages.Batches.BatchDetail;
using ItemDetail = Web::ProcessManager.Web.Components.Pages.Items.ItemDetail;
using StepExecutionDetail = Web::ProcessManager.Web.Components.Pages.StepExecutions.StepExecutionDetail;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The Item, Batch and Step Execution detail pages each have an "Add Data" form whose Data Type
/// dropdown is parsed with <c>Enum.Parse&lt;DataValueType&gt;</c> on save. The dropdown must offer
/// exactly the <see cref="DataValueType"/> names, so every choice can be saved.
/// </summary>
public class ExecutionDataTypeOptionsTests : IntegrationTestBase
{
    public ExecutionDataTypeOptionsTests(TestWebApplicationFactory factory) : base(factory) { }

    public static TheoryData<string> Pages => new() { "Item", "Batch", "StepExecution" };

    public static TheoryData<string, string> PagesAndNumericTypes => new()
    {
        { "Item", "Integer" }, { "Item", "Decimal" },
        { "Batch", "Integer" }, { "Batch", "Decimal" },
        { "StepExecution", "Integer" }, { "StepExecution", "Decimal" },
    };

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task DataTypeDropdown_OffersExactlyTheDataValueTypes(string pageName)
    {
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        await renderer.OpenDataForm(pageName, await CreateTarget(pageName));

        var offered = await renderer.Dispatcher.InvokeAsync(renderer.DataTypeOptions);

        Assert.Equal(Enum.GetNames<DataValueType>(), offered);
    }

    [Theory]
    [MemberData(nameof(PagesAndNumericTypes))]
    public async Task PickingNumericDataType_SavesDataWithThatType(string pageName, string dataType)
    {
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.OpenDataForm(pageName, await CreateTarget(pageName));
        var toastsBefore = renderer.Toasts.Toasts.Count;

        await renderer.PickDataType(dataType);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            SetField(page, "_dataKey", "Length");
            SetField(page, "_dataValue", "12.5");
            await (Task)page.GetType().GetMethod("SaveData", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(page, null)!;
        });

        Assert.Empty(renderer.Toasts.Toasts.Skip(toastsBefore));
        Assert.False((bool)GetField(page, "_showDataForm")!);
        var data = (List<ExecutionDataResponseDto>?)GetField(page, "_data");
        var saved = Assert.Single(data!, d => d.Key == "Length");
        Assert.Equal(Enum.Parse<DataValueType>(dataType), saved.DataType);
    }

    /// <summary>Creates the Item, Batch or Step Execution whose detail page is under test.</summary>
    private async Task<Guid> CreateTarget(string pageName)
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        var (kind, grade) = await CreateKindWithGrade($"DT-{Guid.NewGuid():N}"[..20], "Data kind", isBatchable: true);
        return pageName switch
        {
            "Item" => (await CreateItem(job.Id, kind.Id, grade.Id)).Id,
            "Batch" => (await CreateBatch(job.Id, kind.Id, grade.Id)).Id,
            "StepExecution" => job.StepExecutions!.OrderBy(se => se.Sequence).First().Id,
            _ => throw new ArgumentOutOfRangeException(nameof(pageName))
        };
    }

    private static object? GetField(IComponent page, string name) =>
        page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);

    private static void SetField(IComponent page, string name, object? value) =>
        page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, value);

    private sealed class TestRenderer : Renderer
    {
        private IComponent? _page;
        private int _pageId;

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

        /// <summary>Renders the page, waits for it to load and opens its Add Data form.</summary>
        public async Task<IComponent> OpenDataForm(string pageName, Guid id)
        {
            var pageType = pageName switch
            {
                "Item" => typeof(ItemDetail),
                "Batch" => typeof(BatchDetail),
                "StepExecution" => typeof(StepExecutionDetail),
                _ => throw new ArgumentOutOfRangeException(nameof(pageName))
            };
            _page = InstantiateComponent(pageType);
            _pageId = AssignRootComponentId(_page);
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = id });
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(_pageId, parameters));

            // StepExecutionDetail starts LoadInstructions without awaiting it.
            for (var i = 0; i < 200; i++)
            {
                var loaded = await Dispatcher.InvokeAsync(() =>
                    GetField(_page, "_loading") is false &&
                    _page.GetType().GetField("_contentLoading", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(_page) is null or false);
                if (loaded) break;
                await Task.Delay(25);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                _page.GetType().GetMethod("ShowDataForm", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(_page, null);
                typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(_page, null);
            });
            Assert.True((bool)GetField(_page, "_showDataForm")!);
            Assert.Empty(Toasts.Toasts);
            return _page;
        }

        /// <summary>The option values of the Add Data form's Data Type dropdown, in order.</summary>
        public string[] DataTypeOptions() => FindDataTypeSelect().Options;

        /// <summary>Fires the Data Type dropdown's change event, as the browser does when the user picks a type.</summary>
        public Task PickDataType(string value) => Dispatcher.InvokeAsync(() =>
        {
            var (onChange, options) = FindDataTypeSelect();
            if (!options.Contains(value))
                throw new InvalidOperationException($"The Data Type dropdown doesn't offer '{value}'.");
            return DispatchEventAsync(onChange,
                new EventFieldInfo { ComponentId = _pageId, FieldValue = value },
                new ChangeEventArgs { Value = value });
        });

        private (ulong OnChange, string[] Options) FindDataTypeSelect()
        {
            var frames = GetCurrentRenderTreeFrames(_pageId);
            for (var i = 0; i < frames.Count; i++)
            {
                ref var select = ref frames.Array[i];
                if (select.FrameType != RenderTreeFrameType.Element || select.ElementName != "select") continue;

                var end = i + select.ElementSubtreeLength;
                ulong onChange = 0;
                var options = new List<string>();
                var element = "select";
                for (var j = i + 1; j < end; j++)
                {
                    ref var f = ref frames.Array[j];
                    if (f.FrameType == RenderTreeFrameType.Element) { element = f.ElementName; continue; }
                    if (f.FrameType != RenderTreeFrameType.Attribute) continue;
                    if (element == "select" && f.AttributeName == "onchange" && f.AttributeEventHandlerId != 0)
                        onChange = f.AttributeEventHandlerId;
                    else if (element == "option" && f.AttributeName == "value" && f.AttributeValue is not null)
                        options.Add(f.AttributeValue.ToString()!);
                }
                // The Data Type dropdown is the only one offering both String and Boolean.
                if (onChange != 0 && options.Contains("String") && options.Contains("Boolean"))
                    return (onChange, options.ToArray());
            }
            throw new InvalidOperationException("The Add Data form's Data Type dropdown wasn't rendered.");
        }

        // The pages declare @rendermode InteractiveServer; this host renders them in place.
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

    /// <summary>None of the tested code paths needs a JS result.</summary>
    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    }
}
