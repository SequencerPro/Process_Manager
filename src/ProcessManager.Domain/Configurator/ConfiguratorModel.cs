namespace ProcessManager.Domain.Configurator;

/// <summary>
/// Data model for the visual BoM / product configurator (ported from the
/// SequencerPro design handoff <c>data.js</c>). A <see cref="ProductPlatform"/>
/// is the entire, data-driven product definition: platform meta, ISO compliance
/// block, base BoM, numeric &amp; calculated attributes, option groups (each
/// option carries its own BoM assemblies, routing operations and price/priceExpr)
/// and the declarative rule set.
///
/// The schema is deliberately product-agnostic — any industry's product tree can
/// be expressed with it. Options and BoM assemblies may reference the Process
/// Manager item master via <c>KindId</c>/<c>GradeId</c> so authored structures
/// stay linked to real parts. Properties are mutable because the author views
/// edit the definition in place; revision control happens at the persistence
/// layer (immutable saved revisions), not on these objects.
/// </summary>
public sealed class ProductPlatform
{
    public PlatformMeta Platform { get; set; } = new();
    public ComplianceMeta Compliance { get; set; } = new();
    public List<BomAssembly> BaseBom { get; set; } = new();
    public List<RoutingOp> BaseOps { get; set; } = new();
    public List<ConfigAttribute> Attributes { get; set; } = new();
    public List<CalcAttribute> Calc { get; set; } = new();
    public List<OptionGroup> Groups { get; set; } = new();
    public List<ConfigRule> Rules { get; set; } = new();

    /// <summary>
    /// A minimal, empty platform for authoring a brand-new product from scratch.
    /// Ships with just an order-quantity attribute (used by the order rollup)
    /// and a generic compliance block; groups, options, parts and rules are all
    /// added by the author.
    /// </summary>
    public static ProductPlatform CreateBlank(string code, string name, string tagline) => new()
    {
        Platform = new PlatformMeta { Code = code, Name = name, Tagline = tagline, BasePrice = 0 },
        Compliance = new ComplianceMeta
        {
            FormNo = "QF-BOM-001",
            Revision = "A",
            Standard = "ISO 9001:2015",
            Clauses = "§8.5.1 Production control · §8.5.2 Identification & traceability · ISO 10007 Configuration management",
            Classification = "Controlled — Internal",
            Owner = "Manufacturing Engineering",
            Retention = "Retain 7 years",
            Approvals = new()
            {
                new() { Role = "Prepared by", Title = "Configuration Engineer" },
                new() { Role = "Reviewed by", Title = "Manufacturing Engineer" },
                new() { Role = "Approved by", Title = "Engineering Manager" },
                new() { Role = "Quality release", Title = "QA Inspector" },
            },
            RevisionHistory = new(),
        },
        Attributes = new()
        {
            new() { Id = "orderQty", Name = "Order quantity", Unit = "units", Kind = "range", Min = 1, Max = 100, Step = 1, Default = 1,
                Hint = "Units built by this work order. Multiplies the order-level rollup." },
        },
    };
}

public sealed class PlatformMeta
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
    public double BasePrice { get; set; }
}

/// <summary>ISO 9001:2015 / ISO 10007 controlled-document metadata for the spec sheet.</summary>
public sealed class ComplianceMeta
{
    public string FormNo { get; set; } = "QF-BOM-001";
    public string Revision { get; set; } = "A";
    public string Standard { get; set; } = "ISO 9001:2015";
    public string Clauses { get; set; } = "";
    public string Classification { get; set; } = "";
    public string Owner { get; set; } = "";
    public string Retention { get; set; } = "";
    public List<ApprovalBlock> Approvals { get; set; } = new();
    public List<RevisionEntry> RevisionHistory { get; set; } = new();
}

public sealed class ApprovalBlock
{
    public string Role { get; set; } = "";
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
}

public sealed class RevisionEntry
{
    public string Rev { get; set; } = "";
    public string Date { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
}

/// <summary>
/// A BoM assembly line. <see cref="Qty"/> is a fixed quantity unless
/// <see cref="QtyExpr"/> is set, in which case the quantity is a formula over the
/// attribute / calculated context. <see cref="KindId"/>/<see cref="GradeId"/>
/// link the line to the Process Manager item master when the part was added
/// from a Kind.
/// </summary>
public sealed class BomAssembly
{
    public string System { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Qty { get; set; } = 1;
    public string? QtyExpr { get; set; }
    public double Price { get; set; }
    public Guid? KindId { get; set; }
    public Guid? GradeId { get; set; }
    public List<BomAssembly> Children { get; set; } = new();
}

public sealed class RoutingOp
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Seq { get; set; }
}

/// <summary>A numeric attribute that feeds formulas (D365-style calculated config).</summary>
public sealed class ConfigAttribute
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Kind { get; set; } = "range";
    public double Min { get; set; }
    public double Max { get; set; }
    public double Step { get; set; } = 1;
    public double Default { get; set; }
    public string Hint { get; set; } = "";
}

/// <summary>A read-only attribute whose value is derived from a formula. Resolved top-to-bottom.</summary>
public sealed class CalcAttribute
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Expr { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Hint { get; set; } = "";
}

/// <summary>
/// An option group — one panel/step in the Configure stepper. Authors define
/// these freely: name, icon, single- or multi-select, required or optional.
/// </summary>
public sealed class OptionGroup
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "bi-box";
    public bool Multi { get; set; }
    public bool Required { get; set; }
    public string Hint { get; set; } = "";
    /// <summary>If set, the group only applies when one of these options is selected.</summary>
    public List<string>? AppliesIf { get; set; }
    public List<ConfigOption> Options { get; set; } = new();
}

public sealed class ConfigOption
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double Price { get; set; }
    /// <summary>If set, overrides <see cref="Price"/> — a formula over attributes/calc values.</summary>
    public string? PriceExpr { get; set; }
    public string Sub { get; set; } = "";
    public string? Swatch { get; set; }
    public string? Badge { get; set; }
    /// <summary>End-effector glyph key for the schematic preview (none/grip/vac/weld).</summary>
    public string? Ee { get; set; }
    /// <summary>Item-master Kind this option was created from, when applicable.</summary>
    public Guid? KindId { get; set; }
    /// <summary>Specific Grade of the Kind, when the author picked one.</summary>
    public Guid? GradeId { get; set; }
    public List<BomAssembly> Bom { get; set; } = new();
    public List<RoutingOp> Ops { get; set; } = new();
}

/// <summary>
/// A declarative constraint rule. Types: requires | requiresOneOf | excludes | formula.
/// A formula rule's <see cref="Expr"/> must evaluate truthy; <see cref="Refs"/> ties it to
/// attributes/options for inline display; <see cref="FixAttr"/>/<see cref="FixChoose"/>
/// power one-click fixes.
/// </summary>
public sealed class ConfigRule
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public List<string>? When { get; set; }
    public List<string>? Then { get; set; }
    public string? Expr { get; set; }
    public string Msg { get; set; } = "";
    public List<string>? Refs { get; set; }
    public FixAttr? FixAttr { get; set; }
    public string? FixChoose { get; set; }
}

public sealed class FixAttr
{
    public string Id { get; set; } = "";
    public double Value { get; set; }
}
