namespace ProcessManager.Domain.Services;

/// <summary>
/// Pure layered (Sugiyama-style) auto-layout for a directed graph of builder
/// nodes. Used by the Process and Workflow builders' "Tidy" toolbar button.
///
/// Algorithm:
///   1. Determine each node's <i>layer</i> by longest path from any source
///      (entry / no-incoming) node. Cycles fall back to BFS order so the
///      algorithm always terminates.
///   2. Within each layer, sort nodes by their original Y to preserve user
///      intent when re-tidying an already-arranged graph.
///   3. Distribute nodes evenly along X (one column per layer) and Y
///      (centered within the layer's column).
///
/// No DB, no UI dependency — callers translate the resulting positions into
/// their own diagram model.
/// </summary>
public static class BuilderAutoLayout
{
    /// <summary>
    /// Compute new positions for the given nodes. Returned dictionary is keyed
    /// by <see cref="LayoutNode.Id"/>; every input node has exactly one entry.
    /// </summary>
    /// <param name="nodes">Nodes to lay out. Must have unique Ids.</param>
    /// <param name="edges">Directed edges referencing node Ids.</param>
    /// <param name="options">Spacing options. Defaults give a clean grid.</param>
    public static IReadOnlyDictionary<Guid, LayoutPosition> Compute(
        IReadOnlyCollection<LayoutNode> nodes,
        IReadOnlyCollection<LayoutEdge> edges,
        LayoutOptions? options = null)
    {
        if (nodes is null) throw new ArgumentNullException(nameof(nodes));
        if (edges is null) throw new ArgumentNullException(nameof(edges));

        var opts = options ?? LayoutOptions.Default;
        var result = new Dictionary<Guid, LayoutPosition>(nodes.Count);
        if (nodes.Count == 0) return result;

        var nodeIds = nodes.Select(n => n.Id).ToHashSet();
        if (nodeIds.Count != nodes.Count)
            throw new ArgumentException("Node Ids must be unique.", nameof(nodes));

        // Filter edges to those whose endpoints exist in the node set —
        // ignore dangling refs rather than crash.
        var validEdges = edges
            .Where(e => nodeIds.Contains(e.From) && nodeIds.Contains(e.To))
            .ToList();

        // ── Layer assignment via longest path from source nodes ──
        var incoming = nodeIds.ToDictionary(id => id, _ => new List<Guid>());
        var outgoing = nodeIds.ToDictionary(id => id, _ => new List<Guid>());
        foreach (var e in validEdges)
        {
            outgoing[e.From].Add(e.To);
            incoming[e.To].Add(e.From);
        }

        var layer = nodeIds.ToDictionary(id => id, _ => 0);
        var visited = new HashSet<Guid>();

        // Start from nodes with no incoming edges (sources). If none exist
        // (a pure cycle), fall back to the first node by stable id ordering.
        var sources = nodeIds.Where(id => incoming[id].Count == 0).ToList();
        if (sources.Count == 0)
            sources = new List<Guid> { nodeIds.OrderBy(g => g).First() };

        // BFS, taking the max layer seen so far for each node.
        var queue = new Queue<Guid>(sources);
        foreach (var s in sources) visited.Add(s);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var next in outgoing[cur])
            {
                var candidate = layer[cur] + 1;
                if (candidate > layer[next]) layer[next] = candidate;
                if (visited.Add(next)) queue.Enqueue(next);
            }
        }

        // Any nodes still unvisited (disconnected components) get their own
        // layer 0 — they'll appear as a separate row of the grid.
        foreach (var id in nodeIds)
        {
            if (!visited.Contains(id)) layer[id] = 0;
        }

        // ── Distribute within layers ──
        var byLayer = layer
            .GroupBy(kv => kv.Value)
            .OrderBy(g => g.Key)
            .ToList();

        var originalY = nodes.ToDictionary(n => n.Id, n => n.OriginalY);

        foreach (var group in byLayer)
        {
            var ordered = group
                .OrderBy(kv => originalY[kv.Key])
                .ThenBy(kv => kv.Key) // stable tiebreak
                .Select(kv => kv.Key)
                .ToList();

            var x = opts.OriginX + group.Key * opts.LayerSpacingX;

            // Center the column vertically around OriginY.
            var totalHeight = (ordered.Count - 1) * opts.NodeSpacingY;
            var startY = opts.OriginY - totalHeight / 2.0;

            for (var i = 0; i < ordered.Count; i++)
            {
                var y = startY + i * opts.NodeSpacingY;
                result[ordered[i]] = new LayoutPosition(x, y);
            }
        }

        return result;
    }
}

public sealed record LayoutNode(Guid Id, double OriginalY = 0);

public sealed record LayoutEdge(Guid From, Guid To);

public sealed record LayoutPosition(double X, double Y);

public sealed record LayoutOptions(
    double OriginX = 80,
    double OriginY = 240,
    double LayerSpacingX = 240,
    double NodeSpacingY = 160)
{
    public static readonly LayoutOptions Default = new();
}
