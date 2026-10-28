extern alias Web;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ToastContainer = Web::ProcessManager.Web.Components.Shared.ToastContainer;
using ToastService = Web::ProcessManager.Web.Services.ToastService;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// ToastContainer starts a timer per toast and dismisses the toast when it fires.
/// These tests host it in a minimal Blazor renderer.
/// </summary>
public class ToastContainerTimerTests
{
    [Fact]
    public async Task ExpiredToast_IsDismissedOnRendererDispatcher()
    {
        using var renderer = new TestRenderer();
        await renderer.RenderToastContainer();

        // ToastService and ToastContainer state isn't thread-safe, so a timer-driven
        // dismiss has to run on the circuit's dispatcher like every other change.
        var dismissed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        renderer.Toasts.OnChange += () =>
        {
            if (renderer.Toasts.Toasts.Count == 0)
                dismissed.TrySetResult(renderer.Dispatcher.CheckAccess());
        };

        await renderer.Dispatcher.InvokeAsync(() => renderer.Toasts.ShowInfo("Saved", durationMs: 20));

        var onDispatcher = await dismissed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(onDispatcher);
    }

    [Fact]
    public async Task DisposedContainer_DoesNotDismissToast()
    {
        var renderer = new TestRenderer();
        await renderer.RenderToastContainer();

        await renderer.Dispatcher.InvokeAsync(() => renderer.Toasts.ShowInfo("Saved", durationMs: 100));
        await renderer.Dispatcher.InvokeAsync(renderer.Dispose);
        await Task.Delay(300);

        Assert.Single(renderer.Toasts.Toasts);
    }

    private sealed class TestRenderer : Renderer
    {
        public TestRenderer() : this(new ServiceCollection().AddSingleton<ToastService>().BuildServiceProvider()) { }

        private TestRenderer(IServiceProvider services) : base(services, NullLoggerFactory.Instance)
        {
            Toasts = services.GetRequiredService<ToastService>();
        }

        public ToastService Toasts { get; }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public Task RenderToastContainer() =>
            Dispatcher.InvokeAsync(() => RenderRootComponentAsync(AssignRootComponentId(InstantiateComponent(typeof(ToastContainer)))));

        protected override void HandleException(Exception exception) => throw exception;

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
}
