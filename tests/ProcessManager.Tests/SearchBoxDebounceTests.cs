extern alias Web;

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SearchBox = Web::ProcessManager.Web.Components.Shared.SearchBox;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// The SearchBox debounces typing with a timer and reports the value through
/// ValueChanged 300 ms later. These tests host it in a minimal Blazor renderer.
/// </summary>
public class SearchBoxDebounceTests
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    [Fact]
    public async Task Typing_ReportsLastValueOnceAfterDebounce()
    {
        var calls = new List<string>();
        var reported = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var renderer = new TestRenderer();
        var box = await renderer.RenderSearchBox(v => { calls.Add(v); reported.TrySetResult(v); });

        await renderer.Type(box, "Wid");
        await renderer.Type(box, "Widget");

        var value = await reported.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(Debounce * 2);

        Assert.Equal("Widget", value);
        Assert.Equal(["Widget"], calls);
    }

    [Fact]
    public async Task DisposingWithinDebounce_DoesNotReportValue()
    {
        var calls = 0;
        var renderer = new TestRenderer();
        var box = await renderer.RenderSearchBox(_ => Interlocked.Increment(ref calls));

        await renderer.Type(box, "Widget");
        // Navigating away disposes the page's components before the timer fires.
        await renderer.Dispatcher.InvokeAsync(renderer.Dispose);
        await Task.Delay(Debounce * 3);

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ValueChangedException_IsReportedToRenderer()
    {
        using var renderer = new TestRenderer();
        var box = await renderer.RenderSearchBox(_ => throw new InvalidOperationException("search failed"));

        await renderer.Type(box, "Widget");

        var ex = await renderer.UnhandledException.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("search failed", ex.Message);
    }

    private sealed class TestRenderer : Renderer
    {
        private static readonly MethodInfo OnInput =
            typeof(SearchBox).GetMethod("OnInput", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public TestRenderer()
            : base(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance) { }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public TaskCompletionSource<Exception> UnhandledException { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<SearchBox> RenderSearchBox(Action<string> onValueChanged)
        {
            var box = new SearchBox();
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(SearchBox.ValueChanged)] = EventCallback.Factory.Create(this, onValueChanged)
            });
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(AssignRootComponentId(box), parameters));
            return box;
        }

        public Task Type(SearchBox box, string text) =>
            Dispatcher.InvokeAsync(() => OnInput.Invoke(box, [new ChangeEventArgs { Value = text }]));

        protected override void HandleException(Exception exception) =>
            UnhandledException.TrySetResult(exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
}
