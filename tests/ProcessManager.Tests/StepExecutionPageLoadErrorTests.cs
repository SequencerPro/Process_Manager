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
using StepExecutionList = Web::ProcessManager.Web.Components.Pages.StepExecutions.StepExecutionList;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The Step Execution detail page loads the execution, its port transactions and its data when it
/// opens, and the Step Execution list loads the Job filter's Jobs. A failed request must show an
/// error toast. An exception escaping initialization tears down the user's whole circuit instead.
/// </summary>
public class StepExecutionPageLoadErrorTests : IntegrationTestBase
{
    public StepExecutionPageLoadErrorTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Detail_Initialize_ApiFailure_ShowsErrorToastInsteadOfThrowing()
    {
        using var renderer = new TestRenderer(new ApiClient(FailingClient(_ => true), JsonOptions));

        var ex = await Record.ExceptionAsync(() => renderer.Render<StepExecutionDetail>(("Id", Guid.NewGuid())));

        Assert.Null(ex);
        var toast = Assert.Single(renderer.Toasts.Toasts);
        Assert.Equal(ToastType.Danger, toast.Type);
        Assert.False((bool)GetField(renderer.Page!, "_loading")!);
    }

    [Fact]
    public async Task List_Initialize_JobsFailure_ShowsErrorToastAndStillLoadsExecutions()
    {
        // Only the Job filter's request fails; the step executions themselves load.
        var api = new ApiClient(FailingClient(path => path.StartsWith("/api/jobs")), JsonOptions);
        using var renderer = new TestRenderer(api);

        var ex = await Record.ExceptionAsync(() => renderer.Render<StepExecutionList>());

        Assert.Null(ex);
        var toast = Assert.Single(renderer.Toasts.Toasts);
        Assert.Equal(ToastType.Danger, toast.Type);
        Assert.False((bool)GetField(renderer.Page!, "_loading")!);
        Assert.NotNull(GetField(renderer.Page!, "_data"));
    }

    [Fact]
    public async Task Detail_Initialize_ApiAvailable_LoadsExecutionWithoutToast()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));

        var page = await renderer.Render<StepExecutionDetail>(("Id", step1.Id));
        await WaitUntil(() => !(bool)GetField(page, "_contentLoading")!);

        Assert.Empty(renderer.Toasts.Toasts);
        Assert.False((bool)GetField(page, "_loading")!);
        var exec = (StepExecutionResponseDto)GetField(page, "_exec")!;
        Assert.Equal(step1.Id, exec.Id);
        Assert.NotNull(GetField(page, "_transactions"));
        Assert.NotNull(GetField(page, "_data"));
    }

    [Fact]
    public async Task List_Initialize_ApiAvailable_LoadsJobsAndExecutionsWithoutToast()
    {
        var scenario = await BuildWidgetFinishingScenario();
        await CreateJob(scenario.Process.Id);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));

        var page = await renderer.Render<StepExecutionList>();

        Assert.Empty(renderer.Toasts.Toasts);
        Assert.False((bool)GetField(page, "_loading")!);
        Assert.NotEmpty((System.Collections.IList)GetField(page, "_jobs")!);
        Assert.NotNull(GetField(page, "_data"));
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

        public IComponent? Page { get; private set; }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public async Task<IComponent> Render<TPage>(params (string Name, object? Value)[] parameters) where TPage : IComponent
        {
            Page = InstantiateComponent(typeof(TPage));
            var parameterView = ParameterView.FromDictionary(parameters.ToDictionary(p => p.Name, p => p.Value));
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(AssignRootComponentId(Page), parameterView));
            return Page;
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

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new NotSupportedException("JS interop is not available in this test.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new NotSupportedException("JS interop is not available in this test.");
    }
}
