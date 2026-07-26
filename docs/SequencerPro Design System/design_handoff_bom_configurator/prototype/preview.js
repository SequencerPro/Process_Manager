/* ============================================================
   BoM Configurator — product preview + image authoring
   window.ProductPreview  — composites author images, else schematic
   window.PreviewAuthor    — the authoring panel (mode: "preview")
   ============================================================ */
(function () {
  const { useState, useRef, useEffect } = React;
  const DS = window.SequencerProDesignSystem_5eb90b;
  const { Button } = DS;
  const C = window.CFG, CAR = window.CAR;
  const PS = window.PreviewStore;

  // Re-render whenever the image store changes.
  function usePreviewStore() {
    const [, set] = useState(0);
    useEffect(() => PS.subscribe(() => set(n => n + 1)), []);
    return PS;
  }

  // Collect overlay srcs for the selected options, in group order (z-stack).
  function overlaysFor(sel) {
    const out = [];
    CAR.groups.forEach(g => {
      const ids = g.multi ? (sel[g.id] || []) : (sel[g.id] ? [sel[g.id]] : []);
      ids.forEach(id => { const src = PS.overlay(id); if (src) out.push({ id, src }); });
    });
    return out;
  }

  // ── Live composited product preview (falls back to the schematic car) ──
  function ProductPreview({ sel, schematic }) {
    usePreviewStore();
    const base = PS.base();
    if (!base) {
      // No author images yet → built-in schematic example.
      return React.createElement(window.SchematicRobot, schematic);
    }
    const overlays = overlaysFor(sel);
    return React.createElement("div", { style: { position: "relative", width: "100%", aspectRatio: String(PS.aspect()), maxWidth: 520, margin: "0 auto" } },
      React.createElement("img", { src: base, alt: "Product", style: { position: "absolute", inset: 0, width: "100%", height: "100%", objectFit: "contain" } }),
      overlays.map(o => React.createElement("img", { key: o.id, src: o.src, alt: "", style: { position: "absolute", inset: 0, width: "100%", height: "100%", objectFit: "contain", pointerEvents: "none" } })));
  }

  // ── Reusable image drop / upload slot ──
  function ImageSlot({ src, onPick, onClear, label, height = 92, hint }) {
    const inputRef = useRef(null);
    const [over, setOver] = useState(false);
    const [busy, setBusy] = useState(false);
    const take = async (file, maxDim) => {
      if (!file) return;
      setBusy(true);
      try { const rec = await PS.fileToDataUrl(file, maxDim); onPick(rec); }
      catch (e) { console.warn(e); alert("Could not read that image."); }
      setBusy(false);
    };
    return React.createElement("div", {
      onDragOver: e => { e.preventDefault(); setOver(true); },
      onDragLeave: () => setOver(false),
      onDrop: e => { e.preventDefault(); setOver(false); take(e.dataTransfer.files[0], label === "base" ? 1000 : 1000); },
      onClick: () => inputRef.current && inputRef.current.click(),
      style: {
        position: "relative", height, borderRadius: "var(--radius)", cursor: "pointer", overflow: "hidden",
        border: over ? "2px dashed var(--gold)" : src ? "1px solid var(--border-default)" : "1.5px dashed var(--border-strong)",
        background: src ? "var(--bg-sunken)" : over ? "var(--gold-50)" : "var(--bg-surface)",
        display: "flex", alignItems: "center", justifyContent: "center", transition: "border-color .12s, background .12s" } },
      React.createElement("input", { ref: inputRef, type: "file", accept: "image/*", style: { display: "none" },
        onChange: e => { take(e.target.files[0], 1000); e.target.value = ""; } }),
      src
        ? React.createElement(React.Fragment, null,
            React.createElement("img", { src, alt: "", style: { maxWidth: "100%", maxHeight: "100%", objectFit: "contain" } }),
            React.createElement("button", { onClick: e => { e.stopPropagation(); onClear(); }, title: "Remove image", style: {
              position: "absolute", top: 4, right: 4, width: 20, height: 20, borderRadius: "50%", border: "none",
              background: "rgba(33,33,33,0.7)", color: "#fff", cursor: "pointer", fontSize: 12, lineHeight: 1, display: "grid", placeItems: "center" } }, "×"))
        : React.createElement("div", { style: { textAlign: "center", color: "var(--text-muted)", pointerEvents: "none", padding: 6 } },
            React.createElement("i", { className: "bi " + (busy ? "bi-hourglass-split" : "bi-image"), style: { fontSize: 18, display: "block", marginBottom: 3 } }),
            React.createElement("div", { style: { fontSize: "var(--text-2xs)", lineHeight: 1.3 } }, busy ? "Loading…" : (hint || "Drop image or click"))));
  }

  // ── One option's overlay slot (label + swatch under a drop slot) ──
  function OverlayCard({ o }) {
    return React.createElement("div", null,
      React.createElement(ImageSlot, { src: PS.overlay(o.id), label: o.id, height: 84, hint: "Overlay image",
        onPick: rec => PS.setOverlay(o.id, rec), onClear: () => PS.remove(o.id) }),
      React.createElement("div", { style: { display: "flex", alignItems: "center", gap: 5, marginTop: 4 } },
        o.swatch ? React.createElement("span", { style: { width: 9, height: 9, borderRadius: "50%", background: o.swatch, border: "1px solid rgba(0,0,0,0.1)", flex: "none" } }) : null,
        React.createElement("span", { style: { fontSize: "var(--text-2xs)", color: "var(--text-secondary)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" } }, o.name)));
  }

  // ── One option group: header + grid of overlay slots ──
  function GroupOverlays({ g }) {
    return React.createElement("div", { style: { marginBottom: 22 } },
      React.createElement("div", { style: { display: "flex", alignItems: "center", gap: 8, marginBottom: 9, paddingBottom: 6, borderBottom: "var(--border)" } },
        React.createElement("i", { className: "bi " + (g.icon || "bi-dot"), style: { color: "var(--gold-600)" } }),
        React.createElement("span", { style: { fontWeight: 700, fontSize: "var(--text-md)" } }, g.name),
        React.createElement("span", { style: { fontSize: "var(--text-2xs)", color: "var(--text-muted)", marginLeft: "auto", textTransform: "uppercase", letterSpacing: "0.05em" } }, "Overlay when selected")),
      React.createElement("div", { style: { display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(150px, 1fr))", gap: 10 } },
        g.options.map(o => React.createElement(OverlayCard, { key: o.id, o }))));
  }

  // ── Authoring panel: base image + per-option overlays ──
  function PreviewAuthor() {
    usePreviewStore();
    const sample = React.useMemo(() => C.defaultSelections(), []);
    const base = PS.base();
    return React.createElement("div", { style: { height: "100%", overflow: "auto", background: "var(--bg-app)" } },
      React.createElement("div", { style: { maxWidth: 960, margin: "0 auto", padding: "22px 26px 60px", display: "grid", gridTemplateColumns: "320px 1fr", gap: 26, alignItems: "start" } },

        // Left: live preview + base + intro
        React.createElement("div", { style: { position: "sticky", top: 22 } },
          React.createElement("h2", { style: { fontSize: "var(--text-xl)", fontWeight: 700, margin: "0 0 4px", display: "flex", alignItems: "center", gap: 9 } },
            React.createElement("i", { className: "bi bi-images", style: { color: "var(--gold-600)" } }), "Preview images"),
          React.createElement("p", { style: { fontSize: "var(--text-sm)", color: "var(--text-secondary)", margin: "0 0 14px", lineHeight: 1.5 } },
            "The schematic robot is only a placeholder. Upload images of ", React.createElement("em", null, "your"), " product — photos or renders — and the preview composites them live as options are picked."),
          React.createElement("div", { style: { background: "var(--bg-surface)", border: "var(--border)", borderRadius: "var(--radius-md)", padding: 14, boxShadow: "var(--shadow-xs)" } },
            React.createElement("div", { style: { fontSize: "var(--text-2xs)", fontWeight: 700, textTransform: "uppercase", letterSpacing: "var(--ls-caps)", color: "var(--text-muted)", marginBottom: 8 } }, "Live composite"),
            React.createElement("div", { style: { background: "var(--bg-sunken)", borderRadius: "var(--radius)", padding: 10, marginBottom: 14 } },
              React.createElement(ProductPreview, { sel: sample, schematic: { color: "#e95b15", badge: "10 KG" } })),
            React.createElement("div", { style: { fontSize: "var(--text-2xs)", fontWeight: 700, textTransform: "uppercase", letterSpacing: "var(--ls-caps)", color: "var(--text-muted)", marginBottom: 6 } }, "Base image"),
            React.createElement(ImageSlot, { src: base, label: "base", height: 120, hint: "Drop base product image",
              onPick: rec => PS.setBase(rec), onClear: () => PS.remove("__base__") }),
            React.createElement("p", { style: { fontSize: "var(--text-2xs)", color: "var(--text-muted)", margin: "8px 0 0", lineHeight: 1.4 } },
              "Always-visible bottom layer. Sets the preview's aspect ratio. Overlays should share its dimensions to line up."),
            PS.hasImages() ? React.createElement("button", { onClick: () => { if (confirm("Remove all preview images and revert to the schematic?")) PS.clearAll(); }, style: {
              marginTop: 12, width: "100%", padding: "7px 0", border: "1px solid var(--border-strong)", borderRadius: "var(--radius-sm)",
              background: "var(--bg-surface)", color: "var(--status-fail)", cursor: "pointer", fontFamily: "var(--font-sans)", fontSize: "var(--text-xs)", fontWeight: 600 } },
              React.createElement("i", { className: "bi bi-trash", style: { marginRight: 5 } }), "Clear all images") : null)),

        // Right: per-option overlay slots
        React.createElement("div", null,
          !base ? React.createElement("div", { style: { display: "flex", gap: 9, alignItems: "flex-start", background: "var(--gold-50)", border: "1px solid var(--gold-200)", borderRadius: "var(--radius)", padding: "10px 13px", marginBottom: 16, fontSize: "var(--text-xs)", color: "var(--gold-800)" } },
            React.createElement("i", { className: "bi bi-info-circle-fill", style: { marginTop: 1 } }),
            React.createElement("span", null, "Add a base image first. Until then, the configurator shows the built-in schematic example.")) : null,
          CAR.groups.map(g => React.createElement(GroupOverlays, { key: g.id, g })))));
  }

  Object.assign(window, { ProductPreview, PreviewAuthor, PreviewImageSlot: ImageSlot });
})();
