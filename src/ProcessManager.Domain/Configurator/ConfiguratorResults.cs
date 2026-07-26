namespace ProcessManager.Domain.Configurator;

/// <summary>
/// The single source of truth for a configuration: which option(s) are chosen in
/// each group, plus the numeric attribute values. Single-select groups hold 0..1
/// option ids; multi-select groups hold any number.
/// </summary>
public sealed class ConfigSelection
{
    /// <summary>groupId → chosen option ids (single groups carry at most one).</summary>
    public Dictionary<string, List<string>> Choices { get; set; } = new();
    public Dictionary<string, double> Attrs { get; set; } = new();

    public ConfigSelection Clone()
    {
        var c = new ConfigSelection { Attrs = new Dictionary<string, double>(Attrs) };
        foreach (var kv in Choices) c.Choices[kv.Key] = new List<string>(kv.Value);
        return c;
    }

    public List<string> Of(string groupId) =>
        Choices.TryGetValue(groupId, out var v) ? v : (Choices[groupId] = new List<string>());

    /// <summary>Single-select convenience: the one chosen id (or null).</summary>
    public string? Single(string groupId)
    {
        var list = Choices.TryGetValue(groupId, out var v) ? v : null;
        return list is { Count: > 0 } ? list[0] : null;
    }
}

public sealed class ReconcileResult
{
    public ConfigSelection Selection { get; set; } = new();
    /// <summary>optionId → reason it is disabled (the exact rule message).</summary>
    public Dictionary<string, string> Disabled { get; set; } = new();
    public List<Violation> Violations { get; set; } = new();
    /// <summary>Auto-corrections that were applied (human-readable).</summary>
    public List<string> Notes { get; set; } = new();
}

public sealed class Violation
{
    public string RuleId { get; set; } = "";
    public string Msg { get; set; } = "";
    public string FixLabel { get; set; } = "";
    /// <summary>Applies the one-click fix to a selection, returning the corrected copy.</summary>
    public Func<ConfigSelection, ConfigSelection>? Fix { get; set; }
}

public sealed class ResolvedBomLine
{
    public string System { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Qty { get; set; }
    public string? QtyExpr { get; set; }
    public double Price { get; set; }
    public string Source { get; set; } = "";
    public List<ResolvedBomLine> Children { get; set; } = new();

    public double LineTotal => Price * Qty + Children.Sum(c => c.Price * c.Qty);
}

public sealed class BomSystem
{
    public string Name { get; set; } = "";
    public List<ResolvedBomLine> Assemblies { get; set; } = new();
    public double Total { get; set; }
}

public sealed class BomResult
{
    public List<BomSystem> Systems { get; set; } = new();
    public int PartCount { get; set; }
    public double Cost { get; set; }
}

public sealed class PriceLine
{
    public string Label { get; set; } = "";
    public double Amount { get; set; }
    public bool Base { get; set; }
    public string? Group { get; set; }
    public bool Formula { get; set; }
}

public sealed class PriceResult
{
    public List<PriceLine> Lines { get; set; } = new();
    public double Total { get; set; }
}

public sealed class CalcValue
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Expr { get; set; } = "";
    public string Hint { get; set; } = "";
    public double Value { get; set; }
}

public sealed class AttrBounds
{
    public double Min { get; set; }
    public double Max { get; set; }
    public List<string> FromRules { get; set; } = new();
    public bool Clamped { get; set; }
}

public sealed class ConfigStatus
{
    public int Done { get; set; }
    public int Total { get; set; }
    public bool Complete { get; set; }
}

public sealed class DependencyEdge
{
    public string Rule { get; set; } = "";
    public string Type { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Msg { get; set; } = "";
}

// ── Serialisable export (behind a released work order) ────────────────────────
public sealed class ExportSpec
{
    public ExportMeta Meta { get; set; } = new();
    public List<ExportAttribute> Attributes { get; set; } = new();
    public List<CalcValue> Calculated { get; set; } = new();
    public List<ExportSelection> Selections { get; set; } = new();
    public ExportBom Bom { get; set; } = new();
    public ExportPrice Price { get; set; } = new();
    public List<ExportRouting> Routing { get; set; } = new();
    public List<ExportRule> Rules { get; set; } = new();
}

public sealed class ExportMeta
{
    public PlatformMeta Platform { get; set; } = new();
    public double BasePrice { get; set; }
    public string GeneratedAt { get; set; } = "";
    public int OrderQty { get; set; }
    public ComplianceMeta Compliance { get; set; } = new();
    public string ConfigHash { get; set; } = "";
    public string DocNo { get; set; } = "";
}

public sealed class ExportAttribute
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public string Unit { get; set; } = "";
    public string Kind { get; set; } = "";
    public ExportBounds? Bounds { get; set; }
}

public sealed class ExportBounds
{
    public double Min { get; set; }
    public double Max { get; set; }
    public List<string> ClampedBy { get; set; } = new();
}

public sealed class ExportSelection
{
    public string Group { get; set; } = "";
    public string GroupId { get; set; } = "";
    public string OptionId { get; set; } = "";
    public string Name { get; set; } = "";
    public double UnitPrice { get; set; }
    public string? PriceFormula { get; set; }
}

public sealed class ExportBom
{
    public List<ExportBomRow> Rows { get; set; } = new();
    public int PartCount { get; set; }
    public double Cost { get; set; }
}

public sealed class ExportBomRow
{
    public int Level { get; set; }
    public string System { get; set; } = "";
    public string Source { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Qty { get; set; }
    public string? QtyFormula { get; set; }
    public double UnitPrice { get; set; }
    public double Ext { get; set; }
}

public sealed class ExportPrice
{
    public List<ExportPriceLine> Lines { get; set; } = new();
    public double PerUnit { get; set; }
    public int OrderQty { get; set; }
    public double OrderTotal { get; set; }
}

public sealed class ExportPriceLine
{
    public string Label { get; set; } = "";
    public double Amount { get; set; }
    public bool Base { get; set; }
    public string? Group { get; set; }
    public string? PriceFormula { get; set; }
}

public sealed class ExportRouting
{
    public int Step { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class ExportRule
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Msg { get; set; } = "";
    public List<string>? When { get; set; }
    public List<string>? Then { get; set; }
    public string? Expr { get; set; }
    public List<string>? Refs { get; set; }
}
