extern alias Web;

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using ProcessManager.Api.DTOs;
using Web::ProcessManager.Web.Services;
using ProcessDetail = Web::ProcessManager.Web.Components.Pages.Processes.ProcessDetail;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The Process detail page loads step templates, a step's ports and validation results from the API
/// in response to clicks. A failed request must show an error toast. An exception escaping a Blazor
/// event handler tears down the user's whole circuit instead.
/// </summary>
public class ProcessDetailLoadErrorTests : IntegrationTestBase
{
    public ProcessDetailLoadErrorTests(TestWebApplicationFactory factory) : base(factory) { }

    public static TheoryData<string, string?> LoadHandlers => new()
    {
        { "ShowAddStep", null },
        { "Validate", null },
        { "OnFlowSrcStepChanged", "_flowSrcStepId" },
        { "OnFlowTgtStepChanged", "_flowTgtStepId" },
    };

    [Theory]
    [MemberData(nameof(LoadHandlers))]
    public async Task Handler_ApiFailure_ShowsErrorToastInsteadOfThrowing(string handler, string? stepIdField)
    {
        var scenario = await BuildWidgetFinishingScenario();
        var process = (await Client.GetFromJsonAsync<ProcessResponseDto>($"/api/processes/{scenario.Process.Id}", JsonOptions))!;
        using var renderer = new TestRenderer(new ApiClient(FailingClient(), JsonOptions));
        var page = await renderer.Render(process.Id);
        // The initial load failed too; give the page the process so the handler reaches its API call.
        SetField(page, "_proc", process);
        if (stepIdField != null) SetField(page, stepIdField, scenario.ProcessStep1.Id.ToString());
        var toastsBefore = renderer.Toasts.Toasts.Count;

        var ex = await Record.ExceptionAsync(() => renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, handler)));

        Assert.Null(ex);
        var toast = Assert.Single(renderer.Toasts.Toasts.Skip(toastsBefore));
        Assert.Equal(ToastType.Danger, toast.Type);
    }

    [Fact]
    public async Task Handlers_ApiAvailable_LoadTemplatesPortsAndValidationWithoutToast()
    {
        var scenario = await BuildWidgetFinishingScenario();
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.Render(scenario.Process.Id);

        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "ShowAddStep"));
        SetField(page, "_flowSrcStepId", scenario.ProcessStep1.Id.ToString());
        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "OnFlowSrcStepChanged"));
        SetField(page, "_flowTgtStepId", scenario.ProcessStep2.Id.ToString());
        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "OnFlowTgtStepChanged"));
        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "Validate"));

        Assert.Empty(renderer.Toasts.Toasts);
        var templates = (PaginatedResponse<StepTemplateResponseDto>)GetField(page, "_templates")!;
        Assert.Contains(templates.Items, t => t.Id == scenario.DeburrStep.Id);
        var srcPorts = (List<PortResponseDto>)GetField(page, "_flowSrcPorts")!;
        Assert.Equal(new[] { scenario.DeburrOutPort.Id }, srcPorts.Select(p => p.Id));
        var tgtPorts = (List<PortResponseDto>)GetField(page, "_flowTgtPorts")!;
        Assert.Equal(new[] { scenario.InspInPort.Id }, tgtPorts.Select(p => p.Id));
        Assert.NotNull(GetField(page, "_validation"));
    }

    private static HttpClient FailingClient() =>
        new(new FailingHandler()) { BaseAddress = new Uri("http://localhost/") };

    private static Task InvokeTask(IComponent page, string method) =>
        (Task)page.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(page, null)!;

    private static object? GetField(IComponent page, string name) =>
        page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);

    private static void SetField(IComponent page, string name, object value) =>
        page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, value);

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }

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

        public async Task<IComponent> Render(Guid processId)
        {
            var page = InstantiateComponent(typeof(ProcessDetail));
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = processId });
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(AssignRootComponentId(page), parameters));
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
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/");

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
