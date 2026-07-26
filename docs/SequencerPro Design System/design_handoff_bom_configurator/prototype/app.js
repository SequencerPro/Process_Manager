/* ============================================================
   BoM Configurator — app shell (mode switch + state wiring)
   ============================================================ */
(function () {
  const { useState, useMemo, useEffect } = React;

  function ModeTab({ id, mode, setMode, icon, children }) {
    const on = mode === id;
    return <button onClick={() => setMode(id)} style={{
      display: "inline-flex", alignItems: "center", gap: 7, padding: "7px 16px", border: "none", cursor: "pointer",
      borderRadius: "var(--radius)", fontFamily: "var(--font-sans)", fontSize: "var(--text-sm)", fontWeight: 600,
      background: on ? "var(--slate)" : "transparent", color: on ? "var(--white)" : "var(--text-secondary)", transition: "background .15s" }}>
      <i className={`bi ${icon}`} />{children}</button>;
  }

  function App() {
    const C = window.CFG, CAR = window.CAR;
    const { NodeMark, Button } = window.SequencerProDesignSystem_5eb90b;
    const [mode, setMode] = useState("configure");
    const [rawSel, setRawSel] = useState(() => C.defaultSelections());
    const [enabled, setEnabled] = useState(() => CAR.rules.map(r => r.id));
    const [woOpen, setWoOpen] = useState(false);
    const [printJob, setPrintJob] = useState(null);
    const [, setBump] = useState(0);

    // When a print job is queued, let the off-screen sheet render, then open the
    // browser print dialog (Save as PDF). Clear after printing.
    useEffect(() => {
      if (!printJob) return;
      const id = requestAnimationFrame(() => requestAnimationFrame(() => { window.print(); }));
      const after = () => setPrintJob(null);
      window.addEventListener("afterprint", after);
      return () => { cancelAnimationFrame(id); window.removeEventListener("afterprint", after); };
    }, [printJob]);

    const rec = useMemo(() => C.reconcile(rawSel, enabled), [rawSel, enabled]);
    const sel = rec.sel;

    const onChoose = (id) => setRawSel(C.applyChoice(sel, id));
    const onAttr = (id, v) => setRawSel(C.setAttr(sel, id, v));
    const onFix = (v) => setRawSel(v.fix(sel));
    const onToggleRule = (rid) => setEnabled(e => e.includes(rid) ? e.filter(x => x !== rid) : [...e, rid]);
    const onAddRule = (rule) => { CAR.rules.push(rule); setEnabled(e => [...e, rule.id]); setBump(b => b + 1); };
    const reset = () => { setRawSel(C.defaultSelections()); };

    const st = C.status(sel, rec.violations);

    return (
      <div style={{ display: "flex", flexDirection: "column", height: "100vh", overflow: "hidden", background: "var(--bg-app)" }}>
        <header style={{ height: 58, flex: "none", display: "flex", alignItems: "center", gap: 16, padding: "0 20px", background: "var(--bg-surface)", borderBottom: "var(--border)" }}>
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <NodeMark size={30} />
            <div>
              <div style={{ fontSize: "var(--text-md)", fontWeight: 700, lineHeight: 1.1 }}>Platform Configurator</div>
              <div style={{ fontSize: "var(--text-2xs)", color: "var(--text-muted)" }}>{CAR.platform.name} · {CAR.platform.code}</div>
            </div>
          </div>
          <div style={{ display: "flex", gap: 2, background: "var(--bg-sunken)", borderRadius: "var(--radius)", padding: 3, marginLeft: 12 }}>
            <ModeTab id="configure" mode={mode} setMode={setMode} icon="bi-sliders2">Configure</ModeTab>
            <ModeTab id="author" mode={mode} setMode={setMode} icon="bi-diagram-3">Author rules</ModeTab>
            <ModeTab id="preview" mode={mode} setMode={setMode} icon="bi-images">Preview images</ModeTab>
            <ModeTab id="tree" mode={mode} setMode={setMode} icon="bi-diagram-2">Product tree</ModeTab>
          </div>
          <div style={{ marginLeft: "auto", display: "flex", alignItems: "center", gap: 12 }}>
            {mode === "configure" && <span style={{ fontSize: "var(--text-xs)", color: "var(--text-secondary)" }}>
              <span style={{ fontFamily: "var(--font-mono)", fontWeight: 700, color: "var(--text-primary)" }}>{C.fmt(C.priceRollup(sel).total)}</span> MSRP</span>}
            <Button variant="ghost" size="sm" onClick={reset} iconLeft={<i className="bi bi-arrow-counterclockwise" />}>Reset</Button>
          </div>
        </header>

        <main style={{ flex: 1, overflow: "hidden" }}>
          {mode === "configure"
            ? <window.ConfigureView sel={sel} disabled={rec.disabled} violations={rec.violations} notes={rec.notes}
                onChoose={onChoose} onFix={onFix} onAttr={onAttr} onCreateWO={() => setWoOpen(true)} enabled={enabled} />
            : mode === "preview"
            ? <window.PreviewAuthor />
            : mode === "tree"
            ? <window.ProductTree sel={sel} />
            : <window.AuthorView enabled={enabled} onToggleRule={onToggleRule} onAddRule={onAddRule} onRefresh={() => setBump(b => b + 1)} />}
        </main>

        {woOpen && <window.WorkOrderModal sel={sel} enabled={enabled} onClose={() => setWoOpen(false)} onPrint={(spec, wo) => setPrintJob({ spec, wo })} />}

        {printJob && ReactDOM.createPortal(
          <div id="print-root"><window.SpecSheet spec={printJob.spec} wo={printJob.wo} /></div>,
          document.body
        )}
      </div>
    );
  }

  // Expose App for the page's inline mount script. NOTE: no auto-mount here —
  // this file is also concatenated into _ds_bundle.js, and auto-mounting would
  // render the configurator onto any DS page that has a #root. The configurator
  // page (index.html) mounts <BomApp> into #bom-app explicitly.
  window.BomApp = App;
})();
