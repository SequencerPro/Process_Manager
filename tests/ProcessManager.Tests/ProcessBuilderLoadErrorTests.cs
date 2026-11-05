extern alias Web;

using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Web::ProcessManager.Web.Services;
using ProcessBuilder = Web::ProcessManager.Web.Components.Pages.Processes.ProcessBuilder;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The Process Builder loads the process and its step templates when it opens, and loads validation
/// results when the user clicks Validate. A failed request must show an error toast. An exception
/// escaping initialization or an event handler tears down the user's whole circuit instead.
/// </summary>
public class ProcessBuilderLoadErrorTests : IntegrationTestBase
{
    public ProcessBuilderLoadErrorTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Initialize_ApiFailure_ShowsErrorToastInsteadOfThrowing()
    {
        var scenario = await BuildWidgetFinishingScenario();
        using var renderer = new TestRenderer(new ApiClient(FailingClient(), JsonOptions));

        var ex = await Record.ExceptionAsync(() => renderer.Render(scenario.Process.Id));

        Assert.Null(ex);
        var toast = Assert.Single(renderer.Toasts.Toasts);
        Assert.Equal(ToastType.Danger, toast.Type);
        Assert.False((bool)GetField(renderer.Page!, "_loading")!);
    }

    [Fact]
    public async Task Save_AfterFailedLoad_ShowsErrorToastInsteadOfThrowing()
    {
        var scenario = await BuildWidgetFinishingScenario();
        using var renderer = new TestRenderer(new ApiClient(FailingClient(), JsonOptions));
        var page = await renderer.Render(scenario.Process.Id);
        SetField(page, "_code", "NEW-CODE");
        SetField(page, "_name", "New name");
        var toastsBefore = renderer.Toasts.Toasts.Count;

        var ex = await Record.ExceptionAsync(() => renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "Save")));

        Assert.Null(ex);
        var toast = Assert.Single(renderer.Toasts.Toasts.Skip(toastsBefore));
        Assert.Equal(ToastType.Danger, toast.Type);
    }

    [Fact]
    public async Task RunValidation_ApiFailure_ShowsErrorToastInsteadOfThrowing()
    {
        var scenario = await BuildWidgetFinishingScenario();
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.Render(scenario.Process.Id);
        // The page loaded normally; make the Validate request fail.
        SetApi(page, new ApiClient(FailingClient(), JsonOptions));

        var ex = await Record.ExceptionAsync(() => renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "RunValidation")));

        Assert.Null(ex);
        var toast = Assert.Single(renderer.Toasts.Toasts);
        Assert.Equal(ToastType.Danger, toast.Type);
        Assert.Null(GetField(page, "_validation"));
    }

    [Fact]
    public async Task InitializeAndValidate_ApiAvailable_LoadStepsAndValidationWithoutToast()
    {
        var scenario = await BuildWidgetFinishingScenario();
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.Render(scenario.Process.Id);

        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "RunValidation"));

        Assert.Empty(renderer.Toasts.Toasts);
        Assert.False((bool)GetField(page, "_loading")!);
        Assert.Equal(scenario.Process.Code, GetField(page, "_code"));
        var steps = (System.Collections.IList)GetField(page, "_steps")!;
        Assert.Equal(2, steps.Count);
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

    private static void SetApi(IComponent page, ApiClient api) =>
        page.GetType().GetProperty("Api", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, api);

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

        public IComponent? Page { get; private set; }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public async Task<IComponent> Render(Guid processId)
        {
            Page = InstantiateComponent(typeof(ProcessBuilder));
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = (Guid?)processId });
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(AssignRootComponentId(Page), parameters));
            return Page;
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
