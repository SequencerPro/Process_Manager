extern alias Web;

using System.Net;
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
/// On the Step Template detail page, the Add/Edit Port form's Grade dropdown lists the
/// Grades of the selected Kind. Choosing a Kind in the form must load that Kind's Grades
/// and drop a Grade picked for a different Kind.
/// </summary>
public class StepTemplatePortKindChangeTests : IntegrationTestBase
{
    public StepTemplatePortKindChangeTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ChangingKind_LoadsThatKindsGrades()
    {
        var (kind, grade) = await CreateKindWithGrade($"PK-{Guid.NewGuid():N}"[..20], "Port kind");
        var st = await CreateTemplate(kind.Id, grade.Id);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.OpenAddPortForm(st.Id);

        await renderer.ChangeKind(kind.Id);

        var grades = (List<GradeResponseDto>?)GetField(page, "_grades");
        Assert.NotNull(grades);
        Assert.Contains(grades!, g => g.Id == grade.Id);
    }

    [Fact]
    public async Task ChangingKind_ClearsGradeOfPreviousKind()
    {
        var (kindA, gradeA) = await CreateKindWithGrade($"PA-{Guid.NewGuid():N}"[..20], "Port kind A");
        var (kindB, gradeB) = await CreateKindWithGrade($"PB-{Guid.NewGuid():N}"[..20], "Port kind B");
        var st = await CreateTemplate(kindA.Id, gradeA.Id);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.OpenAddPortForm(st.Id);
        await renderer.ChangeKind(kindA.Id);
        await renderer.Dispatcher.InvokeAsync(() => SetField(page, "_portGradeId", gradeA.Id.ToString()));

        await renderer.ChangeKind(kindB.Id);

        Assert.Equal("", GetField(page, "_portGradeId"));
        var grades = (List<GradeResponseDto>?)GetField(page, "_grades");
        Assert.NotNull(grades);
        Assert.Contains(grades!, g => g.Id == gradeB.Id);
        Assert.DoesNotContain(grades!, g => g.Id == gradeA.Id);
    }

    [Fact]
    public async Task ChangingKind_KindRequestFails_ShowsErrorToast()
    {
        var (kind, grade) = await CreateKindWithGrade($"PF-{Guid.NewGuid():N}"[..20], "Port kind");
        var st = await CreateTemplate(kind.Id, grade.Id);
        var handler = new SingleKindFailingHandler(Client);
        using var renderer = new TestRenderer(new ApiClient(OuterClient(handler), JsonOptions));
        var page = await renderer.OpenAddPortForm(st.Id);
        var toastsBefore = renderer.Toasts.Toasts.Count;

        await renderer.ChangeKind(kind.Id);

        var toast = Assert.Single(renderer.Toasts.Toasts.Skip(toastsBefore));
        Assert.Equal(ToastType.Danger, toast.Type);
        var grades = (List<GradeResponseDto>?)GetField(page, "_grades");
        Assert.True(grades is null || grades.Count == 0);
    }

    private Task<StepTemplateResponseDto> CreateTemplate(Guid kindId, Guid gradeId) =>
        CreateTransformStep($"ST-{Guid.NewGuid():N}"[..20], "Port form template", kindId, gradeId, kindId, gradeId);

    private static HttpClient OuterClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("http://localhost/") };

    private static object? GetField(IComponent page, string name) =>
        page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);

    private static void SetField(IComponent page, string name, object? value) =>
        page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, value);

    /// <summary>Returns 500 for GET /api/kinds/{id}; forwards everything else, including the Kind list.</summary>
    private sealed class SingleKindFailingHandler : HttpMessageHandler
    {
        private readonly HttpClient _inner;
        public SingleKindFailingHandler(HttpClient inner) => _inner = inner;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.StartsWith("/api/kinds/", StringComparison.OrdinalIgnoreCase) && path.Length > "/api/kinds/".Length)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            var copy = new HttpRequestMessage(request.Method, request.RequestUri) { Content = request.Content };
            return await _inner.SendAsync(copy, cancellationToken);
        }
    }

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

        /// <summary>Renders the page and opens the Add Input Port form, waiting for its Kind list.</summary>
        public async Task<IComponent> OpenAddPortForm(Guid stepTemplateId)
        {
            _page = InstantiateComponent(typeof(StepTemplateDetail));
            _pageId = AssignRootComponentId(_page);
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = stepTemplateId });
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(_pageId, parameters));

            await Dispatcher.InvokeAsync(() =>
                _page.GetType().GetMethod("ShowPortForm", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(_page, new object[] { PortDirection.Input }));

            // ShowPortForm starts LoadLookups without awaiting it.
            for (var i = 0; i < 200; i++)
            {
                if (await Dispatcher.InvokeAsync(() => GetField(_page, "_kinds") is not null)) return _page;
                await Task.Delay(25);
            }
            throw new TimeoutException("The port form's Kind list never loaded.");
        }

        /// <summary>Fires the Kind dropdown's change event, as the browser does when the user picks a Kind.</summary>
        public Task ChangeKind(Guid kindId) => Dispatcher.InvokeAsync(() =>
        {
            var value = kindId.ToString();
            var handlerId = FindChangeHandlerOfSelectWithOption(value);
            return DispatchEventAsync(handlerId,
                new EventFieldInfo { ComponentId = _pageId, FieldValue = value },
                new ChangeEventArgs { Value = value });
        });

        private ulong FindChangeHandlerOfSelectWithOption(string optionValue)
        {
            var frames = GetCurrentRenderTreeFrames(_pageId);
            for (var i = 0; i < frames.Count; i++)
            {
                ref var select = ref frames.Array[i];
                if (select.FrameType != RenderTreeFrameType.Element || select.ElementName != "select") continue;

                var end = i + select.ElementSubtreeLength;
                ulong onChange = 0;
                var hasOption = false;
                for (var j = i + 1; j < end; j++)
                {
                    ref var f = ref frames.Array[j];
                    if (f.FrameType != RenderTreeFrameType.Attribute) continue;
                    if (f.AttributeName == "onchange" && f.AttributeEventHandlerId != 0) onChange = f.AttributeEventHandlerId;
                    if (f.AttributeName == "value" && f.AttributeValue?.ToString() == optionValue) hasOption = true;
                }
                if (hasOption && onChange != 0) return onChange;
            }
            throw new InvalidOperationException($"No <select> offers the option '{optionValue}'.");
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
