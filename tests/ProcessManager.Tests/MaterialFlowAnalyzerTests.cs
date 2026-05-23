using ProcessManager.Domain.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Phase 37 — pure unit tests for MaterialFlowAnalyzer covering Live and Designed
/// modes. No DB.
/// </summary>
public class MaterialFlowAnalyzerTests
{
    private static readonly Guid KindA = Guid.NewGuid();
    private static readonly Guid KindB = Guid.NewGuid();

    private static IReadOnlyDictionary<Guid, FlowKind> Kinds() => new Dictionary<Guid, FlowKind>
    {
        [KindA] = new FlowKind(KindA, "A", "Kind A"),
        [KindB] = new FlowKind(KindB, "B", "Kind B"),
    };

    private static FlowWorkstation Ws(string id, double x, double y, params Guid[] kinds) =>
        new(id, id, x, y, kinds.ToList());

    private static FlowInventoryLocation Loc(
        string id, double x, double y,
        Dictionary<Guid, int>? onHand = null,
        IEnumerable<Guid>? designated = null) =>
        new(id, id, $"LOC-{id}", x, y,
            onHand ?? new Dictionary<Guid, int>(),
            (designated ?? Enumerable.Empty<Guid>()).ToHashSet());

    // ──────────── Live mode ────────────

    [Fact]
    public void Live_RoutesFromNearestLocationWithStock()
    {
        var ws = Ws("WS1", 0, 0, KindA);
        var near = Loc("NEAR", 100, 0, new Dictionary<Guid, int> { [KindA] = 5 });
        var far = Loc("FAR", 1000, 0, new Dictionary<Guid, int> { [KindA] = 99 });

        var result = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { near, far }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Live));

        var flow = Assert.Single(result.Flows);
        Assert.Equal("NEAR", flow.SourceLocationPlacementId);
        Assert.Equal(5, flow.OnHandQuantity);
        Assert.Equal(100, flow.DistanceMm);
        Assert.Empty(result.Unresolved);
    }

    [Fact]
    public void Live_SkipsEmptyLocations_UnlessIncludeEmpty()
    {
        var ws = Ws("WS1", 0, 0, KindA);
        var empty = Loc("EMPTY", 50, 0); // no stock
        var stocked = Loc("STOCK", 500, 0, new Dictionary<Guid, int> { [KindA] = 3 });

        // Default: empty is skipped, routes to the stocked (farther) location.
        var def = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { empty, stocked }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Live));
        Assert.Equal("STOCK", Assert.Single(def.Flows).SourceLocationPlacementId);

        // IncludeEmpty: nearest wins even with zero stock.
        var incl = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { empty, stocked }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Live, IncludeEmptyLocations: true));
        var flow = Assert.Single(incl.Flows);
        Assert.Equal("EMPTY", flow.SourceLocationPlacementId);
        Assert.Equal(0, flow.OnHandQuantity);
    }

    [Fact]
    public void Live_NoStockAnywhere_IsUnresolved()
    {
        var ws = Ws("WS1", 0, 0, KindA);
        var empty = Loc("EMPTY", 100, 0);

        var result = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { empty }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Live));

        Assert.Empty(result.Flows);
        var u = Assert.Single(result.Unresolved);
        Assert.Equal("no_inventory_location_with_stock", u.Reason);
        Assert.Equal(KindA, u.KindId);
    }

    // ──────────── Designed mode ────────────

    [Fact]
    public void Designed_RoutesFromDesignatedLocationRegardlessOfStock()
    {
        var ws = Ws("WS1", 0, 0, KindA);
        // The designated location has NO stock; a closer location has stock but
        // is not designated. Designed mode must pick the designated one.
        var designatedEmpty = Loc("DESIG", 800, 0, designated: new[] { KindA });
        var closerStocked = Loc("CLOSE", 50, 0, new Dictionary<Guid, int> { [KindA] = 10 });

        var result = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { designatedEmpty, closerStocked }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Designed));

        var flow = Assert.Single(result.Flows);
        Assert.Equal("DESIG", flow.SourceLocationPlacementId);
        Assert.Equal(0, flow.OnHandQuantity); // reports actual on-hand (none)
    }

    [Fact]
    public void Designed_NearestAmongMultipleDesignated()
    {
        var ws = Ws("WS1", 0, 0, KindA);
        var d1 = Loc("D1", 600, 0, designated: new[] { KindA });
        var d2 = Loc("D2", 200, 0, designated: new[] { KindA });

        var result = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { d1, d2 }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Designed));

        Assert.Equal("D2", Assert.Single(result.Flows).SourceLocationPlacementId);
    }

    [Fact]
    public void Designed_NoDesignation_IsUnresolved()
    {
        var ws = Ws("WS1", 0, 0, KindA);
        var stockedButNotDesignated = Loc("STOCK", 100, 0, new Dictionary<Guid, int> { [KindA] = 99 });

        var result = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { stockedButNotDesignated }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Designed));

        Assert.Empty(result.Flows);
        Assert.Equal("no_designated_source", Assert.Single(result.Unresolved).Reason);
    }

    // ──────────── General ────────────

    [Fact]
    public void MultipleKinds_ProduceMultipleFlows_AndSpaghettiScore()
    {
        var ws = Ws("WS1", 0, 0, KindA, KindB);
        var locA = Loc("LA", 300, 0, new Dictionary<Guid, int> { [KindA] = 1 });
        var locB = Loc("LB", 0, 400, new Dictionary<Guid, int> { [KindB] = 1 });

        var result = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { locA, locB }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Live));

        Assert.Equal(2, result.Flows.Count);
        // Spaghetti score = 300 + 400 = 700.
        Assert.Equal(700, result.TotalTravelDistanceMm);
    }

    [Fact]
    public void DuplicateRequiredKinds_AreDeduped()
    {
        var ws = Ws("WS1", 0, 0, KindA, KindA, KindA);
        var loc = Loc("L", 100, 0, new Dictionary<Guid, int> { [KindA] = 1 });

        var result = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { loc }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Live));

        Assert.Single(result.Flows);
    }

    [Fact]
    public void UnknownKind_IsSkippedSilently()
    {
        var ws = Ws("WS1", 0, 0, Guid.NewGuid()); // kind not in lookup
        var loc = Loc("L", 100, 0);

        var result = MaterialFlowAnalyzer.Analyze(
            new[] { ws }, new[] { loc }, Kinds(),
            new MaterialFlowOptions(MaterialFlowMode.Live));

        Assert.Empty(result.Flows);
        Assert.Empty(result.Unresolved);
    }

    [Fact]
    public void EmptyInputs_YieldEmptyResult()
    {
        var result = MaterialFlowAnalyzer.Analyze(
            Array.Empty<FlowWorkstation>(), Array.Empty<FlowInventoryLocation>(),
            Kinds(), new MaterialFlowOptions(MaterialFlowMode.Live));

        Assert.Empty(result.Flows);
        Assert.Empty(result.Unresolved);
        Assert.Equal(0, result.TotalTravelDistanceMm);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var opts = new MaterialFlowOptions(MaterialFlowMode.Live);
        Assert.Throws<ArgumentNullException>(() => MaterialFlowAnalyzer.Analyze(null!, Array.Empty<FlowInventoryLocation>(), Kinds(), opts));
        Assert.Throws<ArgumentNullException>(() => MaterialFlowAnalyzer.Analyze(Array.Empty<FlowWorkstation>(), null!, Kinds(), opts));
        Assert.Throws<ArgumentNullException>(() => MaterialFlowAnalyzer.Analyze(Array.Empty<FlowWorkstation>(), Array.Empty<FlowInventoryLocation>(), null!, opts));
        Assert.Throws<ArgumentNullException>(() => MaterialFlowAnalyzer.Analyze(Array.Empty<FlowWorkstation>(), Array.Empty<FlowInventoryLocation>(), Kinds(), null!));
    }
}
