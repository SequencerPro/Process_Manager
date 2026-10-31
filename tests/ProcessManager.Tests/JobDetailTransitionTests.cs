extern alias Web;

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using ProcessManager.Api.DTOs;
using Web::ProcessManager.Web.Services;
using JobDetail = Web::ProcessManager.Web.Components.Pages.Jobs.JobDetail;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The Job detail page's lifecycle buttons (Start, Hold, Resume, Complete, Cancel) must
/// keep showing the job's steps afterwards. The transition endpoints return the job
/// without its step executions, so the page can't use that response as-is.
/// </summary>
public class JobDetailTransitionTests : IntegrationTestBase
{
    public JobDetailTransitionTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task InitialLoad_ShowsJobSteps()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));

        var page = await renderer.RenderJobDetail(job.Id);

        var shown = GetJob(page);
        Assert.Equal("Created", shown.Status);
        Assert.Equal(2, shown.StepExecutions?.Count);
    }

    [Theory]
    [InlineData("start", "InProgress")]
    [InlineData("cancel", "Cancelled")]
    public async Task Transition_KeepsJobSteps(string action, string expectedStatus)
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.RenderJobDetail(job.Id);

        await renderer.Dispatcher.InvokeAsync(() => InvokeTransition(page, action));

        var shown = GetJob(page);
        Assert.Equal(expectedStatus, shown.Status);
        Assert.Empty(renderer.Toasts.Toasts);
        Assert.NotNull(shown.StepExecutions);
        Assert.Equal(
            job.StepExecutions!.Select(se => se.Id),
            shown.StepExecutions!.Select(se => se.Id));
    }

    private static Task InvokeTransition(JobDetail page, string action) =>
        (Task)typeof(JobDetail)
            .GetMethod("Transition", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(page, new object[] { action })!;

    private static JobResponseDto GetJob(JobDetail page) =>
        (JobResponseDto)typeof(JobDetail)
            .GetField("_job", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(page)!;

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

        public async Task<JobDetail> RenderJobDetail(Guid id)
        {
            var page = (JobDetail)InstantiateComponent(typeof(JobDetail));
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(
                AssignRootComponentId(page),
                ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(JobDetail.Id)] = id })));
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
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/jobs");

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
