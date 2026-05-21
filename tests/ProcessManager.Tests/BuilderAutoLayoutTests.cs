using ProcessManager.Domain.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Phase 36.3 (T3.2) — unit tests for the pure auto-layout algorithm used
/// by the Process and Workflow builders' "Tidy" toolbar button.
/// </summary>
public class BuilderAutoLayoutTests
{
    [Fact]
    public void Empty_graph_returns_empty_result()
    {
        var result = BuilderAutoLayout.Compute(
            Array.Empty<LayoutNode>(),
            Array.Empty<LayoutEdge>());

        Assert.Empty(result);
    }

    [Fact]
    public void Single_node_lands_at_origin()
    {
        var n = new LayoutNode(Guid.NewGuid());
        var opts = LayoutOptions.Default;

        var result = BuilderAutoLayout.Compute(new[] { n }, Array.Empty<LayoutEdge>(), opts);

        Assert.Single(result);
        var pos = result[n.Id];
        Assert.Equal(opts.OriginX, pos.X);
        Assert.Equal(opts.OriginY, pos.Y);
    }

    [Fact]
    public void Linear_chain_assigns_one_layer_per_node()
    {
        var a = new LayoutNode(Guid.NewGuid());
        var b = new LayoutNode(Guid.NewGuid());
        var c = new LayoutNode(Guid.NewGuid());

        var result = BuilderAutoLayout.Compute(
            new[] { a, b, c },
            new[] { new LayoutEdge(a.Id, b.Id), new LayoutEdge(b.Id, c.Id) });

        // X increases by LayerSpacingX between consecutive nodes
        Assert.True(result[a.Id].X < result[b.Id].X);
        Assert.True(result[b.Id].X < result[c.Id].X);

        var dx1 = result[b.Id].X - result[a.Id].X;
        var dx2 = result[c.Id].X - result[b.Id].X;
        Assert.Equal(dx1, dx2);
    }

    [Fact]
    public void Diamond_graph_places_sibling_branches_in_same_layer()
    {
        //      a
        //     / \
        //    b   c
        //     \ /
        //      d
        var a = new LayoutNode(Guid.NewGuid());
        var b = new LayoutNode(Guid.NewGuid(), OriginalY: 100);
        var c = new LayoutNode(Guid.NewGuid(), OriginalY: 200);
        var d = new LayoutNode(Guid.NewGuid());

        var result = BuilderAutoLayout.Compute(
            new[] { a, b, c, d },
            new[]
            {
                new LayoutEdge(a.Id, b.Id),
                new LayoutEdge(a.Id, c.Id),
                new LayoutEdge(b.Id, d.Id),
                new LayoutEdge(c.Id, d.Id),
            });

        // b and c share the same layer (X)
        Assert.Equal(result[b.Id].X, result[c.Id].X);
        // d is in a strictly later layer than b/c (longest path = 2)
        Assert.True(result[d.Id].X > result[b.Id].X);
        // b sorted before c because of OriginalY
        Assert.True(result[b.Id].Y < result[c.Id].Y);
    }

    [Fact]
    public void Longest_path_wins_when_multiple_routes_exist()
    {
        // a → b → c
        //  \____ ___↗
        //       ↘
        // Direct a→c plus longer a→b→c. c should be at layer 2 (longest path).
        var a = new LayoutNode(Guid.NewGuid());
        var b = new LayoutNode(Guid.NewGuid());
        var c = new LayoutNode(Guid.NewGuid());

        var result = BuilderAutoLayout.Compute(
            new[] { a, b, c },
            new[]
            {
                new LayoutEdge(a.Id, b.Id),
                new LayoutEdge(b.Id, c.Id),
                new LayoutEdge(a.Id, c.Id),
            });

        // c should be in layer 2 (longest), not layer 1 (direct edge)
        var dx = result[c.Id].X - result[a.Id].X;
        Assert.Equal(2 * LayoutOptions.Default.LayerSpacingX, dx);
    }

    [Fact]
    public void Disconnected_nodes_get_placed_in_first_layer()
    {
        var a = new LayoutNode(Guid.NewGuid()); // entry
        var b = new LayoutNode(Guid.NewGuid()); // disconnected island

        var result = BuilderAutoLayout.Compute(
            new[] { a, b },
            Array.Empty<LayoutEdge>());

        Assert.Equal(result[a.Id].X, result[b.Id].X);
    }

    [Fact]
    public void Cycle_does_not_hang()
    {
        // a → b → a forms a cycle with no source. Algorithm must still terminate.
        var a = new LayoutNode(Guid.NewGuid());
        var b = new LayoutNode(Guid.NewGuid());

        var result = BuilderAutoLayout.Compute(
            new[] { a, b },
            new[]
            {
                new LayoutEdge(a.Id, b.Id),
                new LayoutEdge(b.Id, a.Id),
            });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Dangling_edges_referencing_unknown_nodes_are_ignored()
    {
        var a = new LayoutNode(Guid.NewGuid());

        // Edge with a missing endpoint — shouldn't crash, just ignored.
        var result = BuilderAutoLayout.Compute(
            new[] { a },
            new[] { new LayoutEdge(a.Id, Guid.NewGuid()) });

        Assert.Single(result);
    }

    [Fact]
    public void Duplicate_node_ids_throw()
    {
        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => BuilderAutoLayout.Compute(
            new[] { new LayoutNode(id), new LayoutNode(id) },
            Array.Empty<LayoutEdge>()));
    }

    [Fact]
    public void Null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BuilderAutoLayout.Compute(null!, Array.Empty<LayoutEdge>()));
        Assert.Throws<ArgumentNullException>(() =>
            BuilderAutoLayout.Compute(Array.Empty<LayoutNode>(), null!));
    }
}
