using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProcessManager.Domain.Configurator;

/// <summary>
/// The pure rules / resolution engine for the BoM configurator (ported from the
/// design handoff <c>engine.js</c>). No UI, no DB — given a <see cref="ProductPlatform"/>
/// and a <see cref="ConfigSelection"/> it evaluates calculated attributes, fires
/// the rules (producing disabled options + violations + auto-corrections),
/// resolves the multi-level BoM with formula quantities, rolls up price,
/// generates routing, derives attribute bounds and builds the serialisable
/// export spec (with an ISO 10007 configuration fingerprint).
///
/// One instance wraps one platform; it holds no per-request state.
/// </summary>
public sealed class ConfiguratorEngine
{
    private readonly ProductPlatform _p;

    public ConfiguratorEngine(ProductPlatform platform) => _p = platform;

    public ProductPlatform Platform => _p;

    // ── lookups ───────────────────────────────────────────────────────────────
    public ConfigOption? Opt(string id)
    {
        foreach (var g in _p.Groups)
        {
            var o = g.Options.FirstOrDefault(o => o.Id == id);
            if (o != null) return o;
        }
        return null;
    }

    public OptionGroup? GroupOf(string id) => _p.Groups.FirstOrDefault(g => g.Options.Any(o => o.Id == id));
    public OptionGroup? GroupById(string gid) => _p.Groups.FirstOrDefault(g => g.Id == gid);
    private ConfigAttribute? AttrDef(string id) => _p.Attributes.FirstOrDefault(a => a.Id == id);
    public string AttrName(string id) => AttrDef(id)?.Name ?? id;

    // ── selection helpers ───────────────────────────────────────────────────────
    public bool IsSel(ConfigSelection sel, string id)
    {
        var g = GroupOf(id);
        if (g == null) return false;
        return sel.Of(g.Id).Contains(id);
    }

    private bool AllSel(ConfigSelection sel, IEnumerable<string> ids) => ids.All(id => IsSel(sel, id));
    private bool AnySel(ConfigSelection sel, IEnumerable<string> ids) => ids.Any(id => IsSel(sel, id));

    public List<OptionGroup> ApplicableGroups(ConfigSelection sel) =>
        _p.Groups.Where(g => g.AppliesIf == null || AnySel(sel, g.AppliesIf)).ToList();

    public ConfigSelection DefaultSelections()
    {
        var sel = new ConfigSelection();
        foreach (var g in _p.Groups)
        {
            if (!g.Multi && g.Required && g.Options.Count > 0) sel.Choices[g.Id] = new List<string> { g.Options[0].Id };
            else sel.Choices[g.Id] = new List<string>();
        }
        foreach (var a in _p.Attributes) sel.Attrs[a.Id] = a.Default;
        return sel;
    }

    // ── formula / calculated-attribute engine ──────────────────────────────────
    private EvalScope Context(ConfigSelection sel)
    {
        var values = new Dictionary<string, double>();
        foreach (var a in _p.Attributes)
            values[a.Id] = sel.Attrs.TryGetValue(a.Id, out var v) ? v : a.Default;

        var scope = new EvalScope(values, id => IsSel(sel, id));
        // Calculated values resolve in declaration order so later calc can use earlier.
        foreach (var c in _p.Calc)
            values[c.Id] = ExpressionEvaluator.Eval(c.Expr, scope);
        return scope;
    }

    public List<CalcValue> ComputeCalc(ConfigSelection sel)
    {
        var scope = Context(sel);
        return _p.Calc.Select(c => new CalcValue
        {
            Id = c.Id, Name = c.Name, Unit = c.Unit, Expr = c.Expr, Hint = c.Hint,
            Value = ExpressionEvaluator.Eval(c.Expr, scope),
        }).ToList();
    }

    private IEnumerable<string> KnownIdents()
    {
        foreach (var a in _p.Attributes) yield return a.Id;
        foreach (var c in _p.Calc) yield return c.Id;
    }

    /// <summary>Validate an authored expression against this platform's identifiers.</summary>
    public ExprValidation ValidateExpr(string? expr) =>
        ExpressionEvaluator.Validate(expr, KnownIdents(), Context(DefaultSelections()));

    public ConfigSelection SetAttr(ConfigSelection sel, string id, double val)
    {
        sel = sel.Clone();
        sel.Attrs[id] = val;
        return sel;
    }

    /// <summary>
    /// Derive the effective [min,max] for a numeric attribute from active bound
    /// rules. Recognises simple comparisons that reference ONLY this attribute
    /// (e.g. "reach &lt;= 1400"). Combines with the attribute's own declared
    /// min/max — tightest wins.
    /// </summary>
    public AttrBounds AttrBoundsFor(string attrId, IReadOnlyCollection<string>? enabledRuleIds)
    {
        var a = AttrDef(attrId);
        double lo = a?.Min ?? double.NegativeInfinity;
        double hi = a?.Max ?? double.PositiveInfinity;
        var fromRules = new List<string>();
        var step = a?.Step ?? 1;
        var re = new Regex(@"^\s*([A-Za-z_$][\w$]*)\s*(<=|>=|<|>)\s*(-?\d+(?:\.\d+)?)\s*$");

        foreach (var r in _p.Rules)
        {
            if (r.Type != "formula") continue;
            if (enabledRuleIds != null && !enabledRuleIds.Contains(r.Id)) continue;
            foreach (var clause in (r.Expr ?? "").Split("&&"))
            {
                var m = re.Match(clause);
                if (!m.Success || m.Groups[1].Value != attrId) continue;
                var op = m.Groups[2].Value;
                var n = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                if (op == "<=") hi = Math.Min(hi, n);
                else if (op == "<") hi = Math.Min(hi, n - step);
                else if (op == ">=") lo = Math.Max(lo, n);
                else if (op == ">") lo = Math.Max(lo, n + step);
                if (!fromRules.Contains(r.Id)) fromRules.Add(r.Id);
            }
        }
        return new AttrBounds { Min = lo, Max = hi, FromRules = fromRules, Clamped = fromRules.Count > 0 };
    }

    // ── reconcile: apply active rules to the selection ──────────────────────────
    public ReconcileResult Reconcile(ConfigSelection sel, IReadOnlyCollection<string>? enabledRuleIds = null)
    {
        sel = sel.Clone();
        var rules = _p.Rules.Where(r => enabledRuleIds == null || enabledRuleIds.Contains(r.Id)).ToList();
        var disabled = new Dictionary<string, string>();
        var violations = new List<Violation>();
        var notes = new List<string>();

        // Clear selections in non-applicable groups.
        var appIds = ApplicableGroups(sel).Select(g => g.Id).ToHashSet();
        foreach (var g in _p.Groups)
            if (!appIds.Contains(g.Id)) sel.Choices[g.Id] = new List<string>();

        // Iterate to a fixpoint (auto-corrections can cascade), capped.
        for (var pass = 0; pass < 6; pass++)
        {
            var changed = false;
            foreach (var r in rules)
            {
                if (r.Type == "formula" || r.When == null || !AllSel(sel, r.When)) continue;

                if (r.Type == "requires")
                {
                    foreach (var tid in r.Then ?? new())
                    {
                        var tg = GroupOf(tid);
                        if (tg == null) continue;
                        if (tg.Multi)
                        {
                            if (!IsSel(sel, tid))
                            {
                                var captured = tid; var g = tg;
                                violations.Add(new Violation
                                {
                                    RuleId = r.Id, Msg = r.Msg, FixLabel = $"Add {Opt(captured)?.Name}",
                                    Fix = s => { s = s.Clone(); var l = s.Of(g.Id); if (!l.Contains(captured)) l.Add(captured); return s; },
                                });
                            }
                        }
                        else
                        {
                            // single-select: force the required option; disable siblings.
                            foreach (var o in tg.Options) if (o.Id != tid) disabled[o.Id] = r.Msg;
                            if (sel.Single(tg.Id) != tid && appIds.Contains(tg.Id))
                            {
                                sel.Choices[tg.Id] = new List<string> { tid };
                                changed = true;
                                notes.Add($"{r.Msg} — set {tg.Name} to {Opt(tid)?.Name}.");
                            }
                        }
                    }
                }
                else if (r.Type == "requiresOneOf")
                {
                    if (!AnySel(sel, r.Then ?? new()))
                    {
                        var first = r.Then![0];
                        violations.Add(new Violation
                        {
                            RuleId = r.Id, Msg = r.Msg, FixLabel = $"Choose {Opt(first)?.Name}",
                            Fix = s => ApplyChoice(s, first),
                        });
                    }
                }
                else if (r.Type == "excludes")
                {
                    foreach (var tid in r.Then ?? new())
                    {
                        disabled[tid] = r.Msg;
                        if (IsSel(sel, tid))
                        {
                            var tg = GroupOf(tid)!;
                            var captured = tid;
                            violations.Add(new Violation
                            {
                                RuleId = r.Id, Msg = r.Msg, FixLabel = $"Remove {Opt(captured)?.Name}",
                                Fix = s =>
                                {
                                    s = s.Clone();
                                    if (tg.Multi) s.Choices[tg.Id] = s.Of(tg.Id).Where(x => x != captured).ToList();
                                    else s.Choices[tg.Id] = tg.Required && tg.Options.Count > 0 ? new List<string> { tg.Options[0].Id } : new List<string>();
                                    return s;
                                },
                            });
                        }
                    }
                }
            }
            if (!changed) break;
        }

        // Formula constraints: the expression must evaluate truthy, else a violation.
        foreach (var r in rules)
        {
            if (r.Type != "formula") continue;
            var scope = Context(sel);
            if (!ExpressionEvaluator.EvalBool(r.Expr, scope))
            {
                var v = new Violation { RuleId = r.Id, Msg = r.Msg };
                if (r.FixChoose != null)
                {
                    var choose = r.FixChoose;
                    v.FixLabel = $"Choose {Opt(choose)?.Name}";
                    v.Fix = s => ApplyChoice(s, choose);
                }
                else if (r.FixAttr != null)
                {
                    var fa = r.FixAttr;
                    v.FixLabel = $"Set {AttrName(fa.Id)} = {Fmt(fa.Value)}";
                    v.Fix = s => SetAttr(s, fa.Id, fa.Value);
                }
                else
                {
                    v.FixLabel = "Review";
                    v.Fix = s => s;
                }
                violations.Add(v);
            }
        }

        // De-dupe by id+msg.
        var seen = new HashSet<string>();
        var uniq = violations.Where(v => seen.Add(v.RuleId + v.Msg)).ToList();
        return new ReconcileResult { Selection = sel, Disabled = disabled, Violations = uniq, Notes = notes };
    }

    /// <summary>Apply a single choice (respecting single/multi) WITHOUT reconcile.</summary>
    public ConfigSelection ApplyChoice(ConfigSelection sel, string id)
    {
        sel = sel.Clone();
        var g = GroupOf(id);
        if (g == null) return sel;
        if (g.Multi)
        {
            var l = sel.Of(g.Id);
            sel.Choices[g.Id] = l.Contains(id) ? l.Where(x => x != id).ToList() : l.Append(id).ToList();
        }
        else
        {
            sel.Choices[g.Id] = new List<string> { id };
        }
        return sel;
    }

    // ── BoM resolution ──────────────────────────────────────────────────────────
    public BomResult ResolveBom(ConfigSelection sel)
    {
        var scope = Context(sel);
        int Qv(BomAssembly l) => l.QtyExpr != null ? Math.Max(0, (int)Math.Round(ExpressionEvaluator.Eval(l.QtyExpr, scope), MidpointRounding.AwayFromZero)) : l.Qty;

        ResolvedBomLine Resolve(BomAssembly l, string source) => new()
        {
            System = l.System, Code = l.Code, Name = l.Name, Qty = Qv(l), QtyExpr = l.QtyExpr, Price = l.Price, Source = source,
            Children = l.Children.Select(c => new ResolvedBomLine
            {
                System = l.System, Code = c.Code, Name = c.Name, Qty = Qv(c), QtyExpr = c.QtyExpr, Price = c.Price, Source = source,
            }).ToList(),
        };

        var lines = _p.BaseBom.Select(b => Resolve(b, "Platform")).ToList();
        foreach (var g in ApplicableGroups(sel))
        {
            var chosen = g.Multi ? sel.Of(g.Id) : (sel.Single(g.Id) is { } one ? new List<string> { one } : new List<string>());
            foreach (var id in chosen)
            {
                var o = Opt(id);
                if (o != null) foreach (var b in o.Bom) lines.Add(Resolve(b, o.Name));
            }
        }

        var bySystem = new Dictionary<string, List<ResolvedBomLine>>();
        foreach (var l in lines)
        {
            if (!bySystem.TryGetValue(l.System, out var list)) bySystem[l.System] = list = new();
            list.Add(l);
        }

        var systems = bySystem.Select(kv => new BomSystem
        {
            Name = kv.Key,
            Assemblies = kv.Value,
            Total = kv.Value.Sum(l => l.LineTotal),
        }).ToList();

        var partCount = lines.Sum(l => l.Qty + l.Children.Sum(c => c.Qty));
        var cost = systems.Sum(s => s.Total);
        return new BomResult { Systems = systems, PartCount = partCount, Cost = cost };
    }

    // ── price rollup ────────────────────────────────────────────────────────────
    public PriceResult PriceRollup(ConfigSelection sel)
    {
        var scope = Context(sel);
        var lines = new List<PriceLine> { new() { Label = $"{_p.Platform.Name} base", Amount = _p.Platform.BasePrice, Base = true } };
        foreach (var g in ApplicableGroups(sel))
        {
            var chosen = g.Multi ? sel.Of(g.Id) : (sel.Single(g.Id) is { } one ? new List<string> { one } : new List<string>());
            foreach (var id in chosen)
            {
                var o = Opt(id);
                if (o == null) continue;
                var amt = o.PriceExpr != null ? Math.Round(ExpressionEvaluator.Eval(o.PriceExpr, scope), MidpointRounding.AwayFromZero) : o.Price;
                if (amt > 0) lines.Add(new PriceLine { Label = o.Name, Amount = amt, Group = g.Name, Formula = o.PriceExpr != null });
            }
        }
        return new PriceResult { Lines = lines, Total = lines.Sum(l => l.Amount) };
    }

    // ── routing ─────────────────────────────────────────────────────────────────
    public List<RoutingOp> GenerateRouting(ConfigSelection sel)
    {
        var ops = new List<RoutingOp>(_p.BaseOps);
        foreach (var g in ApplicableGroups(sel))
        {
            var chosen = g.Multi ? sel.Of(g.Id) : (sel.Single(g.Id) is { } one ? new List<string> { one } : new List<string>());
            foreach (var id in chosen) { var o = Opt(id); if (o != null) ops.AddRange(o.Ops); }
        }
        var map = new Dictionary<string, RoutingOp>();
        foreach (var o in ops) map[o.Code] = o; // de-dupe by code
        return map.Values.OrderBy(o => o.Seq).ToList();
    }

    // ── completion status ───────────────────────────────────────────────────────
    public ConfigStatus Status(ConfigSelection sel, IReadOnlyCollection<Violation> violations)
    {
        // Required groups that have no options yet (mid-authoring) can't be
        // selected, so they don't count against completeness.
        var need = ApplicableGroups(sel).Where(g => g.Required && g.Options.Count > 0).ToList();
        var done = need.Count(g => sel.Of(g.Id).Count > 0);
        return new ConfigStatus { Done = done, Total = need.Count, Complete = done == need.Count && violations.Count == 0 };
    }

    // ── dependency edges (author canvas) ────────────────────────────────────────
    public List<DependencyEdge> DependencyEdges(IReadOnlyCollection<string>? enabledRuleIds)
    {
        var edges = new List<DependencyEdge>();
        foreach (var r in _p.Rules)
        {
            if (enabledRuleIds != null && !enabledRuleIds.Contains(r.Id)) continue;
            var froms = r.When ?? (r.Refs != null && r.Refs.Count > 0 ? new List<string> { r.Refs[0] } : new());
            var tos = r.Then ?? (r.Refs != null ? r.Refs.Skip(1).ToList() : new());
            foreach (var w in froms)
                foreach (var t in tos)
                    if (Opt(w) != null && Opt(t) != null)
                        edges.Add(new DependencyEdge { Rule = r.Id, Type = r.Type, From = w, To = t, Msg = r.Msg });
        }
        return edges;
    }

    // ── serialisable export spec ────────────────────────────────────────────────
    public ExportSpec ExportSpec(ConfigSelection sel, IReadOnlyCollection<string>? enabled = null)
    {
        var scope = Context(sel);
        var oq = (int)(sel.Attrs.TryGetValue("orderQty", out var q) ? q : 1);
        var bom = ResolveBom(sel);
        var price = PriceRollup(sel);

        var attributes = _p.Attributes.Select(a =>
        {
            var b = a.Kind == "range" ? AttrBoundsFor(a.Id, enabled) : new AttrBounds();
            return new ExportAttribute
            {
                Id = a.Id, Name = a.Name,
                Value = sel.Attrs.TryGetValue(a.Id, out var v) ? v : a.Default,
                Unit = a.Unit, Kind = a.Kind,
                Bounds = a.Kind == "range" ? new ExportBounds
                {
                    Min = double.IsFinite(b.Min) ? b.Min : a.Min,
                    Max = double.IsFinite(b.Max) ? b.Max : a.Max,
                    ClampedBy = b.FromRules,
                } : null,
            };
        }).ToList();

        var calculated = _p.Calc.Select(c => new CalcValue
        {
            Id = c.Id, Name = c.Name, Expr = c.Expr, Unit = c.Unit, Value = ExpressionEvaluator.Eval(c.Expr, scope),
        }).ToList();

        var selections = new List<ExportSelection>();
        foreach (var g in ApplicableGroups(sel))
        {
            var ids = g.Multi ? sel.Of(g.Id) : (sel.Single(g.Id) is { } one ? new List<string> { one } : new List<string>());
            foreach (var id in ids)
            {
                var o = Opt(id);
                if (o == null) continue;
                selections.Add(new ExportSelection
                {
                    Group = g.Name, GroupId = g.Id, OptionId = id, Name = o.Name,
                    UnitPrice = o.PriceExpr != null ? Math.Round(ExpressionEvaluator.Eval(o.PriceExpr, scope), MidpointRounding.AwayFromZero) : o.Price,
                    PriceFormula = o.PriceExpr,
                });
            }
        }

        var rows = new List<ExportBomRow>();
        foreach (var s in bom.Systems)
            foreach (var a in s.Assemblies)
            {
                rows.Add(new ExportBomRow { Level = 1, System = s.Name, Source = a.Source, Code = a.Code, Name = a.Name, Qty = a.Qty, QtyFormula = a.QtyExpr, UnitPrice = a.Price, Ext = a.Price * a.Qty });
                foreach (var c in a.Children)
                    rows.Add(new ExportBomRow { Level = 2, System = s.Name, Source = a.Source, Code = c.Code, Name = c.Name, Qty = c.Qty, QtyFormula = c.QtyExpr, UnitPrice = c.Price, Ext = c.Price * c.Qty });
            }

        var priceLines = price.Lines.Select(l =>
        {
            var o = l.Formula ? OptByName(l.Label) : null;
            return new ExportPriceLine { Label = l.Label, Amount = l.Amount, Base = l.Base, Group = l.Group, PriceFormula = o?.PriceExpr };
        }).ToList();

        var rules = _p.Rules.Where(r => enabled == null || enabled.Contains(r.Id)).Select(r => new ExportRule
        {
            Id = r.Id, Type = r.Type, Msg = r.Msg, When = r.When, Then = r.Then, Expr = r.Expr, Refs = r.Refs,
        }).ToList();

        // Configuration fingerprint (ISO 10007 traceability): deterministic djb2
        // hash over the resolved selections + attributes → identical config yields
        // an identical document-control id.
        var fpSrc = "{\"p\":\"" + _p.Platform.Code + "\",\"s\":[" +
            string.Join(",", selections.Select(s => "\"" + s.OptionId + "\"")) + "],\"a\":[" +
            string.Join(",", attributes.Select(a => "\"" + a.Id + ":" + Fmt(a.Value) + "\"")) + "]}";
        uint h = 5381;
        foreach (var ch in fpSrc) h = ((h << 5) + h + ch);
        var configHash = h.ToString("X8");
        var comp = _p.Compliance;
        var docNo = comp.FormNo + "/" + _p.Platform.Code + "-" + configHash;

        return new ExportSpec
        {
            Meta = new ExportMeta
            {
                Platform = _p.Platform, BasePrice = _p.Platform.BasePrice,
                GeneratedAt = DateTime.UtcNow.ToString("o"), OrderQty = oq,
                Compliance = comp, ConfigHash = configHash, DocNo = docNo,
            },
            Attributes = attributes,
            Calculated = calculated,
            Selections = selections,
            Bom = new ExportBom { Rows = rows, PartCount = bom.PartCount, Cost = bom.Cost },
            Price = new ExportPrice { Lines = priceLines, PerUnit = price.Total, OrderQty = oq, OrderTotal = price.Total * oq },
            Routing = GenerateRouting(sel).Select((o, i) => new ExportRouting { Step = i + 1, Code = o.Code, Name = o.Name }).ToList(),
            Rules = rules,
        };
    }

    private ConfigOption? OptByName(string name)
    {
        foreach (var g in _p.Groups)
        {
            var o = g.Options.FirstOrDefault(o => o.Name == name);
            if (o != null) return o;
        }
        return null;
    }

    // ── serializers ─────────────────────────────────────────────────────────────
    public static string BomToCsv(ExportSpec spec)
    {
        var head = new[] { "Level", "System", "Source", "Code", "Part", "Qty", "QtyFormula", "UnitPrice", "ExtPrice" };
        static string Esc(string? v)
        {
            v ??= "";
            return Regex.IsMatch(v, "[\",\n]") ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }
        var sb = new StringBuilder();
        sb.Append(string.Join(",", head)).Append('\n');
        foreach (var r in spec.Bom.Rows)
            sb.Append(string.Join(",", new[]
            {
                r.Level.ToString(), r.System, r.Source, r.Code, r.Name, r.Qty.ToString(),
                r.QtyFormula ?? "", Fmt(r.UnitPrice), Fmt(r.Ext),
            }.Select(Esc))).Append('\n');
        return sb.ToString().TrimEnd('\n');
    }

    public static string Fmt(double n) => (Math.Abs(n % 1) < 1e-9 ? ((long)Math.Round(n)).ToString(CultureInfo.InvariantCulture) : n.ToString(CultureInfo.InvariantCulture));
    public static string Money(double n) => "$" + Math.Round(n).ToString("#,##0", CultureInfo.InvariantCulture);
}
