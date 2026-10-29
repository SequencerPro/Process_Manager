extern alias Web;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Pager = Web::ProcessManager.Web.Components.Shared.Pager;

// Renderer and RenderBatch are framework-internal types; this is a deliberately minimal test host.
#pragma warning disable BL0006

namespace ProcessManager.Tests;

/// <summary>
/// Pager's previous/next links are only styled as disabled at the ends of the range.
/// Bootstrap's pointer-events rule blocks mouse clicks, but a focused link still
/// raises onclick from the keyboard, so the component itself must stay in range.
/// </summary>
public class PagerBoundsTests
{
    [Fact]
    public async Task PreviousOnFirstPage_DoesNotRequestPageZero()
    {
        var requested = await ClickAsync(page: 1, totalCount: 60, link: Link.Previous);

        Assert.Empty(requested);
    }

    [Fact]
    public async Task NextOnLastPage_DoesNotRequestPagePastEnd()
    {
        var requested = await ClickAsync(page: 3, totalCount: 60, link: Link.Next);

        Assert.Empty(requested);
    }

    [Theory]
    [InlineData(2, Link.Previous, 1)]
    [InlineData(2, Link.Next, 3)]
    public async Task PreviousAndNext_InsideRange_RequestAdjacentPage(int page, Link link, int expected)
    {
        var requested = await ClickAsync(page, totalCount: 60, link);

        Assert.Equal(new[] { expected }, requested);
    }

    public enum Link { Previous, Next }

    private static async Task<List<int>> ClickAsync(int page, int totalCount, Link link)
    {
        using var renderer = new TestRenderer();
        var requested = new List<int>();
        await renderer.RenderPager(new Dictionary<string, object?>
        {
            [nameof(Pager.Page)] = page,
            [nameof(Pager.PageSize)] = 25,
            [nameof(Pager.TotalCount)] = totalCount,
            [nameof(Pager.OnPageChanged)] = EventCallback.Factory.Create<int>(new object(), requested.Add),
        });

        // Previous is the first onclick handler rendered, Next is the last.
        var handlerId = link == Link.Previous ? renderer.ClickHandlers[0] : renderer.ClickHandlers[^1];
        await renderer.Dispatcher.InvokeAsync(() =>
            renderer.DispatchEventAsync(handlerId, new EventFieldInfo(), new MouseEventArgs()));

        return requested;
    }

    private sealed class TestRenderer : Renderer
    {
        public TestRenderer() : base(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance) { }

        public List<ulong> ClickHandlers { get; } = new();

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public Task RenderPager(IDictionary<string, object?> parameters) =>
            Dispatcher.InvokeAsync(() => RenderRootComponentAsync(
                AssignRootComponentId(InstantiateComponent(typeof(Pager))),
                ParameterView.FromDictionary(parameters)));

        protected override void HandleException(Exception exception) => throw exception;

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            for (var i = 0; i < renderBatch.ReferenceFrames.Count; i++)
            {
                var frame = renderBatch.ReferenceFrames.Array[i];
                if (frame.FrameType == RenderTreeFrameType.Attribute
                    && frame.AttributeName == "onclick"
                    && frame.AttributeEventHandlerId != 0)
                    ClickHandlers.Add(frame.AttributeEventHandlerId);
            }
            return Task.CompletedTask;
        }
    }
}
