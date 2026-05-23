namespace ProcessManager.Domain.Services;

/// <summary>
/// How material-flow source locations are chosen for each workstation input.
/// </summary>
public enum MaterialFlowMode
{
    /// <summary>
    /// Live view — route each required Kind from the nearest inventory location
    /// that actually has on-hand stock of it right now. Reflects today's reality.
    /// </summary>
    Live,

    /// <summary>
    /// Designed view — route each required Kind from the inventory location(s)
    /// explicitly designated to supply it, regardless of current stock. Reflects
    /// the intended layout. If several locations are designated for the same
    /// Kind, the nearest is chosen.
    /// </summary>
    Designed
}

/// <summary>
/// Pure-domain material-flow analyzer for the Factory Design Suite (Phase 37).
/// Given workstation placements (with the Kinds their assigned processes consume),
/// inventory location placements, and a mode, it returns the flow lines that
/// should be drawn from sources to consumers, the unresolved requirements, and a
/// total-travel "spaghetti" score for comparing layouts.
///
/// No DB, no DTOs, no entities — the controller projects its data into these
/// records so the algorithm can be unit-tested in isolation.
/// </summary>
public static class MaterialFlowAnalyzer
{
    public static MaterialFlowResult Analyze(
        IReadOnlyCollection<FlowWorkstation> workstations,
        IReadOnlyCollection<FlowInventoryLocation> locations,
        IReadOnlyDictionary<Guid, FlowKind> kindLookup,
        MaterialFlowOptions options)
    {
        if (workstations is null) throw new ArgumentNullException(nameof(workstations));
        if (locations is null) throw new ArgumentNullException(nameof(locations));
        if (kindLookup is null) throw new ArgumentNullException(nameof(kindLookup));
        if (options is null) throw new ArgumentNullException(nameof(options));

        var flows = new List<FlowLine>();
        var unresolved = new List<UnresolvedMaterial>();

        foreach (var ws in workstations)
        {
            foreach (var kindId in ws.RequiredKindIds.Distinct())
            {
                if (!kindLookup.TryGetValue(kindId, out var kind))
                    continue; // unknown kind — skip silently

                var candidate = SelectSource(ws, kindId, locations, options);

                if (candidate is null)
                {
                    unresolved.Add(new UnresolvedMaterial(
                        ws.PlacementId, kindId, kind.Code, kind.Name,
                        options.Mode == MaterialFlowMode.Designed
                            ? "no_designated_source"
                            : "no_inventory_location_with_stock"));
                    continue;
                }

                var (loc, distance, onHand) = candidate.Value;
                flows.Add(new FlowLine(
                    ws.PlacementId, ws.Label,
                    kindId, kind.Code, kind.Name,
                    loc.PlacementId, loc.Label, loc.LocationCode,
                    onHand,
                    Math.Round(distance, 1),
                    Math.Round(distance / 1000.0, 3),
                    new FlowPoint(loc.CenterX, loc.CenterY),
                    new FlowPoint(ws.CenterX, ws.CenterY)));
            }
        }

        var totalTravel = Math.Round(flows.Sum(f => f.DistanceMm), 1);
        return new MaterialFlowResult(flows, unresolved, totalTravel);
    }

    private static (FlowInventoryLocation Loc, double Distance, int OnHand)? SelectSource(
        FlowWorkstation ws,
        Guid kindId,
        IReadOnlyCollection<FlowInventoryLocation> locations,
        MaterialFlowOptions options)
    {
        IEnumerable<FlowInventoryLocation> eligible = options.Mode switch
        {
            // Designed: only locations explicitly designated for this Kind.
            MaterialFlowMode.Designed =>
                locations.Where(l => l.DesignatedKindIds.Contains(kindId)),

            // Live: locations holding stock — unless empties are explicitly included.
            _ => options.IncludeEmptyLocations
                ? locations
                : locations.Where(l => l.OnHandByKind.TryGetValue(kindId, out var q) && q > 0),
        };

        (FlowInventoryLocation Loc, double Distance, int OnHand)? best = null;

        foreach (var loc in eligible)
        {
            var dx = ws.CenterX - loc.CenterX;
            var dy = ws.CenterY - loc.CenterY;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            var onHand = loc.OnHandByKind.TryGetValue(kindId, out var q) ? q : 0;

            if (best is null || dist < best.Value.Distance)
                best = (loc, dist, onHand);
        }

        return best;
    }
}

// ──────────── Inputs ────────────

public sealed record FlowWorkstation(
    string PlacementId,
    string Label,
    double CenterX,
    double CenterY,
    IReadOnlyList<Guid> RequiredKindIds);

public sealed record FlowInventoryLocation(
    string PlacementId,
    string Label,
    string LocationCode,
    double CenterX,
    double CenterY,
    IReadOnlyDictionary<Guid, int> OnHandByKind,
    IReadOnlySet<Guid> DesignatedKindIds);

public sealed record FlowKind(Guid Id, string Code, string Name);

public sealed record MaterialFlowOptions(
    MaterialFlowMode Mode,
    bool IncludeEmptyLocations = false);

// ──────────── Outputs ────────────

public sealed record FlowPoint(double X, double Y);

public sealed record FlowLine(
    string WorkstationPlacementId,
    string WorkstationLabel,
    Guid KindId,
    string KindCode,
    string KindName,
    string SourceLocationPlacementId,
    string SourceLocationLabel,
    string SourceLocationCode,
    int OnHandQuantity,
    double DistanceMm,
    double DistanceM,
    FlowPoint FromPoint,
    FlowPoint ToPoint);

public sealed record UnresolvedMaterial(
    string WorkstationPlacementId,
    Guid KindId,
    string KindCode,
    string KindName,
    string Reason);

public sealed record MaterialFlowResult(
    IReadOnlyList<FlowLine> Flows,
    IReadOnlyList<UnresolvedMaterial> Unresolved,
    double TotalTravelDistanceMm);
