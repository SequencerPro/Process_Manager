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
using WorkflowDetail = Web::ProcessManager.Web.Components.Pages.Workflows.WorkflowDetail;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The Workflow detail page's "Add Process" button loads the active Processes from the API.
/// A failed request must show an error toast. An exception escaping a Blazor event handler
/// tears down the user's whole circuit instead.
/// </summary>
public class WorkflowDetailLoadErrorTests : IntegrationTestBase
{
    public WorkflowDetailLoadErrorTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ShowAddProcess_ApiFailure_ShowsErrorToastInsteadOfThrowing()
    {
        using var renderer = new TestRenderer(new ApiClient(FailingClient(), JsonOptions));
        var page = await renderer.Render(Guid.NewGuid());
        var toastsBefore = renderer.Toasts.Toasts.Count;

        var ex = await Record.ExceptionAsync(() =>
            renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "ShowAddProcess")));

        Assert.Null(ex);
        var toast = Assert.Single(renderer.Toasts.Toasts.Skip(toastsBefore));
        Assert.Equal(ToastType.Danger, toast.Type);
    }

    [Fact]
    public async Task ShowAddProcess_ApiAvailable_LoadsActiveProcessesWithoutToast()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var wfResponse = await Client.PostAsJsonAsync("/api/workflows",
            new CreateWorkflowDto($"WF-{Guid.NewGuid():N}"[..20], "Detail page workflow"), JsonOptions);
        wfResponse.EnsureSuccessStatusCode();
        var workflow = (await wfResponse.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions))!;

        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.Render(workflow.Id);

        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "ShowAddProcess"));

        Assert.Empty(renderer.Toasts.Toasts);
        Assert.True((bool)GetField(page, "_showProcessForm")!);
        var processes = (PaginatedResponse<ProcessSummaryResponseDto>)GetField(page, "_availableProcesses")!;
        Assert.Contains(processes.Items, p => p.Id == scenario.Process.Id);
    }

    private static HttpClient FailingClient() =>
        new(new FailingHandler()) { BaseAddress = new Uri("http://localhost/") };

    private static Task InvokeTask(IComponent page, string method) =>
        (Task)page.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(page, null)!;

    private static object? GetField(IComponent page, string name) =>
        page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);

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

        public async Task<IComponent> Render(Guid workflowId)
        {
            var page = InstantiateComponent(typeof(WorkflowDetail));
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = workflowId });
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
