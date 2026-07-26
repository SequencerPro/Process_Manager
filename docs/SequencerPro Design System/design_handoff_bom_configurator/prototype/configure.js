/* ============================================================
   BoM Configurator — Configure (consuming) experience
   window.ConfigureView
   ============================================================ */
(function () {
  const { useState } = React;
  const DS = window.SequencerProDesignSystem_5eb90b;
  const { Button, StatusBadge } = DS;
  const C = window.CFG, CAR = window.CAR, fmt = C.fmt;

  function rulesFor(optId, enabled) {
    return CAR.rules.filter(r => (!enabled || enabled.includes(r.id)) && ((r.when || []).includes(optId) || (r.then || []).includes(optId)));
  }

  function OptionCard({ group, o, selected, disabled, reason, onChoose, enabled }) {
    const deps = rulesFor(o.id, enabled);
    const lead = o.swatch
      ? <span style={{ width: 28, height: 28, borderRadius: "50%", background: o.swatch, border: "1px solid var(--border-strong)", flex: "none" }} />
      : <span style={{ width: 28, height: 28, borderRadius: 6, background: selected ? "var(--gold-100)" : "var(--bg-sunken)", color: selected ? "var(--gold-700)" : "var(--text-muted)", display: "inline-flex", alignItems: "center", justifyContent: "center", flex: "none" }}><i className={`bi ${group.icon}`} /></span>;

    return (
      <button onClick={() => !disabled && onChoose(o.id)} disabled={disabled} title={disabled ? reason : undefined}
        style={{ display: "flex", alignItems: "center", gap: 12, width: "100%", textAlign: "left",
          padding: "11px 13px", borderRadius: "var(--radius-md)", cursor: disabled ? "not-allowed" : "pointer",
          background: selected ? "var(--gold-50)" : "var(--bg-surface)",
          border: selected ? "2px solid var(--gold)" : "1px solid var(--border-default)",
          margin: selected ? 0 : 1, opacity: disabled ? 0.55 : 1, transition: "var(--transition, all .15s)", position: "relative", fontFamily: "var(--font-sans)" }}>
        {lead}
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 7 }}>
            <span style={{ fontSize: "var(--text-sm)", fontWeight: 600, color: "var(--text-primary)" }}>{o.name}</span>
            {deps.length > 0 && <i className="bi bi-link-45deg" title={deps.map(d => d.msg).join("\n")} style={{ fontSize: 13, color: "var(--text-muted)" }} />}
          </div>
          {(o.sub || disabled) && <div style={{ fontSize: "var(--text-2xs)", color: disabled ? "var(--status-fail)" : "var(--text-muted)", marginTop: 1, lineHeight: 1.35 }}>
            {disabled ? <span><i className="bi bi-lock-fill" style={{ marginRight: 3 }} />{reason}</span> : o.sub}</div>}
        </div>
        <div style={{ display: "flex", flexDirection: "column", alignItems: "flex-end", gap: 4, flex: "none" }}>
          <span style={{ fontFamily: "var(--font-mono)", fontSize: "var(--text-xs)", fontWeight: 600, color: o.price ? "var(--text-primary)" : "var(--text-muted)" }}>
            {o.price ? "+" + fmt(o.price) : "Incl."}</span>
          <span style={{ width: 18, height: 18, borderRadius: group.multi ? 4 : "50%", flex: "none",
            border: selected ? "none" : "2px solid var(--border-strong)", background: selected ? "var(--gold)" : "transparent",
            display: "inline-flex", alignItems: "center", justifyContent: "center" }}>
            {selected && <i className="bi bi-check-lg" style={{ fontSize: 12, color: "var(--slate)" }} />}</span>
        </div>
      </button>
    );
  }

  function AttributesPanel({ sel, onAttr, onFix, violations, enabled }) {
    const attrs = C.attributes, calc = C.computeCalc(sel);
    const val = (id) => (sel.attrs && sel.attrs[id] != null) ? sel.attrs[id] : C.attrDef(id).default;
    const ruleById = (id) => CAR.rules.find(r => r.id === id);
    const attrViolations = (aid) => (violations || []).filter(v => { const r = ruleById(v.id); return r && (r.refs || []).includes(aid); });
    return (
      <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
        {attrs.map(a => {
          const vio = attrViolations(a.id);
          const bad = vio.length > 0;
          return (
          <div key={a.id} style={{ background: "var(--bg-surface)", border: `1px solid ${bad ? "var(--orange)" : "var(--border-default)"}`, borderRadius: "var(--radius-md)", padding: "13px 15px" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
              <span style={{ fontSize: "var(--text-sm)", fontWeight: 600 }}>{a.name}</span>
              <span style={{ fontFamily: "var(--font-mono)", fontSize: "var(--text-md)", fontWeight: 700, color: bad ? "var(--orange-700)" : "var(--text-primary)" }}>{val(a.id)} <span style={{ color: "var(--text-muted)", fontWeight: 400, fontSize: "var(--text-2xs)" }}>{a.unit}</span></span>
            </div>
            <div style={{ fontSize: "var(--text-2xs)", color: "var(--text-muted)", margin: "2px 0 11px" }}>{a.hint}</div>
            {a.kind === "choice"
              ? <div style={{ display: "flex", gap: 6 }}>{a.options.map(o => {
                  const on = val(a.id) === o;
                  return <button key={o} onClick={() => onAttr(a.id, o)} style={{ flex: 1, padding: "8px 0", borderRadius: "var(--radius-sm)", cursor: "pointer",
                    border: on ? "2px solid var(--gold)" : "1px solid var(--border-strong)", background: on ? "var(--gold-50)" : "var(--bg-surface)",
                    fontFamily: "var(--font-sans)", fontWeight: 600, fontSize: "var(--text-sm)" }}>{o}</button>;
                })}</div>
              : (() => {
                  const b = a.kind === "range" ? C.attrBounds(a.id, enabled) : { clamped: false };
                  const lo = (b.clamped && isFinite(b.min)) ? Math.max(a.min, b.min) : a.min;
                  const hi = (b.clamped && isFinite(b.max)) ? Math.min(a.max, b.max) : a.max;
                  const clampSet = (raw) => onAttr(a.id, Math.min(hi, Math.max(lo, raw)));
                  return <div>
                  <input type="range" min={lo} max={hi} step={a.step} value={Math.min(hi, Math.max(lo, val(a.id)))} onChange={e => clampSet(Number(e.target.value))} style={{ width: "100%", accentColor: bad ? "var(--orange)" : b.clamped ? "var(--blue)" : "var(--gold)" }} />
                  <div style={{ display: "flex", justifyContent: "space-between", fontSize: "var(--text-2xs)", color: "var(--text-muted)", fontFamily: "var(--font-mono)" }}><span>{lo}{a.unit ? " " + a.unit : ""}</span><span>{hi}{a.unit ? " " + a.unit : ""}</span></div>
                  {b.clamped && <div style={{ display: "flex", alignItems: "center", gap: 5, marginTop: 7, fontSize: "var(--text-2xs)", color: "var(--blue-700)" }}>
                    <i className="bi bi-lock-fill" /><span>Range limited to {isFinite(b.min) ? lo : "–∞"}–{isFinite(b.max) ? hi : "∞"}{a.unit ? " " + a.unit : ""} by rule {b.fromRules.join(", ")}</span>
                  </div>}
                </div>;
                })()}
            {bad && vio.map((v, i) => (
              <div key={i} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8, marginTop: 9, padding: "6px 9px", background: "var(--orange-50)", border: "1px solid var(--orange-200)", borderRadius: "var(--radius-sm)" }}>
                <span style={{ fontSize: "var(--text-2xs)", color: "var(--orange-700)", fontWeight: 600, display: "flex", alignItems: "center", gap: 5 }}><i className="bi bi-exclamation-triangle-fill" />{v.msg}</span>
                {v.fix && <button onClick={() => onFix(v)} style={{ flex: "none", border: "none", background: "var(--orange)", color: "#fff", borderRadius: "var(--radius-xs)", fontSize: "var(--text-2xs)", fontWeight: 700, padding: "3px 8px", cursor: "pointer", fontFamily: "var(--font-sans)" }}>{v.fixLabel}</button>}
              </div>
            ))}
          </div>
          );
        })}
        <div style={{ background: "var(--bg-sunken)", borderRadius: "var(--radius-md)", padding: "13px 15px", border: "1px dashed var(--border-strong)" }}>
          <div style={{ fontSize: "var(--text-2xs)", fontWeight: 700, letterSpacing: "var(--ls-caps)", textTransform: "uppercase", color: "var(--text-muted)", marginBottom: 10, display: "flex", alignItems: "center", gap: 6 }}>
            <i className="bi bi-calculator" />Calculated attributes</div>
          <div style={{ display: "flex", flexDirection: "column", gap: 9 }}>
            {calc.map(c => (
              <div key={c.id} style={{ display: "flex", alignItems: "center", gap: 10 }}>
                <span style={{ width: 120, fontSize: "var(--text-sm)", fontWeight: 600, flex: "none" }}>{c.name}</span>
                <code style={{ flex: 1, minWidth: 0, fontFamily: "var(--font-mono)", fontSize: "var(--text-2xs)", color: "var(--text-secondary)", background: "var(--bg-surface)", border: "1px solid var(--border-default)", borderRadius: "var(--radius-xs)", padding: "3px 7px", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{c.expr}</code>
                <span style={{ fontFamily: "var(--font-display)", fontSize: "var(--text-lg)", fontWeight: 800, color: "var(--gold-700)", width: 34, textAlign: "right", flex: "none" }}>{c.value}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  function ConfigureView({ sel, disabled, violations, notes, onChoose, onFix, onAttr, onCreateWO, enabled }) {
    const groups = C.applicableGroups(sel);
    const steps = [...groups, { id: "__dims", name: "Dimensions & Quantity", icon: "bi-rulers", dims: true }];
    const [stepId, setStepId] = useState(groups[0].id);
    const [tab, setTab] = useState("bom");
    let idx = steps.findIndex(s => s.id === stepId); if (idx < 0) idx = 0;
    const group = steps[idx];
    const st = C.status(sel, violations);

    const colorOpt = C.opt(sel.color), armOpt = C.opt(sel.arm), eeOpt = C.opt(sel.effector), ctlOpt = C.opt(sel.controller);
    const reachVal = (sel.attrs && sel.attrs.reach != null) ? sel.attrs.reach : 1300;
    const railVal = (sel.attrs && sel.attrs.railLen != null) ? sel.attrs.railLen : 0;
    const hasRail = C.isSel(sel, "pkg-track") && railVal > 0;

    const stepDone = (g) => g.dims ? true : (g.multi ? (sel[g.id] || []).length >= 0 : !!sel[g.id]);

    return (
      <div style={{ display: "grid", gridTemplateColumns: "210px 1fr 340px", height: "100%", overflow: "hidden" }}>
        {/* Stepper */}
        <nav style={{ borderRight: "var(--border)", background: "var(--bg-surface)", overflowY: "auto", padding: "14px 10px" }}>
          <div style={{ fontSize: "var(--text-2xs)", fontWeight: 700, letterSpacing: "var(--ls-caps)", textTransform: "uppercase", color: "var(--text-muted)", padding: "0 8px 8px" }}>Configuration</div>
          {steps.map((g, i) => {
            const on = g.id === stepId;
            return <button key={g.id} onClick={() => setStepId(g.id)} style={{
              display: "flex", alignItems: "center", gap: 10, width: "100%", padding: "9px 10px", border: "none",
              borderRadius: "var(--radius)", cursor: "pointer", background: on ? "var(--bg-sunken)" : "transparent",
              fontFamily: "var(--font-sans)", fontSize: "var(--text-sm)", fontWeight: on ? 600 : 500, color: on ? "var(--text-primary)" : "var(--text-secondary)", textAlign: "left", marginBottom: 2 }}>
              <span style={{ width: 22, height: 22, borderRadius: "50%", flex: "none", display: "inline-flex", alignItems: "center", justifyContent: "center",
                background: stepDone(g) ? "var(--teal)" : "var(--grey-200)", color: stepDone(g) ? "#fff" : "var(--text-muted)", fontSize: 11, fontWeight: 700 }}>
                {g.dims ? <i className="bi bi-rulers" /> : stepDone(g) ? <i className="bi bi-check-lg" /> : i + 1}</span>
              <span style={{ flex: 1, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{g.name}</span>
              {!g.dims && !g.multi && <span style={{ fontSize: "var(--text-2xs)", color: "var(--text-muted)", fontFamily: "var(--font-mono)" }}>{C.opt(sel[g.id]) ? "" : "—"}</span>}
            </button>;
          })}
        </nav>

        {/* Center: preview + options */}
        <div style={{ overflowY: "auto", background: "var(--bg-app)" }}>
          <div style={{ background: "linear-gradient(180deg, var(--bg-surface), var(--bg-app))", borderBottom: "var(--border)", padding: "18px 26px 6px" }}>
            <div style={{ maxWidth: 520, margin: "0 auto" }}><window.ProductPreview sel={sel} schematic={{ color: colorOpt ? colorOpt.swatch : "#e95b15", reach: reachVal, effector: eeOpt ? eeOpt.ee : "none", badge: armOpt ? armOpt.badge : "10 KG", hasRail: hasRail }} /></div>
            <div style={{ display: "flex", justifyContent: "center", gap: 7, flexWrap: "wrap", paddingBottom: 12 }}>
              {[armOpt, ctlOpt, eeOpt, colorOpt].filter(Boolean).map((o, i) =>
                <span key={i} style={{ fontSize: "var(--text-2xs)", fontWeight: 600, color: "var(--text-secondary)", background: "var(--bg-surface)", border: "var(--border)", borderRadius: "var(--radius-pill)", padding: "3px 10px" }}>{o.name}</span>)}
            </div>
          </div>

          <div style={{ padding: "20px 26px", maxWidth: 660, margin: "0 auto" }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 4 }}>
              <h2 style={{ fontSize: "var(--text-xl)", fontWeight: 700, margin: 0, display: "flex", alignItems: "center", gap: 9 }}>
                <i className={`bi ${group.icon}`} style={{ color: "var(--gold-600)" }} />{group.name}</h2>
              <span style={{ fontSize: "var(--text-2xs)", color: "var(--text-muted)", textTransform: "uppercase", letterSpacing: "0.06em" }}>{group.dims ? "Computed live" : group.multi ? "Select any" : "Select one"}</span>
            </div>
            <p style={{ fontSize: "var(--text-sm)", color: "var(--text-secondary)", margin: "0 0 16px" }}>{group.dims ? "Numeric attributes drive calculated values, BoM quantities and per-unit pricing by formula — watch the BoM update as you change them." : group.hint}</p>
            {group.dims
              ? <AttributesPanel sel={sel} onAttr={onAttr} onFix={onFix} violations={violations} enabled={enabled} />
              : <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              {group.options.map(o => <OptionCard key={o.id} group={group} o={o}
                selected={C.isSel(sel, o.id)} disabled={!!disabled[o.id] && !C.isSel(sel, o.id)} reason={disabled[o.id]}
                onChoose={onChoose} enabled={enabled} />)}
            </div>}
            <div style={{ display: "flex", justifyContent: "space-between", marginTop: 20 }}>
              <Button variant="secondary" disabled={idx === 0} onClick={() => setStepId(steps[Math.max(0, idx - 1)].id)} iconLeft={<i className="bi bi-arrow-left" />}>Back</Button>
              {idx < steps.length - 1
                ? <Button onClick={() => setStepId(steps[idx + 1].id)} iconRight={<i className="bi bi-arrow-right" />}>Next: {steps[idx + 1].name}</Button>
                : <Button variant="accent" onClick={onCreateWO} disabled={!st.complete} iconRight={<i className="bi bi-box-arrow-up-right" />}>Release work order</Button>}
            </div>
          </div>
        </div>

        {/* Right: output */}
        <aside style={{ borderLeft: "var(--border)", background: "var(--bg-surface)", display: "flex", flexDirection: "column", overflow: "hidden" }}>
          <div style={{ padding: "16px 18px 12px", borderBottom: "var(--border)" }}>
            <div style={{ fontSize: "var(--text-2xs)", fontWeight: 700, letterSpacing: "var(--ls-caps)", textTransform: "uppercase", color: "var(--text-muted)" }}>Total MSRP</div>
            <div style={{ fontFamily: "var(--font-display)", fontWeight: 800, fontSize: "var(--text-3xl)", color: "var(--text-primary)", lineHeight: 1.1 }}>{fmt(C.priceRollup(sel).total)}</div>
            <div style={{ display: "flex", alignItems: "center", gap: 8, marginTop: 6 }}>
              {st.complete ? <StatusBadge tone="pass">Buildable</StatusBadge> : violations.length ? <StatusBadge tone="fail">{violations.length} conflict{violations.length > 1 ? "s" : ""}</StatusBadge> : <StatusBadge tone="active">In progress</StatusBadge>}
              <span style={{ fontSize: "var(--text-2xs)", color: "var(--text-muted)" }}>{st.done}/{st.total} required set</span>
            </div>
          </div>
          <div style={{ display: "flex", gap: 2, padding: "8px 10px 0" }}>
            {[["bom", "BoM", "bi-diagram-3"], ["route", "Routing", "bi-signpost-split"], ["issues", "Issues", "bi-exclamation-triangle"]].map(([id, label, ic]) =>
              <button key={id} onClick={() => setTab(id)} style={{ flex: 1, padding: "7px 4px", border: "none", borderRadius: "var(--radius) var(--radius) 0 0", cursor: "pointer",
                background: tab === id ? "var(--bg-sunken)" : "transparent", fontFamily: "var(--font-sans)", fontSize: "var(--text-2xs)", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em",
                color: tab === id ? "var(--text-primary)" : "var(--text-muted)", display: "flex", alignItems: "center", justifyContent: "center", gap: 5 }}>
                <i className={`bi ${ic}`} />{label}{id === "issues" && violations.length > 0 && <span style={{ background: "var(--status-fail)", color: "#fff", borderRadius: 8, fontSize: 9, padding: "0 5px", marginLeft: 2 }}>{violations.length}</span>}</button>)}
          </div>
          <div style={{ flex: 1, overflowY: "auto", padding: "14px 16px", background: "var(--bg-sunken)" }}>
            {tab === "bom" && <window.BomTree sel={sel} />}
            {tab === "route" && <window.Routing sel={sel} />}
            {tab === "issues" && <window.ViolationsList violations={violations} onFix={onFix} />}
            {notes && notes.length > 0 && tab !== "issues" && <div style={{ marginTop: 12, padding: "8px 10px", background: "var(--blue-50)", border: "1px solid var(--blue-100)", borderRadius: "var(--radius-sm)", fontSize: "var(--text-2xs)", color: "var(--blue-700)" }}>
              <i className="bi bi-info-circle" style={{ marginRight: 4 }} />{notes[notes.length - 1]}</div>}
          </div>
          <div style={{ padding: 14, borderTop: "var(--border)" }}>
            <Button block variant="accent" disabled={!st.complete} onClick={onCreateWO} iconLeft={<i className="bi bi-box-arrow-up-right" />}>Release work order</Button>
          </div>
        </aside>
      </div>
    );
  }

  window.ConfigureView = ConfigureView;
})();
