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
using StepExecutionDetail = Web::ProcessManager.Web.Components.Pages.StepExecutions.StepExecutionDetail;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The Step Execution detail page starts loading the Instructions tab (the Job, the step's content
/// and its prompt responses) without awaiting it. A failed request there must show an error toast;
/// otherwise the exception goes to an unobserved task and the tab silently shows no content.
/// </summary>
public class StepExecutionInstructionsLoadErrorTests : IntegrationTestBase
{
    public StepExecutionInstructionsLoadErrorTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Detail_InstructionsFailure_ShowsErrorToast()
    {
        var step1 = await StartJobAndGetFirstStep();
        // The execution, transactions and data load; only the Instructions tab's Job request fails.
        var api = new ApiClient(FailingClient(path => path.StartsWith("/api/jobs/")), JsonOptions);
        using var renderer = new TestRenderer(api);

        var page = await renderer.Render<StepExecutionDetail>(("Id", step1.Id));
        await WaitUntil(() => !(bool)GetField(page, "_contentLoading")!);

        var toast = Assert.Single(renderer.Toasts.Toasts);
        Assert.Equal(ToastType.Danger, toast.Type);
        Assert.Contains("instructions", toast.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(GetField(page, "_exec"));
    }

    [Fact]
    public async Task Detail_InstructionsAvailable_LoadsWithoutToast()
    {
        var step1 = await StartJobAndGetFirstStep();
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));

        var page = await renderer.Render<StepExecutionDetail>(("Id", step1.Id));
        await WaitUntil(() => !(bool)GetField(page, "_contentLoading")!);

        Assert.Empty(renderer.Toasts.Toasts);
        Assert.NotNull(GetField(page, "_promptResponses"));
    }

    private async Task<StepExecutionResponseDto> StartJobAndGetFirstStep()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        return executions!.First(se => se.Sequence == 1);
    }

    /// <summary>Returns 500 for requests whose path matches; forwards the rest to the in-memory API.</summary>
    private HttpClient FailingClient(Func<string, bool> fails) =>
        new(new FailingHandler(Client, fails)) { BaseAddress = new Uri("http://localhost/") };

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException();
            await Task.Delay(20);
        }
    }

    private static object? GetField(IComponent page, string name) =>
        page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);

    private sealed class FailingHandler(HttpClient inner, Func<string, bool> fails) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            fails(request.RequestUri!.AbsolutePath)
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError))
                : inner.SendAsync(new HttpRequestMessage(request.Method, request.RequestUri), cancellationToken);
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

        public async Task<IComponent> Render<TPage>(params (string Name, object? Value)[] parameters) where TPage : IComponent
        {
            var page = InstantiateComponent(typeof(TPage));
            var parameterView = ParameterView.FromDictionary(parameters.ToDictionary(p => p.Name, p => p.Value));
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(AssignRootComponentId(page), parameterView));
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
