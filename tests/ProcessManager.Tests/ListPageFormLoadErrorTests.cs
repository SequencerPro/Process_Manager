extern alias Web;

using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using ProcessManager.Api.DTOs;
using Web::ProcessManager.Web.Services;
using BatchList = Web::ProcessManager.Web.Components.Pages.Batches.BatchList;
using ItemList = Web::ProcessManager.Web.Components.Pages.Items.ItemList;
using JobList = Web::ProcessManager.Web.Components.Pages.Jobs.JobList;
using StepTemplateList = Web::ProcessManager.Web.Components.Pages.StepTemplates.StepTemplateList;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The list pages' "New" forms load their dropdowns (Jobs, Kinds, Processes, a Kind's Grades) from the API.
/// A failed request must show an error toast. An exception escaping a Blazor event handler
/// tears down the user's whole circuit instead.
/// </summary>
public class ListPageFormLoadErrorTests : IntegrationTestBase
{
    public ListPageFormLoadErrorTests(TestWebApplicationFactory factory) : base(factory) { }

    public static TheoryData<Type, string, string?> FormLoadHandlers => new()
    {
        { typeof(BatchList), "ShowCreateForm", null },
        { typeof(BatchList), "OnFormKindChanged", "_formKindId" },
        { typeof(ItemList), "ShowCreateForm", null },
        { typeof(ItemList), "OnFormKindChanged", "_formKindId" },
        { typeof(JobList), "ShowCreate", null },
        { typeof(StepTemplateList), "ShowCreateForm", null },
        { typeof(StepTemplateList), "OnInlineKindChanged", "_ipKindId" },
    };

    [Theory]
    [MemberData(nameof(FormLoadHandlers))]
    public async Task FormLoad_ApiFailure_ShowsErrorToastInsteadOfThrowing(Type pageType, string handler, string? kindIdField)
    {
        using var renderer = new TestRenderer(new ApiClient(FailingClient(), JsonOptions));
        var page = await renderer.Render(pageType);
        if (kindIdField != null) SetField(page, kindIdField, Guid.NewGuid().ToString());
        var toastsBefore = renderer.Toasts.Toasts.Count;

        var ex = await Record.ExceptionAsync(() => renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, handler)));

        Assert.Null(ex);
        var toast = Assert.Single(renderer.Toasts.Toasts.Skip(toastsBefore));
        Assert.Equal(ToastType.Danger, toast.Type);
    }

    [Fact]
    public async Task ItemListForm_ApiAvailable_LoadsKindsAndGradesWithoutToast()
    {
        var kind = await CreateKind($"FORM-{Guid.NewGuid().ToString()[..6]}", "Form Kind");
        var grade = await CreateGrade(kind.Id, "RAW", "Raw", isDefault: true);
        using var renderer = new TestRenderer(new ApiClient(Client, JsonOptions));
        var page = await renderer.Render(typeof(ItemList));

        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "ShowCreateForm"));
        SetField(page, "_formKindId", kind.Id.ToString());
        await renderer.Dispatcher.InvokeAsync(() => InvokeTask(page, "OnFormKindChanged"));

        Assert.Empty(renderer.Toasts.Toasts);
        var kinds = (PaginatedResponse<KindResponseDto>)GetField(page, "_kinds")!;
        Assert.Contains(kinds.Items, k => k.Id == kind.Id);
        var grades = (List<GradeResponseDto>)GetField(page, "_formGrades")!;
        Assert.Equal(new[] { grade.Id }, grades.Select(g => g.Id));
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

        public async Task<IComponent> Render(Type pageType)
        {
            var page = InstantiateComponent(pageType);
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(AssignRootComponentId(page), ParameterView.Empty));
            return page;
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
