/* ============================================================
   BoM Configurator — output panels (BoM tree, price, routing, WO)
   window.PriceRollup, BomTree, Routing, ViolationsList, WorkOrderModal
   ============================================================ */
(function () {
  const DS = window.SequencerProDesignSystem_5eb90b;
  const { Button, StatusBadge, Tag } = DS;
  const fmt = window.CFG.fmt;
  const { useState } = React;

  const SectionLabel = ({ children, right }) => React.createElement("div", {
    style: { display: "flex", justifyContent: "space-between", alignItems: "center",
      fontSize: "var(--text-2xs)", fontWeight: 700, letterSpacing: "var(--ls-caps)",
      textTransform: "uppercase", color: "var(--text-muted)", margin: "0 0 8px" } },
    React.createElement("span", null, children), right);

  function PriceRollup({ sel }) {
    const { lines, total } = window.CFG.priceRollup(sel);
    const oq = (sel.attrs && sel.attrs.orderQty) || 1;
    return React.createElement("div", null,
      React.createElement("div", { style: { display: "flex", flexDirection: "column", gap: 1 } },
        lines.map((l, i) => React.createElement("div", { key: i, style: {
          display: "flex", justifyContent: "space-between", gap: 12, padding: "7px 0",
          borderBottom: "1px solid var(--border-default)", fontSize: "var(--text-sm)" } },
          React.createElement("span", { style: { color: l.base ? "var(--text-primary)" : "var(--text-secondary)", fontWeight: l.base ? 600 : 400 } },
            l.label, l.group && React.createElement("span", { style: { color: "var(--text-muted)", fontSize: "var(--text-2xs)", marginLeft: 6 } }, l.group)),
          React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontWeight: l.base ? 600 : 400, display: "flex", alignItems: "center", gap: 3 } },
            l.formula && React.createElement("span", { title: "per-unit formula price", style: { color: "var(--teal)", fontWeight: 700 } }, "ƒ"),
            l.base ? fmt(l.amount) : "+" + fmt(l.amount))))),
      React.createElement("div", { style: { display: "flex", justifyContent: "space-between", alignItems: "baseline",
        marginTop: 12, paddingTop: 12, borderTop: "2px solid var(--border-strong)" } },
        React.createElement("span", { style: { fontSize: "var(--text-sm)", fontWeight: 700, textTransform: "uppercase", letterSpacing: "var(--ls-caps)" } }, "Per unit"),
        React.createElement("span", { style: { fontFamily: "var(--font-display)", fontWeight: 800, fontSize: "var(--text-2xl)", color: "var(--text-primary)" } }, fmt(total))),
      oq > 1 && React.createElement("div", { style: { display: "flex", justifyContent: "space-between", alignItems: "baseline", marginTop: 8, padding: "8px 10px", background: "var(--gold-50)", border: "1px solid var(--gold-200)", borderRadius: "var(--radius-sm)" } },
        React.createElement("span", { style: { fontSize: "var(--text-xs)", fontWeight: 700, color: "var(--gold-700)" } }, "Order × " + oq + " robots"),
        React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontWeight: 700, color: "var(--gold-700)" } }, fmt(total * oq))));
  }

  function AssemblyRow({ a, lineTotal }) {
    const [open, setOpen] = useState(false);
    const has = a.children && a.children.length > 0;
    return React.createElement("div", null,
      React.createElement("div", { onClick: () => has && setOpen(o => !o), style: {
        display: "flex", alignItems: "center", gap: 8, padding: "7px 8px", borderRadius: "var(--radius-sm)",
        cursor: has ? "pointer" : "default" },
        onMouseEnter: e => e.currentTarget.style.background = "var(--grey-100)",
        onMouseLeave: e => e.currentTarget.style.background = "transparent" },
        React.createElement("i", { className: `bi ${has ? (open ? "bi-caret-down-fill" : "bi-caret-right-fill") : "bi-dot"}`,
          style: { fontSize: has ? 9 : 14, color: "var(--text-muted)", width: 12 } }),
        React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: "var(--text-link)", flex: "none", width: 72 } }, a.code),
        React.createElement("span", { style: { flex: 1, fontSize: "var(--text-sm)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" } }, a.name),
        React.createElement("span", { title: a.qtyExpr || undefined, style: { fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: a.qtyExpr ? "var(--teal-700)" : "var(--text-muted)", flex: "none" } }, "×" + a.qty, a.qtyExpr ? " ƒ" : ""),
        React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontSize: "var(--text-xs)", flex: "none", width: 64, textAlign: "right" } }, fmt(lineTotal(a)))),
      open && has && React.createElement("div", { style: { marginLeft: 32, borderLeft: "1px solid var(--border-default)", paddingLeft: 8 } },
        a.children.map((c, i) => React.createElement("div", { key: i, style: {
          display: "flex", alignItems: "center", gap: 8, padding: "5px 8px" } },
          React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: "var(--text-muted)", flex: "none", width: 72 } }, c.code),
          React.createElement("span", { style: { flex: 1, fontSize: "var(--text-xs)", color: "var(--text-secondary)" } }, c.name),
          React.createElement("span", { title: c.qtyExpr || undefined, style: { fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: c.qtyExpr ? "var(--teal-700)" : "var(--text-muted)", flex: "none" } }, "×" + c.qty, c.qtyExpr ? " ƒ" : ""),
          React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: "var(--text-muted)", flex: "none", width: 64, textAlign: "right" } }, fmt(c.price * c.qty))))));
  }

  function BomTree({ sel }) {
    const { systems, partCount, cost, lineTotal } = window.CFG.resolveBom(sel);
    return React.createElement("div", null,
      React.createElement(SectionLabel, { right: React.createElement("span", { style: { fontFamily: "var(--font-mono)", color: "var(--text-secondary)" } },
        partCount + " parts · " + fmt(cost) + " cost") }, "Resolved multi-level BoM"),
      systems.map((s, i) => React.createElement("div", { key: i, style: { marginBottom: 10 } },
        React.createElement("div", { style: { display: "flex", justifyContent: "space-between", padding: "4px 8px",
          background: "var(--bg-sunken)", borderRadius: "var(--radius-sm)", fontSize: "var(--text-2xs)", fontWeight: 700,
          letterSpacing: "0.04em", textTransform: "uppercase", color: "var(--text-secondary)" } },
          React.createElement("span", null, s.name),
          React.createElement("span", { style: { fontFamily: "var(--font-mono)" } }, fmt(s.total))),
        s.assemblies.map((a, j) => React.createElement(AssemblyRow, { key: j, a, lineTotal })))));
  }

  function Routing({ sel }) {
    const ops = window.CFG.generateRouting(sel);
    return React.createElement("div", null,
      React.createElement(SectionLabel, { right: React.createElement("span", { style: { fontFamily: "var(--font-mono)", color: "var(--text-secondary)" } }, ops.length + " operations") },
        "Generated routing → Process Manager"),
      React.createElement("div", { style: { position: "relative" } },
        ops.map((o, i) => React.createElement("div", { key: o.code, style: { display: "flex", gap: 10, alignItems: "flex-start", padding: "2px 0" } },
          React.createElement("div", { style: { display: "flex", flexDirection: "column", alignItems: "center", flex: "none" } },
            React.createElement("span", { style: { width: 22, height: 22, borderRadius: "50%", background: "var(--slate)", color: "#fff",
              fontSize: "var(--text-2xs)", fontWeight: 700, display: "inline-flex", alignItems: "center", justifyContent: "center" } }, i + 1),
            i < ops.length - 1 && React.createElement("span", { style: { width: 2, height: 16, background: "var(--border-strong)" } })),
          React.createElement("div", { style: { paddingBottom: 6 } },
            React.createElement("div", { style: { fontSize: "var(--text-sm)", fontWeight: 600 } }, o.name),
            React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: "var(--text-muted)" } }, o.code))))));
  }

  function ViolationsList({ violations, onFix }) {
    if (!violations.length) return React.createElement("div", { style: { display: "flex", flexDirection: "column", alignItems: "center", gap: 8, padding: "28px 16px", textAlign: "center" } },
      React.createElement("span", { style: { width: 40, height: 40, borderRadius: "50%", background: "var(--teal-50)", color: "var(--teal-600)", display: "inline-flex", alignItems: "center", justifyContent: "center", fontSize: 20 } },
        React.createElement("i", { className: "bi bi-check-lg" })),
      React.createElement("div", { style: { fontSize: "var(--text-sm)", fontWeight: 600, color: "var(--text-primary)" } }, "No conflicts"),
      React.createElement("div", { style: { fontSize: "var(--text-xs)", color: "var(--text-muted)" } }, "This configuration is valid and buildable."));
    return React.createElement("div", { style: { display: "flex", flexDirection: "column", gap: 8 } },
      violations.map((v, i) => React.createElement("div", { key: i, style: {
        display: "flex", gap: 10, padding: "10px 12px", borderRadius: "var(--radius-sm)",
        background: "#fdecec", border: "1px solid #f3c0c0" } },
        React.createElement("i", { className: "bi bi-exclamation-triangle-fill", style: { color: "var(--status-fail)", fontSize: 14, marginTop: 1 } }),
        React.createElement("div", { style: { flex: 1 } },
          React.createElement("div", { style: { fontSize: "var(--text-xs)", color: "#7a2222", lineHeight: 1.4 } }, v.msg),
          React.createElement("button", { onClick: () => onFix(v), style: {
            marginTop: 6, padding: "3px 10px", fontSize: "var(--text-2xs)", fontWeight: 700, fontFamily: "var(--font-sans)",
            color: "#fff", background: "var(--status-fail)", border: "none", borderRadius: "var(--radius-xs)", cursor: "pointer" } },
            React.createElement("i", { className: "bi bi-magic", style: { marginRight: 4 } }), v.fixLabel)))));
  }

  function ExportRow({ label, children, mono }) {
    return React.createElement("div", { style: { display: "flex", justifyContent: "space-between", gap: 12, padding: "5px 0", borderBottom: "1px solid var(--border-default)", fontSize: "var(--text-xs)" } },
      React.createElement("span", { style: { color: "var(--text-secondary)" } }, label),
      React.createElement("span", { style: { fontFamily: mono ? "var(--font-mono)" : "var(--font-sans)", fontWeight: 600, textAlign: "right" } }, children));
  }
  const ExportHead = ({ icon, children, right }) => React.createElement("div", { style: { display: "flex", alignItems: "center", gap: 7, margin: "16px 0 6px" } },
    React.createElement("i", { className: "bi " + icon, style: { fontSize: 13, color: "var(--text-muted)" } }),
    React.createElement("span", { style: { fontSize: "var(--text-2xs)", fontWeight: 700, letterSpacing: "var(--ls-caps)", textTransform: "uppercase", color: "var(--text-muted)", flex: 1 } }, children),
    right);

  function WorkOrderModal({ sel, enabled, onClose, onPrint }) {
    const spec = window.CFG.exportSpec(sel, enabled);
    const wo = React.useMemo(() => "WO-2026-" + String(Math.floor(Math.random() * 900) + 100), []);
    const [tab, setTab] = useState("summary");
    const [copied, setCopied] = useState("");

    const copy = (text, what) => {
      const done = () => { setCopied(what); setTimeout(() => setCopied(""), 1400); };
      try { navigator.clipboard.writeText(text).then(done, () => { fallbackCopy(text); done(); }); }
      catch (e) { fallbackCopy(text); done(); }
    };
    function fallbackCopy(text) { const ta = document.createElement("textarea"); ta.value = text; document.body.appendChild(ta); ta.select(); try { document.execCommand("copy"); } catch (e) {} document.body.removeChild(ta); }
    const download = (text, name, type) => {
      const blob = new Blob([text], { type: type || "text/plain" });
      const a = document.createElement("a"); a.href = URL.createObjectURL(blob); a.download = name; document.body.appendChild(a); a.click();
      setTimeout(() => { URL.revokeObjectURL(a.href); document.body.removeChild(a); }, 100);
    };

    const TabBtn = ({ id, children }) => React.createElement("button", { onClick: () => setTab(id), style: {
      flex: 1, padding: "8px 0", border: "none", borderBottom: tab === id ? "2px solid var(--gold)" : "2px solid transparent",
      background: "none", cursor: "pointer", fontFamily: "var(--font-sans)", fontSize: "var(--text-xs)", fontWeight: tab === id ? 700 : 500,
      color: tab === id ? "var(--text-primary)" : "var(--text-muted)" } }, children);

    const money = fmt;
    const qtyCell = (r) => React.createElement("span", { title: r.qtyFormula || undefined, style: { fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: r.qtyFormula ? "var(--teal-700)" : "var(--text-secondary)" } }, "×" + r.qty, r.qtyFormula ? " ƒ" : "");

    return React.createElement("div", { onClick: onClose, style: {
      position: "fixed", inset: 0, background: "rgba(33,33,33,0.55)", display: "grid", placeItems: "center", zIndex: 50, padding: 20 } },
      React.createElement("div", { onClick: e => e.stopPropagation(), style: {
        width: 680, maxWidth: "100%", maxHeight: "90vh", background: "var(--bg-surface)", borderRadius: "var(--radius-lg)", boxShadow: "var(--shadow-lg)", overflow: "hidden", display: "flex", flexDirection: "column" } },

        // header
        React.createElement("div", { style: { padding: "16px 22px", borderBottom: "var(--border)", display: "flex", alignItems: "center", gap: 11, flex: "none" } },
          React.createElement("span", { style: { width: 34, height: 34, borderRadius: "50%", background: "var(--teal-50)", color: "var(--teal-600)", display: "inline-flex", alignItems: "center", justifyContent: "center", fontSize: 18, flex: "none" } },
            React.createElement("i", { className: "bi bi-check-circle-fill" })),
          React.createElement("div", { style: { flex: 1 } },
            React.createElement("div", { style: { fontSize: "var(--text-lg)", fontWeight: 700 } }, "Resolved configuration"),
            React.createElement("div", { style: { fontSize: "var(--text-xs)", color: "var(--text-secondary)" } }, "Released to Process Manager as a work order")),
          React.createElement(Tag, { color: "gold" }, wo),
          React.createElement("button", { onClick: onClose, "aria-label": "Close", style: { border: "none", background: "none", cursor: "pointer", fontSize: 20, color: "var(--text-muted)", lineHeight: 1 } }, "×")),

        // stat tiles
        React.createElement("div", { style: { display: "grid", gridTemplateColumns: "repeat(4,1fr)", gap: 8, padding: "14px 22px 0", flex: "none" } },
          [["Parts", spec.bom.partCount], ["Operations", spec.routing.length], ["Per unit", money(spec.price.perUnit)], [spec.meta.orderQty > 1 ? "Order × " + spec.meta.orderQty : "Order total", money(spec.price.orderTotal)]].map(([k, v], i) =>
            React.createElement("div", { key: i, style: { background: "var(--bg-sunken)", borderRadius: "var(--radius-sm)", padding: "9px 10px", textAlign: "center" } },
              React.createElement("div", { style: { fontFamily: "var(--font-display)", fontWeight: 800, fontSize: "var(--text-lg)" } }, v),
              React.createElement("div", { style: { fontSize: 9.5, color: "var(--text-muted)", textTransform: "uppercase", letterSpacing: "0.05em" } }, k)))),

        // tabs
        React.createElement("div", { style: { display: "flex", padding: "10px 22px 0", gap: 4, flex: "none", borderBottom: "var(--border)" } },
          React.createElement(TabBtn, { id: "summary" }, "Spec"),
          React.createElement(TabBtn, { id: "bom" }, "BoM"),
          React.createElement(TabBtn, { id: "rules" }, "Rules & routing"),
          React.createElement(TabBtn, { id: "data" }, "Raw")),

        // body (scroll)
        React.createElement("div", { style: { padding: "4px 22px 18px", overflow: "auto", flex: 1 } },

          tab === "summary" && React.createElement("div", null,
            React.createElement(ExportHead, { icon: "bi-sliders" }, "Attributes"),
            spec.attributes.map(a => React.createElement(ExportRow, { key: a.id, label: a.name, mono: true },
              a.value + (a.unit ? " " + a.unit : ""),
              a.bounds && a.bounds.clampedBy.length ? React.createElement("span", { style: { color: "var(--blue-700)", marginLeft: 6, fontSize: 10 } }, "[" + a.bounds.min + "–" + a.bounds.max + " · " + a.bounds.clampedBy.join(",") + "]") : null)),
            React.createElement(ExportHead, { icon: "bi-calculator" }, "Calculated"),
            spec.calculated.map(c => React.createElement("div", { key: c.id, style: { display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center", padding: "5px 0", borderBottom: "1px solid var(--border-default)" } },
              React.createElement("span", { style: { fontSize: "var(--text-xs)", color: "var(--text-secondary)", flex: "none", width: 110 } }, c.name),
              React.createElement("code", { style: { flex: 1, minWidth: 0, fontFamily: "var(--font-mono)", fontSize: 10, color: "var(--text-muted)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" } }, c.expr),
              React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontWeight: 700, color: "var(--gold-700)", flex: "none" } }, c.value))),
            React.createElement(ExportHead, { icon: "bi-list-check" }, "Selections"),
            spec.selections.map((s, i) => React.createElement(ExportRow, { key: i, label: s.group + " · " + s.name },
              React.createElement("span", { style: { display: "inline-flex", alignItems: "center", gap: 4, fontFamily: "var(--font-mono)" } },
                s.priceFormula ? React.createElement("span", { title: s.priceFormula, style: { color: "var(--teal)", fontWeight: 700 } }, "ƒ") : null,
                s.unitPrice ? money(s.unitPrice) : "Incl."))),
            React.createElement(ExportHead, { icon: "bi-cash-stack" }, "Price rollup"),
            spec.price.lines.map((l, i) => React.createElement(ExportRow, { key: i, label: l.label + (l.group ? " · " + l.group : ""), mono: true },
              React.createElement("span", { style: { display: "inline-flex", alignItems: "center", gap: 4 } },
                l.priceFormula ? React.createElement("span", { title: l.priceFormula, style: { color: "var(--teal)", fontWeight: 700 } }, "ƒ") : null,
                (l.base ? "" : "+") + money(l.amount)))),
            React.createElement("div", { style: { display: "flex", justifyContent: "space-between", paddingTop: 9, marginTop: 3, fontWeight: 800 } },
              React.createElement("span", { style: { fontSize: "var(--text-sm)" } }, "Order total (×" + spec.meta.orderQty + ")"),
              React.createElement("span", { style: { fontFamily: "var(--font-display)", fontSize: "var(--text-xl)" } }, money(spec.price.orderTotal)))),

          tab === "bom" && React.createElement("div", { style: { marginTop: 8 } },
            React.createElement("div", { style: { display: "flex", padding: "6px 8px", fontSize: 9.5, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--text-muted)", borderBottom: "1px solid var(--border-strong)" } },
              React.createElement("span", { style: { width: 80, flex: "none" } }, "Code"),
              React.createElement("span", { style: { flex: 1 } }, "Part"),
              React.createElement("span", { style: { width: 54, flex: "none", textAlign: "right" } }, "Qty"),
              React.createElement("span", { style: { width: 66, flex: "none", textAlign: "right" } }, "Ext")),
            spec.bom.rows.map((r, i) => React.createElement("div", { key: i, style: { display: "flex", alignItems: "center", padding: "5px 8px", paddingLeft: r.level === 2 ? 22 : 8, borderBottom: "1px solid var(--border-default)", background: r.level === 1 ? "transparent" : "var(--grey-50)" } },
              React.createElement("span", { style: { width: r.level === 2 ? 66 : 80, flex: "none", fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: r.level === 1 ? "var(--text-link)" : "var(--text-muted)" } }, r.code),
              React.createElement("span", { style: { flex: 1, fontSize: r.level === 1 ? "var(--text-xs)" : "var(--text-2xs)", color: r.level === 1 ? "var(--text-primary)" : "var(--text-secondary)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" } },
                r.name, React.createElement("span", { style: { color: "var(--text-muted)", marginLeft: 6, fontSize: 9.5 } }, r.system)),
              React.createElement("span", { style: { width: 54, flex: "none", textAlign: "right" } }, qtyCell(r)),
              React.createElement("span", { style: { width: 66, flex: "none", textAlign: "right", fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)" } }, money(r.ext)))),
            React.createElement("div", { style: { display: "flex", justifyContent: "space-between", padding: "9px 8px", fontWeight: 700, fontSize: "var(--text-xs)" } },
              React.createElement("span", null, spec.bom.partCount + " parts"),
              React.createElement("span", { style: { fontFamily: "var(--font-mono)" } }, money(spec.bom.cost) + " cost")),
            React.createElement("div", { style: { fontSize: 10, color: "var(--text-muted)", marginTop: 4 } }, "ƒ marks a formula-driven quantity.")),

          tab === "rules" && React.createElement("div", null,
            React.createElement(ExportHead, { icon: "bi-diagram-3" }, "Active rules (" + spec.rules.length + ")"),
            spec.rules.map((r, i) => React.createElement("div", { key: i, style: { padding: "7px 0", borderBottom: "1px solid var(--border-default)" } },
              React.createElement("div", { style: { display: "flex", alignItems: "center", gap: 6, marginBottom: 3 } },
                React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontSize: 9.5, fontWeight: 700, color: "var(--text-muted)" } }, r.id),
                React.createElement(Tag, { color: r.type === "excludes" ? "orange" : r.type === "formula" ? "blue" : "teal", mono: false }, r.type),
                React.createElement("span", { style: { fontSize: "var(--text-2xs)", color: "var(--text-secondary)", flex: 1 } }, r.msg)),
              r.expr
                ? React.createElement("code", { style: { fontFamily: "var(--font-mono)", fontSize: 10, color: "var(--text-secondary)", background: "var(--bg-sunken)", borderRadius: 3, padding: "2px 6px", display: "inline-block" } }, r.expr)
                : React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontSize: 10, color: "var(--text-muted)" } }, (r.when || []).join(", ") + " → " + (r.then || []).join(", ")))),
            React.createElement(ExportHead, { icon: "bi-signpost-split" }, "Routing → Process Manager"),
            spec.routing.map((o) => React.createElement("div", { key: o.code, style: { display: "flex", alignItems: "center", gap: 9, padding: "4px 0" } },
              React.createElement("span", { style: { width: 20, height: 20, borderRadius: "50%", background: "var(--slate)", color: "#fff", fontSize: 10, fontWeight: 700, display: "inline-flex", alignItems: "center", justifyContent: "center", flex: "none" } }, o.step),
              React.createElement("span", { style: { fontSize: "var(--text-xs)", flex: 1 } }, o.name),
              React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: "var(--text-muted)" } }, o.code)))),

          tab === "data" && React.createElement("pre", { style: { fontFamily: "var(--font-mono)", fontSize: 10, lineHeight: 1.5, color: "var(--text-secondary)", background: "var(--bg-sunken)", borderRadius: "var(--radius-sm)", padding: 12, margin: "12px 0 0", overflow: "auto", whiteSpace: "pre-wrap", wordBreak: "break-word" } }, window.CFG.specToJSON(spec))),

        // footer actions
        React.createElement("div", { style: { padding: "12px 22px", borderTop: "var(--border)", display: "flex", gap: 8, flex: "none", alignItems: "center" } },
          React.createElement("button", { onClick: () => copy(window.CFG.bomToCSV(spec), "csv"), style: actBtn },
            React.createElement("i", { className: "bi bi-clipboard" }), copied === "csv" ? "Copied!" : "Copy BoM CSV"),
          React.createElement("button", { onClick: () => copy(window.CFG.specToJSON(spec), "json"), style: actBtn },
            React.createElement("i", { className: "bi bi-clipboard-data" }), copied === "json" ? "Copied!" : "Copy JSON"),
          React.createElement("button", { onClick: () => download(window.CFG.bomToCSV(spec), wo + "-bom.csv", "text/csv"), style: actBtn },
            React.createElement("i", { className: "bi bi-download" }), "CSV"),
          React.createElement("button", { onClick: () => onPrint(spec, wo), style: { ...actBtn, borderColor: "var(--slate)", background: "var(--slate)", color: "#fff" } },
            React.createElement("i", { className: "bi bi-printer-fill" }), "Print / Save PDF"),
          React.createElement("div", { style: { flex: 1 } }),
          React.createElement(Button, { onClick: onClose }, "Done"))));
  }
  const actBtn = { display: "inline-flex", alignItems: "center", gap: 5, padding: "8px 11px", borderRadius: "var(--radius-sm)",
    border: "1px solid var(--border-strong)", background: "var(--bg-surface)", cursor: "pointer", fontFamily: "var(--font-sans)",
    fontSize: "var(--text-xs)", fontWeight: 600, color: "var(--text-primary)" };

  // ── Print-ready spec sheet ────────────────────────────────────────────────
  // A clean paper document (Letter) rendered off-screen and revealed only under
  // @media print. Captures the full resolved spec for Save-as-PDF / hard copy.
  function SheetSection({ title, count, children }) {
    return React.createElement("section", { style: { marginTop: 18, breakInside: "avoid" } },
      React.createElement("div", { style: { display: "flex", justifyContent: "space-between", alignItems: "baseline", borderBottom: "1.5px solid #212121", paddingBottom: 3, marginBottom: 7 } },
        React.createElement("h2", { style: { margin: 0, fontFamily: "var(--font-display)", fontSize: "12pt", fontWeight: 800, letterSpacing: "0.02em", textTransform: "uppercase" } }, title),
        count != null && React.createElement("span", { style: { fontFamily: "var(--font-mono)", fontSize: "8.5pt", color: "#5e5e5e" } }, count)),
      children);
  }
  const cellTh = { textAlign: "left", fontFamily: "var(--font-sans)", fontSize: "7pt", fontWeight: 700, letterSpacing: "0.05em", textTransform: "uppercase", color: "#5e5e5e", padding: "3px 6px", borderBottom: "1px solid #adadad" };
  const cellTd = { fontFamily: "var(--font-sans)", fontSize: "8.5pt", padding: "3px 6px", borderBottom: "1px solid #e6e6e6", verticalAlign: "top" };
  const mono = { fontFamily: "var(--font-mono)" };

  function SpecSheet({ spec, wo }) {
    const money = fmt;
    const d = new Date(spec.meta.generatedAt);
    const dateStr = d.toLocaleDateString("en-US", { year: "numeric", month: "short", day: "numeric" }) + " " + d.toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit" });
    const NodeMark = DS.NodeMark;
    const comp = spec.meta.compliance || {};
    const ctrlCell = { padding: "3px 8px", borderRight: "1px solid #d4d4d4", borderBottom: "1px solid #d4d4d4" };
    const ctrlK = { fontSize: "6pt", textTransform: "uppercase", letterSpacing: "0.06em", color: "#8a8a8a", fontWeight: 700 };
    const ctrlV = { fontSize: "8pt", fontWeight: 600, fontFamily: "var(--font-mono)" };
    return React.createElement("div", { className: "spec-sheet", style: { width: "7.5in", margin: "0 auto", padding: "0", color: "#212121", fontFamily: "var(--font-sans)", background: "#fff", lineHeight: 1.4 } },

      // ISO controlled-document header
      React.createElement("div", { style: { border: "1.5px solid #212121", marginBottom: 12 } },
        React.createElement("div", { style: { display: "flex", alignItems: "stretch", borderBottom: "1.5px solid #212121" } },
          React.createElement("div", { style: { display: "flex", alignItems: "center", gap: 10, padding: "9px 12px", borderRight: "1.5px solid #212121", flex: 1 } },
            NodeMark ? React.createElement(NodeMark, { size: 38 }) : null,
            React.createElement("div", null,
              React.createElement("div", { style: { fontFamily: "var(--font-display)", fontSize: "15pt", fontWeight: 800, lineHeight: 1 } }, "SequencerPro"),
              React.createElement("div", { style: { fontSize: "7.5pt", color: "#5e5e5e", marginTop: 1 } }, "Manufacturing Engineering · Process Manager"))),
          React.createElement("div", { style: { padding: "7px 12px", textAlign: "center", display: "flex", flexDirection: "column", justifyContent: "center", minWidth: "2.5in" } },
            React.createElement("div", { style: { fontFamily: "var(--font-display)", fontSize: "11.5pt", fontWeight: 800, textTransform: "uppercase", letterSpacing: "0.02em", lineHeight: 1.1 } }, "Configuration & BoM"),
            React.createElement("div", { style: { fontFamily: "var(--font-display)", fontSize: "11.5pt", fontWeight: 800, textTransform: "uppercase", letterSpacing: "0.02em", lineHeight: 1.1 } }, "Specification Record"))),
        // control grid
        React.createElement("div", { style: { display: "grid", gridTemplateColumns: "repeat(4, 1fr)", borderBottom: "0" } },
          [["Document No.", spec.meta.docNo], ["Form / Template", comp.formNo || "QF-BOM-001"], ["Revision", comp.revision || "A"], ["Standard", comp.standard || "ISO 9001:2015"],
           ["Work Order", wo], ["Config Hash", spec.meta.configHash], ["Issue Date", dateStr], ["Page", "1 of 1"]].map(([k, v], i) =>
            React.createElement("div", { key: i, style: { ...ctrlCell, borderBottom: i < 4 ? "1px solid #d4d4d4" : "0", borderRight: (i % 4 === 3) ? "0" : "1px solid #d4d4d4" } },
              React.createElement("div", { style: ctrlK }, k),
              React.createElement("div", { style: ctrlV }, v)))),
        React.createElement("div", { style: { display: "flex", justifyContent: "space-between", alignItems: "center", padding: "3px 12px", background: "#212121", color: "#fff" } },
          React.createElement("span", { style: { fontSize: "7pt", fontWeight: 700, letterSpacing: "0.08em", textTransform: "uppercase" } }, comp.classification || "Controlled Document"),
          React.createElement("span", { style: { fontSize: "7pt", color: "#f1c40f", letterSpacing: "0.04em" } }, "Conforms to " + (comp.clauses || "ISO 9001:2015")))),

      // platform + summary band
      React.createElement("div", { style: { display: "flex", justifyContent: "space-between", alignItems: "center", background: "#f6f6f6", border: "1px solid #e6e6e6", borderRadius: 4, padding: "9px 13px", marginTop: 0 } },
        React.createElement("div", null,
          React.createElement("div", { style: { fontFamily: "var(--font-display)", fontSize: "12.5pt", fontWeight: 800 } }, spec.meta.platform.name),
          React.createElement("div", { style: { ...mono, fontSize: "8pt", color: "#5e5e5e" } }, spec.meta.platform.code)),
        React.createElement("div", { style: { display: "flex", gap: 20 } },
          [["Parts", spec.bom.partCount], ["Ops", spec.routing.length], ["Per unit", money(spec.price.perUnit)], ["Qty", spec.meta.orderQty], ["Order total", money(spec.price.orderTotal)]].map(([k, v], i) =>
            React.createElement("div", { key: i, style: { textAlign: "right" } },
              React.createElement("div", { style: { fontFamily: "var(--font-display)", fontSize: "12pt", fontWeight: 800 } }, v),
              React.createElement("div", { style: { fontSize: "6.5pt", textTransform: "uppercase", letterSpacing: "0.06em", color: "#8a8a8a" } }, k))))),

      // configuration selections
      React.createElement(SheetSection, { title: "Configuration" },
        React.createElement("div", { style: { display: "grid", gridTemplateColumns: "1fr 1fr", gap: "2px 24px" } },
          spec.selections.map((s, i) => React.createElement("div", { key: i, style: { display: "flex", justifyContent: "space-between", gap: 10, padding: "3px 0", borderBottom: "1px solid #f0f0f0", fontSize: "8.5pt" } },
            React.createElement("span", { style: { color: "#5e5e5e" } }, s.group),
            React.createElement("span", { style: { fontWeight: 600, textAlign: "right", flex: 1 } }, s.name,
              s.unitPrice ? React.createElement("span", { style: { ...mono, color: "#5e5e5e", marginLeft: 6, fontWeight: 400 } }, "+" + money(s.unitPrice)) : null))))),

      // attributes + calculated, two columns
      React.createElement("div", { style: { display: "flex", gap: 24, marginTop: 0 } },
        React.createElement("div", { style: { flex: 1 } },
          React.createElement(SheetSection, { title: "Attributes" },
            spec.attributes.map(a => React.createElement("div", { key: a.id, style: { display: "flex", justifyContent: "space-between", padding: "3px 0", borderBottom: "1px solid #f0f0f0", fontSize: "8.5pt" } },
              React.createElement("span", { style: { color: "#5e5e5e" } }, a.name,
                a.bounds && a.bounds.clampedBy.length ? React.createElement("span", { style: { ...mono, color: "#2769ab", fontSize: "7pt", marginLeft: 5 } }, "[" + a.bounds.min + "–" + a.bounds.max + "]") : null),
              React.createElement("span", { style: { ...mono, fontWeight: 700 } }, a.value + (a.unit ? " " + a.unit : "")))))),
        React.createElement("div", { style: { flex: 1 } },
          React.createElement(SheetSection, { title: "Calculated" },
            spec.calculated.map(c => React.createElement("div", { key: c.id, style: { display: "flex", justifyContent: "space-between", alignItems: "baseline", gap: 8, padding: "3px 0", borderBottom: "1px solid #f0f0f0", fontSize: "8.5pt" } },
              React.createElement("span", null, c.name,
                React.createElement("code", { style: { ...mono, display: "block", fontSize: "6.5pt", color: "#8a8a8a" } }, c.expr)),
              React.createElement("span", { style: { ...mono, fontWeight: 700, color: "#93750a" } }, c.value)))))),

      // BoM table
      React.createElement(SheetSection, { title: "Bill of Materials", count: spec.bom.partCount + " parts · " + money(spec.bom.cost) },
        React.createElement("table", { style: { width: "100%", borderCollapse: "collapse" } },
          React.createElement("thead", null, React.createElement("tr", null,
            React.createElement("th", { style: { ...cellTh, width: "13%" } }, "Code"),
            React.createElement("th", { style: cellTh }, "Part"),
            React.createElement("th", { style: { ...cellTh, width: "16%" } }, "Source"),
            React.createElement("th", { style: { ...cellTh, width: "12%", textAlign: "right" } }, "Qty"),
            React.createElement("th", { style: { ...cellTh, width: "11%", textAlign: "right" } }, "Unit"),
            React.createElement("th", { style: { ...cellTh, width: "12%", textAlign: "right" } }, "Ext"))),
          React.createElement("tbody", null,
            spec.bom.rows.map((r, i) => React.createElement("tr", { key: i },
              React.createElement("td", { style: { ...cellTd, ...mono, fontSize: "7.5pt", color: "#2769ab", paddingLeft: r.level === 2 ? 16 : 6 } }, r.code),
              React.createElement("td", { style: { ...cellTd, color: r.level === 2 ? "#5e5e5e" : "#212121" } }, r.name),
              React.createElement("td", { style: { ...cellTd, fontSize: "7.5pt", color: "#5e5e5e" } }, r.source),
              React.createElement("td", { style: { ...cellTd, ...mono, textAlign: "right", color: r.qtyFormula ? "#16806b" : "#212121" } }, "×" + r.qty, r.qtyFormula ? React.createElement("span", { style: { fontSize: "6.5pt" } }, " ƒ " + r.qtyFormula) : null),
              React.createElement("td", { style: { ...cellTd, ...mono, textAlign: "right" } }, money(r.unitPrice)),
              React.createElement("td", { style: { ...cellTd, ...mono, textAlign: "right" } }, money(r.ext)))))),
        React.createElement("div", { style: { fontSize: "7pt", color: "#8a8a8a", marginTop: 4 } }, "ƒ denotes a formula-driven quantity.")),

      // price rollup (full width, capped measure)
      React.createElement(SheetSection, { title: "Price Rollup" },
        React.createElement("div", { style: { maxWidth: "4in" } },
          spec.price.lines.map((l, i) => React.createElement("div", { key: i, style: { display: "flex", justifyContent: "space-between", padding: "3px 0", borderBottom: "1px solid #f0f0f0", fontSize: "8.5pt" } },
            React.createElement("span", { style: { color: l.base ? "#212121" : "#5e5e5e", fontWeight: l.base ? 600 : 400 } }, l.label,
              l.priceFormula ? React.createElement("code", { style: { ...mono, display: "block", fontSize: "6.5pt", color: "#16806b" } }, "ƒ " + l.priceFormula) : null),
            React.createElement("span", { style: { ...mono, fontWeight: l.base ? 600 : 400 } }, (l.base ? "" : "+") + money(l.amount)))),
          React.createElement("div", { style: { display: "flex", justifyContent: "space-between", paddingTop: 6, marginTop: 2, borderTop: "1.5px solid #212121", fontWeight: 800 } },
            React.createElement("span", { style: { fontSize: "9pt" } }, "Order total (×" + spec.meta.orderQty + ")"),
            React.createElement("span", { style: { fontFamily: "var(--font-display)", fontSize: "11pt" } }, money(spec.price.orderTotal))))),

      // ── Routing & Sequence Clearance Record — one clearance stamp per operation ──
      React.createElement(SheetSection, { title: "Routing & Sequence Clearance Record", count: spec.routing.length + " operations" },
        React.createElement("div", { style: { fontSize: "7.5pt", color: "#5e5e5e", marginBottom: 7 } },
          "Operations must be cleared in sequence. On completion of each operation the operator signs and dates the row and the authorized inspector applies the sequence clearance stamp; the next operation may not begin until the preceding stamp is applied (ISO 9001:2015 §8.5.1 production control, §8.5.2 identification & traceability)."),
        React.createElement("table", { style: { width: "100%", borderCollapse: "collapse", border: "1px solid #adadad" } },
          React.createElement("thead", null, React.createElement("tr", { style: { background: "#212121" } },
            React.createElement("th", { style: { ...cellTh, color: "#fff", borderBottom: "1px solid #212121", width: "7%", textAlign: "center" } }, "Seq"),
            React.createElement("th", { style: { ...cellTh, color: "#fff", borderBottom: "1px solid #212121", width: "12%" } }, "Op Code"),
            React.createElement("th", { style: { ...cellTh, color: "#fff", borderBottom: "1px solid #212121" } }, "Operation"),
            React.createElement("th", { style: { ...cellTh, color: "#fff", borderBottom: "1px solid #212121", width: "17%" } }, "Operator"),
            React.createElement("th", { style: { ...cellTh, color: "#fff", borderBottom: "1px solid #212121", width: "11%" } }, "Date"),
            React.createElement("th", { style: { ...cellTh, color: "#fff", borderBottom: "1px solid #212121", width: "16%", textAlign: "center" } }, "Sequence Clearance"))),
          React.createElement("tbody", null,
            spec.routing.map(o => React.createElement("tr", { key: o.code, style: { breakInside: "avoid" } },
              React.createElement("td", { style: { ...cellTd, textAlign: "center", verticalAlign: "middle" } },
                React.createElement("span", { style: { width: 15, height: 15, borderRadius: "50%", background: "#212121", color: "#fff", fontSize: "6.5pt", fontWeight: 700, display: "inline-flex", alignItems: "center", justifyContent: "center" } }, o.step)),
              React.createElement("td", { style: { ...cellTd, ...mono, fontSize: "7.5pt", verticalAlign: "middle" } }, o.code),
              React.createElement("td", { style: { ...cellTd, verticalAlign: "middle" } }, o.name),
              React.createElement("td", { style: { ...cellTd } },
                React.createElement("div", { style: { height: 22, borderBottom: "1px solid #c4c4c4" } }),
                React.createElement("div", { style: { fontSize: "5.5pt", color: "#adadad", textTransform: "uppercase", letterSpacing: "0.05em" } }, "Sign")),
              React.createElement("td", { style: { ...cellTd } },
                React.createElement("div", { style: { height: 22, borderBottom: "1px solid #c4c4c4" } })),
              React.createElement("td", { style: { ...cellTd, padding: "3px" } },
                React.createElement("div", { style: { height: 34, border: "1px dashed #adadad", borderRadius: 2, display: "flex", alignItems: "center", justifyContent: "center" } },
                  React.createElement("span", { style: { fontSize: "5.5pt", textTransform: "uppercase", letterSpacing: "0.07em", color: "#c4c4c4", fontWeight: 700 } }, "Stamp " + String(o.step).padStart(2, "0")))))))),
        React.createElement("div", { style: { fontSize: "6.5pt", color: "#8a8a8a", marginTop: 4 } },
          "A skipped or out-of-sequence stamp voids the record — raise a nonconformance report (§8.7) and re-inspect before proceeding.")),

      // rules
      React.createElement(SheetSection, { title: "Applied Rules", count: spec.rules.length },
        React.createElement("div", { style: { display: "grid", gridTemplateColumns: "1fr 1fr", gap: "2px 24px" } },
          spec.rules.map((r, i) => {
            const detail = r.expr || ((r.when || []).join(",") + " \u2192 " + (r.then || []).join(","));
            const tone = r.type === "excludes" ? "#93380c" : r.type === "formula" ? "#1c4d7e" : "#115f50";
            return React.createElement("div", { key: i, style: { padding: "3px 0", borderBottom: "1px solid #f0f0f0" } },
              React.createElement("div", { style: { fontSize: "8pt", display: "flex", gap: 5 } },
                React.createElement("span", { style: { ...mono, fontWeight: 700, color: "#8a8a8a", fontSize: "7pt" } }, r.id),
                React.createElement("span", { style: { fontWeight: 600, textTransform: "uppercase", fontSize: "6.5pt", letterSpacing: "0.04em", color: tone } }, r.type),
                React.createElement("span", { style: { color: "#5e5e5e", flex: 1 } }, r.msg)),
              React.createElement("code", { style: { ...mono, fontSize: "6.5pt", color: "#8a8a8a" } }, detail));
          }))),

      // ── ISO authorization, approval & inspection stamps ──
      React.createElement(SheetSection, { title: "Authorization & Approval" },
        React.createElement("div", { style: { fontSize: "7.5pt", color: "#5e5e5e", marginBottom: 8 } },
          "By signing and stamping below, each authority confirms this configuration record has been verified per ", (comp.standard || "ISO 9001:2015"), ". Document is not valid for production release until all required signatures and the Quality stamp are applied."),
        React.createElement("div", { style: { display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 10 } },
          (comp.approvals || []).map((a, i) => React.createElement("div", { key: i, style: { border: "1px solid #adadad", borderRadius: 3, display: "flex", minHeight: 78, breakInside: "avoid" } },
            // signature column
            React.createElement("div", { style: { flex: 1, padding: "6px 9px", display: "flex", flexDirection: "column" } },
              React.createElement("div", { style: { fontSize: "6.5pt", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.06em", color: "#8a8a8a" } }, a.role),
              React.createElement("div", { style: { flex: 1 } }),
              React.createElement("div", { style: { borderBottom: "1px solid #212121", marginBottom: 2, minHeight: 16 } }),
              React.createElement("div", { style: { display: "flex", justifyContent: "space-between", fontSize: "6.5pt", color: "#8a8a8a" } },
                React.createElement("span", null, "Name / Signature"),
                React.createElement("span", null, a.title)),
              React.createElement("div", { style: { display: "flex", gap: 6, marginTop: 5 } },
                React.createElement("div", { style: { flex: 1, borderBottom: "1px solid #adadad", fontSize: "6.5pt", color: "#8a8a8a", paddingBottom: 1 } }, "Date"))),
            // stamp box
            React.createElement("div", { style: { width: 78, flex: "none", borderLeft: "1px dashed #adadad", display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center" } },
              React.createElement("span", { style: { fontSize: "6pt", textTransform: "uppercase", letterSpacing: "0.08em", color: "#c4c4c4", fontWeight: 700, lineHeight: 1.3 } }, "Stamp /", React.createElement("br"), "Seal")))))),

      // Quality release — prominent controlled stamp
      React.createElement("div", { style: { display: "flex", gap: 12, marginTop: 14, breakInside: "avoid" } },
        React.createElement("div", { style: { flex: 1, border: "1.5px solid #212121", borderRadius: 3, padding: "8px 11px" } },
          React.createElement("div", { style: { fontSize: "7pt", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.06em", color: "#5e5e5e", marginBottom: 4 } }, "Quality Disposition"),
          React.createElement("div", { style: { display: "flex", gap: 14, marginBottom: 8 } },
            ["Accepted", "Accepted w/ deviation", "Rejected"].map((opt, i) => React.createElement("span", { key: i, style: { display: "inline-flex", alignItems: "center", gap: 5, fontSize: "8pt" } },
              React.createElement("span", { style: { width: 11, height: 11, border: "1.5px solid #212121", borderRadius: 2, flex: "none" } }), opt))),
          React.createElement("div", { style: { display: "flex", gap: 12 } },
            React.createElement("div", { style: { flex: 2, borderBottom: "1px solid #212121", fontSize: "6.5pt", color: "#8a8a8a" } }, "QA Inspector"),
            React.createElement("div", { style: { flex: 1, borderBottom: "1px solid #212121", fontSize: "6.5pt", color: "#8a8a8a" } }, "Insp. ID"),
            React.createElement("div", { style: { flex: 1, borderBottom: "1px solid #212121", fontSize: "6.5pt", color: "#8a8a8a" } }, "Date"))),
        React.createElement("div", { style: { width: 118, flex: "none", border: "1.5px solid #212121", borderRadius: "50%", aspectRatio: "1 / 1", display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", textAlign: "center" } },
          React.createElement("div", { style: { fontSize: "6.5pt", fontWeight: 700, letterSpacing: "0.08em", color: "#c4c4c4", textTransform: "uppercase" } }, "Quality"),
          React.createElement("div", { style: { fontSize: "6.5pt", fontWeight: 700, letterSpacing: "0.08em", color: "#c4c4c4", textTransform: "uppercase" } }, "Approved"),
          React.createElement("div", { style: { width: 70, borderTop: "1px solid #d4d4d4", margin: "6px 0", paddingTop: 4, fontSize: "5.5pt", color: "#c4c4c4" } }, "Controlled stamp here"))),

      // Revision history (ISO 10007 configuration management record)
      React.createElement(SheetSection, { title: "Revision History" },
        React.createElement("table", { style: { width: "100%", borderCollapse: "collapse" } },
          React.createElement("thead", null, React.createElement("tr", null,
            React.createElement("th", { style: { ...cellTh, width: "9%" } }, "Rev"),
            React.createElement("th", { style: { ...cellTh, width: "16%" } }, "Date"),
            React.createElement("th", { style: cellTh }, "Description of change"),
            React.createElement("th", { style: { ...cellTh, width: "26%" } }, "Author / System"))),
          React.createElement("tbody", null,
            (comp.revisionHistory || []).map((r, i) => React.createElement("tr", { key: i },
              React.createElement("td", { style: { ...cellTd, ...mono, fontWeight: 700 } }, r.rev),
              React.createElement("td", { style: { ...cellTd, ...mono, fontSize: "7.5pt" } }, r.date),
              React.createElement("td", { style: cellTd }, r.description),
              React.createElement("td", { style: { ...cellTd, fontSize: "7.5pt", color: "#5e5e5e" } }, r.author))))),
        React.createElement("div", { style: { display: "flex", gap: 24, marginTop: 6, fontSize: "7pt", color: "#5e5e5e" } },
          React.createElement("span", null, React.createElement("strong", null, "Document owner: "), comp.owner || "Manufacturing Engineering"),
          React.createElement("span", null, React.createElement("strong", null, "Retention: "), comp.retention || "Retain 7 years"))),

      // ── ISO controlled-document footer ──
      React.createElement("div", { style: { marginTop: 20, borderTop: "1.5px solid #212121", paddingTop: 6, display: "flex", justifyContent: "space-between", alignItems: "flex-start", fontSize: "6.5pt", color: "#8a8a8a" } },
        React.createElement("div", { style: { maxWidth: "3.6in", lineHeight: 1.4 } },
          React.createElement("strong", { style: { color: "#5e5e5e" } }, "UNCONTROLLED WHEN PRINTED. "),
          "The controlled master of this record is held in SequencerPro Process Manager. Verify revision against the system register before use. © SequencerPro — confidential."),
        React.createElement("div", { style: { textAlign: "right", ...mono, lineHeight: 1.5 } },
          React.createElement("div", null, spec.meta.docNo),
          React.createElement("div", null, (comp.formNo || "QF-BOM-001") + " Rev " + (comp.revision || "A")),
          React.createElement("div", null, "Page 1 of 1"))));
  }

  Object.assign(window, { PriceRollup, BomTree, Routing, ViolationsList, WorkOrderModal, SpecSheet });
})();
