/* ============================================================
   BoM Configurator — Author experience
   Node/graph canvas with visual dependency lines + rule builder.
   window.AuthorView
   ============================================================ */
(function () {
  const { useState } = React;
  const DS = window.SequencerProDesignSystem_5eb90b;
  const { Button } = DS;
  const C = window.CFG, CAR = window.CAR, fmt = C.fmt;

  const COLW = 178, COLGAP = 66, PADX = 26, HEADY = 22, NODEH = 44, NODEGAP = 13, TOPY = 50;
  const colX = (i) => PADX + i * (COLW + COLGAP);
  const nodeY = (r) => TOPY + r * (NODEH + NODEGAP);

  function layout() {
    const pos = {};
    CAR.groups.forEach((g, ci) => g.options.forEach((o, ri) => {
      pos[o.id] = { x: colX(ci), y: nodeY(ri), w: COLW, h: NODEH, cx: colX(ci) + COLW / 2, cyMid: nodeY(ri) + NODEH / 2 };
    }));
    const totalW = colX(CAR.groups.length - 1) + COLW + PADX;
    const maxRows = Math.max(...CAR.groups.map(g => g.options.length));
    const totalH = nodeY(maxRows - 1) + NODEH + 30;
    return { pos, totalW, totalH };
  }

  function edgePath(a, b) {
    const fromX = a.x + a.w, fromY = a.cyMid, toX = b.x, toY = b.cyMid;
    const dir = toX >= fromX ? 1 : -1;
    return `M${fromX},${fromY} C${fromX + 48 * dir},${fromY} ${toX - 48 * dir},${toY} ${toX},${toY}`;
  }

  // Inline editable expression with live validation (commits on blur / Enter).
  function EditableExpr({ value, onCommit, placeholder, boolean }) {
    const [v, setV] = useState(value);
    React.useEffect(() => setV(value), [value]);
    const res = C.validateExpr(v, { boolean });
    const bad = !res.ok;
    return <div>
      <input value={v} placeholder={placeholder || "expression"} spellCheck={false}
        onChange={e => setV(e.target.value)}
        onBlur={() => { if (res.ok) onCommit(v); else setV(value); }}
        onKeyDown={e => { if (e.key === "Enter") e.currentTarget.blur(); if (e.key === "Escape") { setV(value); e.currentTarget.blur(); } }}
        style={{ width: "100%", boxSizing: "border-box", fontFamily: "var(--font-mono)", fontSize: 10,
          background: "var(--bg-surface)", border: `1px solid ${bad ? "var(--orange)" : "var(--border-strong)"}`,
          borderRadius: 3, padding: "4px 6px", color: "var(--text-primary)", outlineColor: bad ? "var(--orange)" : "var(--blue)" }} />
      {bad
        ? <div style={{ fontSize: 9.5, color: "var(--orange-700)", marginTop: 2, display: "flex", alignItems: "center", gap: 4 }}><i className="bi bi-exclamation-triangle-fill" />{res.error}. Press Esc to revert.</div>
        : <div style={{ fontSize: 9.5, color: "var(--text-muted)", marginTop: 2 }}>= {String(res.value)} <span style={{ opacity: .6 }}>(sample)</span></div>}
    </div>;
  }

  function AddCalc({ onRefresh }) {
    const [open, setOpen] = useState(false);
    const [name, setName] = useState("");
    const [expr, setExpr] = useState("");
    if (!open) return <button onClick={() => setOpen(true)} style={{ marginTop: 4, background: "none", border: "none", color: "var(--text-link)", cursor: "pointer", fontSize: 11, fontWeight: 600, padding: 0, fontFamily: "var(--font-sans)" }}><i className="bi bi-plus-lg" /> Add calculated attribute</button>;
    const res = C.validateExpr(expr);
    const sty = { width: "100%", boxSizing: "border-box", height: 28, borderRadius: 3, border: "1px solid var(--border-strong)", fontSize: 11, padding: "0 7px", marginBottom: 5, fontFamily: "var(--font-sans)" };
    return <div style={{ marginTop: 6, paddingTop: 8, borderTop: "1px solid var(--border-default)" }}>
      <input value={name} onChange={e => setName(e.target.value)} placeholder="Name (e.g. Floor mats)" style={sty} />
      <input value={expr} onChange={e => setExpr(e.target.value)} placeholder="Formula (e.g. seats)" spellCheck={false} style={{ ...sty, fontFamily: "var(--font-mono)", fontSize: 10, marginBottom: 2, borderColor: expr && !res.ok ? "var(--orange)" : "var(--border-strong)" }} />
      <div style={{ fontSize: 9.5, color: expr && !res.ok ? "var(--orange-700)" : "var(--text-muted)", marginBottom: 6, minHeight: 12 }}>{!expr ? "" : res.ok ? "= " + String(res.value) + " (sample)" : res.error}</div>
      <div style={{ display: "flex", gap: 6 }}>
        <Button size="sm" variant="secondary" onClick={() => setOpen(false)}>Cancel</Button>
        <Button size="sm" block disabled={!name || !res.ok} onClick={() => { CAR.calc.push({ id: "c" + (CAR.calc.length + 1), name: name || "New value", expr: expr || "0", unit: "" }); onRefresh(); setOpen(false); setName(""); setExpr(""); }}>Add</Button>
      </div>
    </div>;
  }

  function AuthorView({ enabled, onToggleRule, onAddRule, onRefresh }) {
    const { pos, totalW, totalH } = layout();
    const [hover, setHover] = useState(null);     // ruleId hovered
    const [sel, setSel] = useState(null);          // optionId selected on canvas
    const edges = C.dependencyEdges(enabled);
    const sampleVals = {}; C.computeCalc(C.defaultSelections()).forEach(c => sampleVals[c.id] = c.value);
    const colorFor = (t) => t === "excludes" ? "var(--orange)" : "var(--teal)";

    // node is highlighted if part of hovered rule or selected
    const hoveredRule = hover ? CAR.rules.find(r => r.id === hover) : null;
    const nodeHot = (id) => (hoveredRule && ((hoveredRule.when || []).includes(id) || (hoveredRule.then || []).includes(id) || (hoveredRule.refs || []).includes(id))) || sel === id;
    const selRules = sel ? CAR.rules.filter(r => (r.when || []).includes(sel) || (r.then || []).includes(sel) || (r.refs || []).includes(sel)) : [];

    return (
      <div style={{ display: "grid", gridTemplateColumns: "1fr 320px", height: "100%", overflow: "hidden" }}>
        {/* Canvas */}
        <div style={{ overflow: "auto", background: "var(--bg-app)", backgroundImage: "radial-gradient(var(--border-default) 1px, transparent 1px)", backgroundSize: "22px 22px" }}>
          <div style={{ position: "relative", width: totalW, height: totalH, minHeight: "100%" }}>
            {/* group headers */}
            {CAR.groups.map((g, ci) => (
              <div key={g.id} style={{ position: "absolute", left: colX(ci), top: 14, width: COLW, display: "flex", alignItems: "center", gap: 6,
                fontSize: "var(--text-2xs)", fontWeight: 700, letterSpacing: "0.06em", textTransform: "uppercase", color: "var(--text-muted)" }}>
                <i className={`bi ${g.icon}`} /> {g.name}{g.appliesIf && <i className="bi bi-funnel-fill" title="Conditional group" style={{ fontSize: 9 }} />}
              </div>
            ))}
            {/* edges */}
            <svg width={totalW} height={totalH} style={{ position: "absolute", inset: 0, pointerEvents: "none" }}>
              <defs>
                <marker id="ah-teal" markerWidth="9" markerHeight="9" refX="7" refY="4.5" orient="auto"><path d="M0,0 L8,4.5 L0,9 Z" fill="var(--teal)" /></marker>
                <marker id="ah-orange" markerWidth="9" markerHeight="9" refX="7" refY="4.5" orient="auto"><path d="M0,0 L8,4.5 L0,9 Z" fill="var(--orange)" /></marker>
              </defs>
              {edges.map((e, i) => {
                const a = pos[e.from], b = pos[e.to]; if (!a || !b) return null;
                const hot = hover === e.rule || (sel && (e.from === sel || e.to === sel));
                const dim = (hover || sel) && !hot;
                return <path key={i} d={edgePath(a, b)} fill="none" stroke={colorFor(e.type)}
                  strokeWidth={hot ? 2.6 : 1.6} strokeDasharray={e.type === "excludes" ? "5 4" : "none"}
                  opacity={dim ? 0.12 : hot ? 1 : 0.5} markerEnd={`url(#ah-${e.type === "excludes" ? "orange" : "teal"})`} />;
              })}
            </svg>
            {/* nodes */}
            {CAR.groups.map(g => g.options.map(o => {
              const p = pos[o.id], hot = nodeHot(o.id);
              return <div key={o.id} onClick={() => setSel(s => s === o.id ? null : o.id)} style={{
                position: "absolute", left: p.x, top: p.y, width: p.w, height: p.h, boxSizing: "border-box",
                display: "flex", alignItems: "center", gap: 8, padding: "0 10px", cursor: "pointer",
                background: "var(--bg-surface)", borderRadius: "var(--radius)", border: hot ? "2px solid var(--gold)" : "1px solid var(--border-default)",
                boxShadow: hot ? "var(--shadow-md)" : "var(--shadow-xs)", transition: "box-shadow .12s, border-color .12s", zIndex: hot ? 3 : 2 }}>
                <span style={{ width: 8, height: 8, borderRadius: "50%", background: o.swatch || "var(--ink-400)", flex: "none", border: "1px solid rgba(0,0,0,0.1)" }} />
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontSize: "var(--text-xs)", fontWeight: 600, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{o.name}</div>
                  <div style={{ fontFamily: "var(--font-mono)", fontSize: 10, color: "var(--text-muted)" }}>{o.id}</div>
                </div>
                {o.price > 0 && <span style={{ fontFamily: "var(--font-mono)", fontSize: 10, color: "var(--text-secondary)", flex: "none" }}>+{fmt(o.price)}</span>}
              </div>;
            }))}
          </div>
        </div>

        {/* Rule builder */}
        <aside style={{ borderLeft: "var(--border)", background: "var(--bg-surface)", display: "flex", flexDirection: "column", overflow: "hidden" }}>
          <div style={{ padding: "14px 16px", borderBottom: "var(--border)" }}>
            <div style={{ fontSize: "var(--text-md)", fontWeight: 700 }}>Constraint rules</div>
            <div style={{ fontSize: "var(--text-xs)", color: "var(--text-secondary)", marginTop: 2 }}>
              Toggle, edit or add rules &amp; formulas — changes apply live in <b>Configure</b>. {sel ? `Showing rules on ${C.opt(sel).name}.` : "Hover a rule to trace it on the canvas."}</div>
          </div>
          <div style={{ flex: 1, overflowY: "auto", padding: 12 }}>
            {!sel && <div style={{ marginBottom: 12, padding: "10px 12px", borderRadius: "var(--radius-sm)", background: "var(--bg-sunken)", border: "1px dashed var(--border-strong)" }}>
              <div style={{ fontSize: "var(--text-2xs)", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.06em", color: "var(--text-muted)", marginBottom: 8, display: "flex", alignItems: "center", gap: 6 }}><i className="bi bi-calculator" />Calculated attributes</div>
              {CAR.calc.map(c => <div key={c.id} style={{ marginBottom: 8 }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", marginBottom: 3 }}>
                  <span style={{ fontWeight: 600, fontSize: 11 }}>{c.name}</span>
                  <span style={{ fontFamily: "var(--font-mono)", fontSize: 10, fontWeight: 700, color: "var(--gold-700)" }}>= {sampleVals[c.id]}</span>
                </div>
                <EditableExpr value={c.expr} onCommit={v => { c.expr = v; onRefresh(); }} />
              </div>)}
              <AddCalc onRefresh={onRefresh} />
            </div>}
            {(sel ? selRules : CAR.rules).map(r => {
              const on = enabled.includes(r.id);
              return <div key={r.id} onMouseEnter={() => setHover(r.id)} onMouseLeave={() => setHover(null)} style={{
                padding: "10px 12px", marginBottom: 8, borderRadius: "var(--radius-sm)", border: "1px solid var(--border-default)",
                background: hover === r.id ? "var(--bg-sunken)" : "var(--bg-surface)", opacity: on ? 1 : 0.55 }}>
                <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 5 }}>
                  <span style={{ fontFamily: "var(--font-mono)", fontSize: 10, fontWeight: 700, color: "#fff", background: r.type === "excludes" ? "var(--orange)" : "var(--teal)", borderRadius: 4, padding: "1px 5px" }}>{r.id}</span>
                  <span style={{ fontSize: "var(--text-2xs)", fontWeight: 600, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--text-muted)" }}>{r.type === "requiresOneOf" ? "requires one of" : r.type}</span>
                  <button onClick={() => onToggleRule(r.id)} title={on ? "Disable" : "Enable"} style={{ marginLeft: "auto", width: 34, height: 18, borderRadius: 9, border: "none", cursor: "pointer", background: on ? "var(--teal)" : "var(--grey-300)", position: "relative", flex: "none", transition: "background .15s" }}>
                    <span style={{ position: "absolute", top: 2, left: on ? 18 : 2, width: 14, height: 14, borderRadius: "50%", background: "#fff", transition: "left .15s" }} /></button>
                </div>
                <div style={{ fontSize: "var(--text-2xs)", color: "var(--text-secondary)", lineHeight: 1.45, marginBottom: 6 }}>{r.msg}</div>
                {r.type === "formula"
                  ? <EditableExpr value={r.expr} onCommit={v => { r.expr = v; onRefresh(); }} />
                  : <div style={{ display: "flex", alignItems: "center", gap: 5, flexWrap: "wrap", fontSize: 10 }}>
                  {(r.when || []).map(w => <span key={w} style={{ fontFamily: "var(--font-mono)", background: "var(--blue-50)", color: "var(--blue-700)", borderRadius: 3, padding: "1px 5px" }}>{w}</span>)}
                  <i className="bi bi-arrow-right" style={{ color: "var(--text-muted)" }} />
                  {(r.then || []).map(t => <span key={t} style={{ fontFamily: "var(--font-mono)", background: r.type === "excludes" ? "var(--orange-50)" : "var(--teal-50)", color: r.type === "excludes" ? "var(--orange-700)" : "var(--teal-700)", borderRadius: 3, padding: "1px 5px" }}>{t}</span>)}
                </div>}
              </div>;
            })}
          </div>
          <RuleBuilder onAddRule={onAddRule} onRefresh={onRefresh} />
        </aside>
      </div>
    );
  }

  function RuleBuilder({ onAddRule, onRefresh }) {
    const [open, setOpen] = useState(false);
    const [mode, setMode] = useState("requires"); // requires | excludes | formula | bounds
    const allOpts = CAR.groups.flatMap(g => g.options);
    const attrs = C.attributes;
    const [whenId, setWhenId] = useState(allOpts[0].id);
    const [thenId, setThenId] = useState(allOpts[3].id);
    const [expr, setExpr] = useState("reach <= 1400 || has('arm-20')");
    const [msg, setMsg] = useState("");
    const [attrId, setAttrId] = useState(attrs[0] ? attrs[0].id : "");
    const [lo, setLo] = useState(""); const [hi, setHi] = useState("");
    const sty = { width: "100%", boxSizing: "border-box", height: 30, borderRadius: "var(--radius-sm)", border: "1px solid var(--border-strong)", fontFamily: "var(--font-sans)", fontSize: "var(--text-xs)", padding: "0 8px", background: "#fff" };
    const mono = { ...sty, fontFamily: "var(--font-mono)", fontSize: 10 };
    const uid = () => "U" + (CAR.rules.length + 1);
    const fres = C.validateExpr(expr, { boolean: true });
    const canAdd = mode === "formula" ? fres.ok : (mode === "bounds" ? (lo !== "" || hi !== "") : true);
    if (!open) return <div style={{ padding: 12, borderTop: "var(--border)" }}><Button block variant="secondary" onClick={() => setOpen(true)} iconLeft={<i className="bi bi-plus-lg" />}>New rule</Button></div>;
    const add = () => {
      let rule;
      if (mode === "requires" || mode === "excludes")
        rule = { id: uid(), type: mode, when: [whenId], then: [thenId], msg: msg || `${C.opt(whenId).name} ${mode === "excludes" ? "excludes" : "requires"} ${C.opt(thenId).name}.` };
      else if (mode === "formula")
        rule = { id: uid(), type: "formula", expr: expr || "true", msg: msg || "Configuration constraint not met.", refs: [] };
      else {
        const a = [...attrs, ...CAR.calc].find(x => x.id === attrId) || { name: attrId };
        const parts = []; if (lo !== "") parts.push(`${attrId} >= ${lo}`); if (hi !== "") parts.push(`${attrId} <= ${hi}`);
        const auto = `${a.name} must be ${lo !== "" ? "\u2265 " + lo : ""}${lo !== "" && hi !== "" ? " and " : ""}${hi !== "" ? "\u2264 " + hi : ""}.`;
        rule = { id: uid(), type: "formula", expr: parts.join(" && ") || "true", msg: msg || auto, refs: [attrId] };
      }
      onAddRule(rule); setOpen(false); setMsg(""); setLo(""); setHi("");
    };
    return (
      <div style={{ padding: 14, borderTop: "var(--border)", background: "var(--bg-sunken)" }}>
        <div style={{ fontSize: "var(--text-2xs)", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.06em", color: "var(--text-muted)", marginBottom: 8 }}>New constraint</div>
        <div style={{ display: "flex", flexDirection: "column", gap: 7 }}>
          <select value={mode} onChange={e => setMode(e.target.value)} style={sty}>
            <option value="requires">Requires (if A then B)</option>
            <option value="excludes">Excludes (A blocks B)</option>
            <option value="formula">Formula (boolean expression)</option>
            <option value="bounds">Min / max bound on an attribute</option>
          </select>
          {(mode === "requires" || mode === "excludes") && <React.Fragment>
            <select value={whenId} onChange={e => setWhenId(e.target.value)} style={sty}>{allOpts.map(o => <option key={o.id} value={o.id}>{o.name} ({o.id})</option>)}</select>
            <select value={thenId} onChange={e => setThenId(e.target.value)} style={sty}>{allOpts.map(o => <option key={o.id} value={o.id}>{o.name} ({o.id})</option>)}</select>
          </React.Fragment>}
          {mode === "formula" && <React.Fragment>
            <input value={expr} onChange={e => setExpr(e.target.value)} spellCheck={false} placeholder="boolean expression, must be true" style={{ ...mono, borderColor: !fres.ok ? "var(--orange)" : "var(--border-strong)" }} />
            <div style={{ fontSize: 10, color: !fres.ok ? "var(--orange-700)" : "var(--text-muted)", lineHeight: 1.4 }}>{!fres.ok ? <span><i className="bi bi-exclamation-triangle-fill" /> {fres.error}</span> : <span>Use attributes (seats, rackLen…), calc values, <code style={{ fontFamily: "var(--font-mono)" }}>has('optId')</code>, ceil/floor/min/max. Must evaluate true.</span>}</div>
          </React.Fragment>}
          {mode === "bounds" && <React.Fragment>
            <select value={attrId} onChange={e => setAttrId(e.target.value)} style={sty}>{[...attrs, ...CAR.calc].map(a => <option key={a.id} value={a.id}>{a.name} ({a.id})</option>)}</select>
            <div style={{ display: "flex", gap: 6 }}>
              <input value={lo} onChange={e => setLo(e.target.value)} placeholder="min" style={{ ...sty, width: "50%" }} />
              <input value={hi} onChange={e => setHi(e.target.value)} placeholder="max" style={{ ...sty, width: "50%" }} />
            </div>
          </React.Fragment>}
          <input value={msg} onChange={e => setMsg(e.target.value)} placeholder="Validation message (optional)" style={sty} />
          <div style={{ display: "flex", gap: 7 }}>
            <Button variant="secondary" size="sm" onClick={() => setOpen(false)}>Cancel</Button>
            <Button size="sm" block disabled={!canAdd} onClick={add}>Add rule</Button>
          </div>
        </div>
      </div>
    );
  }

  window.AuthorView = AuthorView;
})();
