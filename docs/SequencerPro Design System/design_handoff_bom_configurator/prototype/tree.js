/* ============================================================
   BoM Configurator — Product Tree explorer ("150% BoM"), visual
   window.ProductTree
   A node-and-branch diagram: platform root → connector lines →
   group nodes → option cards hanging off a vertical rail. Cards
   expand in place to show the parts each option adds. Gold ring =
   in the current configuration; ƒ = formula-driven qty/price;
   diagram badge = rules touching the option (hover to read).
   ============================================================ */
(function () {
  const { useState } = React;
  const DS = window.SequencerProDesignSystem_5eb90b;
  const C = window.CFG, CAR = window.CAR, fmt = C.fmt;

  const TREE_CSS = `
    .pt-chart { display: flex; justify-content: flex-start; min-width: max-content; padding: 0 10px; }
    .pt-chart ul { display: flex; padding: 0; margin: 0; position: relative; }
    .pt-chart li { list-style: none; display: flex; flex-direction: column; align-items: center; position: relative; padding: 22px 7px 0 7px; }
    /* horizontal bar halves + vertical drop to each child */
    .pt-chart li::before, .pt-chart li::after { content: ''; position: absolute; top: 0; right: 50%; border-top: 1.5px solid var(--border-strong); width: 50%; height: 22px; }
    .pt-chart li::after { right: auto; left: 50%; border-left: 1.5px solid var(--border-strong); }
    .pt-chart li:only-child::after, .pt-chart li:only-child::before { display: none; }
    .pt-chart li:only-child { padding-top: 0; }
    .pt-chart li:first-child::before, .pt-chart li:last-child::after { border-top: 0 none; }
    .pt-chart li:last-child::before { border-right: 1.5px solid var(--border-strong); border-top-right-radius: 6px; }
    .pt-chart li:first-child::after { border-top-left-radius: 6px; }
    /* stub below a parent node down to its children bar */
    .pt-chart ul ul::before { content: ''; position: absolute; top: -22px; left: 50%; border-left: 1.5px solid var(--border-strong); width: 0; height: 22px; }
  `;

  function ruleBadges(optId) {
    return CAR.rules.filter(r => (r.when || []).includes(optId) || (r.then || []).includes(optId) || (r.refs || []).includes(optId));
  }

  // Parts detail shown inside an expanded card.
  function PartsList({ bom }) {
    if (!(bom || []).length) return <div style={{ fontSize: "var(--text-2xs)", color: "var(--text-muted)", fontStyle: "italic", padding: "4px 2px 0" }}>No parts added</div>;
    return (
      <div style={{ borderTop: "1px dashed var(--border-default)", marginTop: 6, paddingTop: 5, display: "flex", flexDirection: "column", gap: 2 }}>
        {bom.map(a => (
          <React.Fragment key={a.code}>
            <div style={{ display: "flex", alignItems: "center", gap: 5 }}>
              <span style={{ fontFamily: "var(--font-mono)", fontSize: 9.5, color: "var(--text-link)", width: 58, flex: "none", textAlign: "left" }}>{a.code}</span>
              <span style={{ fontSize: "var(--text-2xs)", flex: 1, textAlign: "left", minWidth: 0, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{a.name}</span>
              <span title={a.qtyExpr || undefined} style={{ fontFamily: "var(--font-mono)", fontSize: 9.5, fontWeight: 600, color: a.qtyExpr ? "var(--teal-700)" : "var(--text-muted)", flex: "none" }}>{a.qtyExpr ? "ƒ" : "×" + a.qty}</span>
            </div>
            {(a.children || []).map(p => (
              <div key={p.code} style={{ display: "flex", alignItems: "center", gap: 5, paddingLeft: 12 }}>
                <span style={{ width: 8, borderTop: "1px solid var(--border-default)", flex: "none" }} />
                <span style={{ fontFamily: "var(--font-mono)", fontSize: 9, color: "var(--text-muted)", width: 52, flex: "none", textAlign: "left" }}>{p.code}</span>
                <span style={{ fontSize: 9.5, color: "var(--text-secondary)", flex: 1, textAlign: "left", minWidth: 0, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{p.name}</span>
                <span title={p.qtyExpr || undefined} style={{ fontFamily: "var(--font-mono)", fontSize: 9, fontWeight: 600, color: p.qtyExpr ? "var(--teal-700)" : "var(--text-muted)", flex: "none" }}>{p.qtyExpr ? "ƒ" : "×" + p.qty}</span>
              </div>))}
          </React.Fragment>))}
      </div>);
  }

  // A leaf option card on the diagram.
  function OptionNode({ o, sel }) {
    const [open, setOpen] = useState(false);
    const isSel = C.isSel(sel, o.id);
    const rules = ruleBadges(o.id);
    const hasF = !!o.priceExpr || (o.bom || []).some(a => a.qtyExpr || (a.children || []).some(p => p.qtyExpr));
    return (
      <button onClick={() => setOpen(v => !v)} style={{
        width: 168, textAlign: "center", cursor: "pointer", fontFamily: "var(--font-sans)",
        background: isSel ? "var(--gold-50)" : "var(--bg-surface)",
        border: isSel ? "2px solid var(--gold)" : "1px solid var(--border-strong)",
        borderRadius: "var(--radius)", padding: "7px 9px", boxShadow: "var(--shadow-xs)", position: "relative" }}>
        {isSel && <i className="bi bi-check-circle-fill" title="In current configuration"
          style={{ position: "absolute", top: -7, right: -7, color: "var(--gold-600)", fontSize: 14, background: "var(--bg-surface)", borderRadius: "50%" }} />}
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: 6 }}>
          {o.swatch && <span style={{ width: 10, height: 10, borderRadius: "50%", background: o.swatch, border: "1px solid rgba(0,0,0,0.12)", flex: "none" }} />}
          <span style={{ fontSize: "var(--text-xs)", fontWeight: 700, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{o.name}</span>
        </div>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: 6, marginTop: 3 }}>
          <span style={{ fontFamily: "var(--font-mono)", fontSize: 9.5, fontWeight: 600, color: "var(--text-secondary)" }}>
            {o.priceExpr ? "ƒ price" : o.price ? "+" + fmt(o.price) : "Incl."}</span>
          {hasF && <span title="Contains formula-driven quantities/prices" style={{ fontFamily: "var(--font-mono)", fontSize: 9.5, fontWeight: 700, color: "var(--teal-700)" }}>ƒ</span>}
          {rules.length > 0 && <span title={rules.map(r => r.id + ": " + r.msg).join("\n")}
            style={{ fontSize: 9.5, color: "var(--blue-700)" }}><i className="bi bi-diagram-3" /> {rules.length}</span>}
          {(o.ops || []).length > 0 && <span title={o.ops.map(op => op.code + " " + op.name).join("\n")}
            style={{ fontSize: 9.5, color: "var(--text-muted)" }}><i className="bi bi-signpost-split" /> {o.ops.length}</span>}
        </div>
        {open && <PartsList bom={o.bom} />}
      </button>);
  }

  // Group node card.
  function GroupCard({ g }) {
    return (
      <div style={{ width: 168, textAlign: "center", background: "var(--slate)", color: "#fff",
        borderRadius: "var(--radius)", padding: "7px 9px", boxShadow: "var(--shadow-sm)" }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: 6 }}>
          <i className={"bi " + (g.icon || "bi-dot")} style={{ color: "var(--gold)", fontSize: 12 }} />
          <span style={{ fontSize: "var(--text-xs)", fontWeight: 700 }}>{g.name}</span>
        </div>
        <div style={{ fontSize: 9, textTransform: "uppercase", letterSpacing: "0.06em", color: "var(--grey)", marginTop: 2 }}>
          {g.multi ? "multi-select" : g.required ? "pick one" : "optional"} · {g.options.length}</div>
      </div>);
  }

  // Base-platform branch: assemblies as one stacked card set.
  function BaseCard() {
    const [open, setOpen] = useState(false);
    return (
      <button onClick={() => setOpen(v => !v)} style={{
        width: 168, textAlign: "center", cursor: "pointer", fontFamily: "var(--font-sans)",
        background: "var(--gold-50)", border: "2px solid var(--gold)", borderRadius: "var(--radius)",
        padding: "7px 9px", boxShadow: "var(--shadow-xs)" }}>
        <div style={{ fontSize: "var(--text-xs)", fontWeight: 700 }}><i className="bi bi-box" style={{ color: "var(--gold-700)", marginRight: 5 }} />Every build</div>
        <div style={{ fontFamily: "var(--font-mono)", fontSize: 9.5, fontWeight: 600, color: "var(--text-secondary)", marginTop: 3 }}>
          {CAR.baseBom.length} assemblies · {fmt(CAR.platform.basePrice)}</div>
        {open && <PartsList bom={CAR.baseBom} />}
      </button>);
  }

  function ProductTree({ sel }) {
    const totalOpts = CAR.groups.reduce((n, g) => n + g.options.length, 0);
    return (
      <div style={{ height: "100%", overflow: "auto", background: "var(--bg-app)" }}>
        <style>{TREE_CSS}</style>
        <div style={{ padding: "22px 26px 60px" }}>
          <div style={{ maxWidth: 860, margin: "0 auto 4px" }}>
            <h2 style={{ fontSize: "var(--text-xl)", fontWeight: 700, margin: "0 0 4px", display: "flex", alignItems: "center", gap: 9, justifyContent: "center" }}>
              <i className="bi bi-diagram-2" style={{ color: "var(--gold-600)" }} />Product tree</h2>
            <p style={{ fontSize: "var(--text-xs)", color: "var(--text-secondary)", margin: "0 0 18px", lineHeight: 1.5, textAlign: "center" }}>
              The full "150% BoM" — everything the platform <em>can</em> be. Click any card to see the parts it adds.
              Gold ring = in the current configuration · <span style={{ fontFamily: "var(--font-mono)", color: "var(--teal-700)" }}>ƒ</span> = formula-driven ·{" "}
              <i className="bi bi-diagram-3" style={{ color: "var(--blue-700)" }} /> = rules (hover) · <i className="bi bi-signpost-split" /> = routing ops.</p>
          </div>

          <div className="pt-chart">
            <ul>
              <li>
                {/* root node */}
                <div style={{ width: 200, textAlign: "center", background: "var(--bg-surface)", border: "2px solid var(--slate)",
                  borderRadius: "var(--radius-md)", padding: "10px 12px", boxShadow: "var(--shadow-sm)" }}>
                  <div style={{ fontFamily: "var(--font-display)", fontWeight: 800, fontSize: "var(--text-md)" }}>{CAR.platform.name}</div>
                  <div style={{ fontFamily: "var(--font-mono)", fontSize: 9.5, color: "var(--text-muted)", marginTop: 1 }}>{CAR.platform.code}</div>
                  <div style={{ fontSize: 9.5, color: "var(--text-secondary)", marginTop: 3 }}>{CAR.groups.length} groups · {totalOpts} options · {CAR.rules.length} rules</div>
                </div>
                <ul>
                  <li><BaseCard /></li>
                  {CAR.groups.map(g => (
                    <li key={g.id}>
                      <GroupCard g={g} />
                      {/* vertical rail of option cards */}
                      <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 0, marginTop: 0 }}>
                        {g.options.map(o => (
                          <div key={o.id} style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
                            <span style={{ width: 0, height: 14, borderLeft: "1.5px solid var(--border-strong)" }} />
                            <OptionNode o={o} sel={sel} />
                          </div>))}
                      </div>
                    </li>))}
                </ul>
              </li>
            </ul>
          </div>
        </div>
      </div>);
  }

  window.ProductTree = ProductTree;
})();
