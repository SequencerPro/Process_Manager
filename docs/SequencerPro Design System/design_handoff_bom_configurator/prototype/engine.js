/* ============================================================
   BoM Configurator — constraint engine (pure logic)
   window.CFG
   ============================================================ */
(function () {
  const CAR = window.CAR;
  const opt = (id) => { for (const g of CAR.groups) { const o = g.options.find(o => o.id === id); if (o) return o; } return null; };
  const groupOf = (id) => CAR.groups.find(g => g.options.some(o => o.id === id));
  const groupById = (gid) => CAR.groups.find(g => g.id === gid);

  // ── Formula / calculated-attribute engine (D365-style) ─────────────────────
  const ATTRS = CAR.attributes || [];
  const CALC = CAR.calc || [];
  const HELPERS = { ceil: Math.ceil, floor: Math.floor, round: Math.round, abs: Math.abs,
    min: Math.min, max: Math.max, roundup: Math.ceil, rounddown: Math.floor };
  const attrName = (id) => { const a = ATTRS.find(x => x.id === id); return a ? a.name : id; };
  const attrDef  = (id) => ATTRS.find(x => x.id === id);

  // Build the evaluation scope: helpers + has('optId') + each attribute value +
  // each calculated value (resolved in declaration order so later calc can use earlier).
  function context(sel) {
    const scope = Object.assign({}, HELPERS);
    scope.has = (id) => isSel(sel, id);
    const attrs = (sel && sel.attrs) || {};
    for (const a of ATTRS) scope[a.id] = Number(attrs[a.id] != null ? attrs[a.id] : a.default);
    const calcVals = {};
    for (const c of CALC) { const v = evalExpr(c.expr, scope); scope[c.id] = v; calcVals[c.id] = v; }
    return { scope, calcVals };
  }

  // Safe-ish expression eval over a fixed scope (prototype). Non-finite -> 0.
  function evalExpr(expr, scope) {
    try {
      const keys = Object.keys(scope);
      const fn = new Function(...keys, "return (" + expr + ");");
      const v = fn(...keys.map(k => scope[k]));
      return (typeof v === "number" && !isFinite(v)) ? 0 : v;
    } catch (e) { return 0; }
  }

  // Calculated attributes with their resolved values (for the UI read-outs).
  function computeCalc(sel) {
    const { scope } = context(sel);
    return CALC.map(c => ({ id: c.id, name: c.name, unit: c.unit || "", expr: c.expr, hint: c.hint, value: evalExpr(c.expr, scope) }));
  }

  // Validate an authored expression BEFORE it's saved: reports syntax errors and
  // any identifier that isn't a known attribute / calculated value / helper.
  // Returns { ok, error, unknown:[...], value } — value is a sample evaluation.
  function knownIdents() {
    const k = new Set(["has", "true", "false", "null", "Math", "if", "else", "return"]);
    Object.keys(HELPERS).forEach(x => k.add(x));
    ATTRS.forEach(a => k.add(a.id));
    CALC.forEach(c => k.add(c.id));
    return k;
  }
  function validateExpr(expr, opts) {
    opts = opts || {};
    const out = { ok: true, error: "", unknown: [], value: undefined };
    const src = String(expr || "").trim();
    if (!src) { out.ok = false; out.error = "Empty expression"; return out; }
    // strip string + number literals so their contents aren't read as identifiers
    const stripped = src.replace(/'[^']*'|"[^"]*"/g, " ").replace(/\b\d+(\.\d+)?\b/g, " ");
    // an identifier preceded by '.' is a property access (e.g. Math.ceil) — drop those
    const idents = (stripped.match(/(?:\.)?[A-Za-z_$][A-Za-z0-9_$]*/g) || [])
      .filter(t => t[0] !== ".");
    const known = knownIdents();
    const unknown = [...new Set(idents.filter(id => !known.has(id)))];
    if (unknown.length) { out.ok = false; out.unknown = unknown; out.error = "Unknown: " + unknown.join(", "); }
    // syntax check via compile + sample eval against default selection
    try {
      const { scope } = context(defaultSelections());
      const keys = Object.keys(scope), fn = new Function(...keys, "return (" + src + ");");
      const v = fn(...keys.map(k => scope[k]));
      out.value = v;
      if (opts.boolean && typeof v !== "boolean") { /* allow truthy, just note */ }
    } catch (e) {
      out.ok = false; out.error = (out.unknown.length ? out.error + " · " : "") + "Syntax error";
    }
    return out;
  }

  function setAttr(sel, id, val) { sel = clone(sel); sel.attrs = Object.assign({}, sel.attrs, { [id]: val }); return sel; }

  // Derive the effective [min,max] for a numeric attribute from active bound rules.
  // Recognises simple comparisons that reference ONLY this attribute, e.g.
  // "rackLen <= 180", "seats >= 4", "orderQty < 20". Combines with the attribute's
  // own declared min/max (tightest wins). Returns { min, max, fromRules:[ruleIds] }.
  function attrBounds(attrId, enabledRuleIds) {
    const a = attrDef(attrId) || {};
    let lo = (a.min != null) ? a.min : -Infinity;
    let hi = (a.max != null) ? a.max : Infinity;
    const fromRules = [];
    const rules = CAR.rules.filter(r => r.type === "formula" && (!enabledRuleIds || enabledRuleIds.includes(r.id)));
    const re = /^\s*([A-Za-z_$][\w$]*)\s*(<=|>=|<|>)\s*(-?\d+(?:\.\d+)?)\s*$/;
    for (const r of rules) {
      // each &&-joined clause may carry one bound on this attribute
      for (const clause of String(r.expr).split("&&")) {
        const m = clause.match(re);
        if (!m || m[1] !== attrId) continue;
        const op = m[2], n = Number(m[3]), step = a.step || 1;
        if (op === "<=") hi = Math.min(hi, n);
        else if (op === "<") hi = Math.min(hi, n - step);
        else if (op === ">=") lo = Math.max(lo, n);
        else if (op === ">") lo = Math.max(lo, n + step);
        if (!fromRules.includes(r.id)) fromRules.push(r.id);
      }
    }
    return { min: lo, max: hi, fromRules, clamped: fromRules.length > 0 };
  }

  // selection helpers — sel: { groupId: optionId | [optionIds] }
  const isSel = (sel, id) => { const g = groupOf(id); if (!g) return false; const v = sel[g.id]; return g.multi ? (v || []).includes(id) : v === id; };
  const allSel = (sel, ids) => ids.every(id => isSel(sel, id));
  const anySel = (sel, ids) => ids.some(id => isSel(sel, id));

  // Which groups are applicable given current selections (e.g. battery only for EV)
  function applicableGroups(sel) {
    return CAR.groups.filter(g => !g.appliesIf || anySel(sel, g.appliesIf));
  }

  const defaultSelections = () => {
    const sel = {};
    for (const g of CAR.groups) {
      if (g.multi) sel[g.id] = [];
      else if (g.required) sel[g.id] = g.options[0].id;
      else sel[g.id] = null;
    }
    sel.attrs = {};
    for (const a of ATTRS) sel.attrs[a.id] = a.default;
    return sel;
  };

  /* Reconcile: apply active rules to the selection.
     Returns { sel, disabled, violations, notes } where
       disabled: { optionId: reasonString }
       violations: [{ id, msg, fixLabel, fix(sel)->sel }]
       notes: auto-corrections that were applied (strings)        */
  function reconcile(sel, enabledRuleIds) {
    sel = clone(sel);
    const rules = CAR.rules.filter(r => !enabledRuleIds || enabledRuleIds.includes(r.id));
    const disabled = {};
    const violations = [];
    const notes = [];

    // Clear selections in non-applicable groups (e.g. battery when not EV)
    const appIds = applicableGroups(sel).map(g => g.id);
    for (const g of CAR.groups) if (!appIds.includes(g.id)) sel[g.id] = g.multi ? [] : null;

    // Iterate to a fixpoint (auto-corrections can cascade), capped.
    for (let pass = 0; pass < 6; pass++) {
      let changed = false;
      for (const r of rules) {
        if (r.type === "formula" || !r.when || !allSel(sel, r.when)) continue;

        if (r.type === "requires") {
          for (const tid of r.then) {
            const tg = groupOf(tid);
            if (!tg) continue;
            if (tg.multi) {
              if (!isSel(sel, tid)) {
                violations.push({ id: r.id, msg: r.msg, fixLabel: `Add ${opt(tid).name}`,
                  fix: (s) => { s = clone(s); if (!s[tg.id].includes(tid)) s[tg.id] = [...s[tg.id], tid]; return s; } });
              }
            } else {
              // single-select: force the required option; disable siblings
              for (const o of tg.options) if (o.id !== tid) disabled[o.id] = r.msg;
              if (sel[tg.id] !== tid && appIds.includes(tg.id)) {
                sel[tg.id] = tid; changed = true;
                notes.push(`${r.msg} — set ${tg.name} to ${opt(tid).name}.`);
              }
            }
          }
        } else if (r.type === "requiresOneOf") {
          if (!anySel(sel, r.then)) {
            const first = r.then[0];
            violations.push({ id: r.id, msg: r.msg, fixLabel: `Choose ${opt(first).name}`,
              fix: (s) => applyChoice(s, first) });
          }
        } else if (r.type === "excludes") {
          // disable the 'then' options while 'when' active; flag if already on
          for (const tid of r.then) {
            disabled[tid] = r.msg;
            if (isSel(sel, tid)) {
              const tg = groupOf(tid);
              violations.push({ id: r.id, msg: r.msg, fixLabel: `Remove ${opt(tid).name}`,
                fix: (s) => { s = clone(s); if (tg.multi) s[tg.id] = s[tg.id].filter(x => x !== tid); else s[tg.id] = tg.required ? tg.options[0].id : null; return s; } });
            }
          }
        }
      }
      if (!changed) break;
    }

    // Formula constraints: the expression must evaluate truthy, else it's a violation.
    for (const r of rules) {
      if (r.type !== "formula") continue;
      const { scope } = context(sel);
      if (!evalExpr(r.expr, scope)) {
        const v = { id: r.id, msg: r.msg };
        if (r.fixChoose) { v.fixLabel = `Choose ${opt(r.fixChoose).name}`; v.fix = (s) => applyChoice(s, r.fixChoose); }
        else if (r.fixAttr) { v.fixLabel = `Set ${attrName(r.fixAttr.id)} = ${r.fixAttr.value}`; v.fix = (s) => setAttr(s, r.fixAttr.id, r.fixAttr.value); }
        else { v.fixLabel = "Review"; v.fix = (s) => s; }
        violations.push(v);
      }
    }

    // de-dupe violations by id+msg
    const seen = new Set();
    const uniqV = violations.filter(v => { const k = v.id + v.msg; if (seen.has(k)) return false; seen.add(k); return true; });
    return { sel, disabled, violations: uniqV, notes };
  }

  // Apply a single choice (respecting single/multi) WITHOUT reconcile.
  function applyChoice(sel, id) {
    sel = clone(sel);
    const g = groupOf(id);
    if (!g) return sel;
    if (g.multi) sel[g.id] = sel[g.id].includes(id) ? sel[g.id].filter(x => x !== id) : [...sel[g.id], id];
    else sel[g.id] = id;
    return sel;
  }

  // Resolve the multi-level BoM grouped by system. Quantities may be formula-driven
  // (qtyExpr) and are evaluated against the current attribute/selection context.
  function resolveBom(sel) {
    const { scope } = context(sel);
    const qv = (l) => l.qtyExpr ? Math.max(0, Math.round(evalExpr(l.qtyExpr, scope))) : l.qty;
    const resolve = (l) => Object.assign({}, l, { qty: qv(l), children: (l.children || []).map(c => Object.assign({}, c, { qty: qv(c) })) });

    const lines = CAR.baseBom.map(b => Object.assign(resolve(b), { source: "Platform" }));
    for (const g of applicableGroups(sel)) {
      const chosen = g.multi ? sel[g.id] : (sel[g.id] ? [sel[g.id]] : []);
      for (const id of chosen) {
        const o = opt(id);
        if (o && o.bom) for (const b of o.bom) lines.push(Object.assign(resolve(b), { source: o.name }));
      }
    }
    const bySystem = {};
    for (const l of lines) { (bySystem[l.system] = bySystem[l.system] || []).push(l); }
    const lineTotal = (l) => l.price * l.qty + (l.children || []).reduce((s, c) => s + c.price * c.qty, 0);
    const systems = Object.keys(bySystem).map(name => ({
      name,
      assemblies: bySystem[name],
      total: bySystem[name].reduce((s, l) => s + lineTotal(l), 0),
    }));
    const partCount = lines.reduce((s, l) => s + l.qty + (l.children || []).reduce((t, c) => t + c.qty, 0), 0);
    const cost = systems.reduce((s, x) => s + x.total, 0);
    return { systems, partCount, cost, lineTotal };
  }

  // Price rollup: base + each option delta (priceExpr overrides a fixed price).
  function priceRollup(sel) {
    const { scope } = context(sel);
    const lines = [{ label: `${CAR.platform.name} base`, amount: CAR.platform.basePrice, base: true }];
    for (const g of applicableGroups(sel)) {
      const chosen = g.multi ? sel[g.id] : (sel[g.id] ? [sel[g.id]] : []);
      for (const id of chosen) {
        const o = opt(id);
        if (!o) continue;
        const amt = o.priceExpr ? Math.round(evalExpr(o.priceExpr, scope)) : o.price;
        if (amt > 0) lines.push({ label: o.name, amount: amt, group: g.name, formula: !!o.priceExpr });
      }
    }
    const total = lines.reduce((s, l) => s + l.amount, 0);
    return { lines, total };
  }

  // Generate the routing (ordered, de-duped operations) — ties to Process Manager.
  function generateRouting(sel) {
    const ops = [...CAR.baseOps];
    for (const g of applicableGroups(sel)) {
      const chosen = g.multi ? sel[g.id] : (sel[g.id] ? [sel[g.id]] : []);
      for (const id of chosen) { const o = opt(id); if (o && o.ops) ops.push(...o.ops); }
    }
    const map = {};
    for (const o of ops) map[o.code] = o; // de-dupe by code
    return Object.values(map).sort((a, b) => a.seq - b.seq);
  }

  // Completion: every required & applicable group has a selection, no violations.
  function status(sel, violations) {
    const need = applicableGroups(sel).filter(g => g.required);
    const done = need.filter(g => sel[g.id]).length;
    return { done, total: need.length, complete: done === need.length && violations.length === 0 };
  }

  function clone(o) { return JSON.parse(JSON.stringify(o)); }
  // ── Resolved export: the full, serialisable spec behind a released work order ──
  // Surfaces everything the configurator computed: attribute values & their bounds,
  // calculated attributes with formulas, formula-driven BoM quantities, per-unit
  // price formulas, the order-quantity rollup, the active rule set, and routing.
  function exportSpec(sel, enabled) {
    const ctx = context(sel);
    const oq = Number((sel.attrs && sel.attrs.orderQty) || 1);
    const bom = resolveBom(sel);
    const price = priceRollup(sel);

    const attributes = ATTRS.map(a => {
      const b = a.kind === "range" ? attrBounds(a.id, enabled) : { clamped: false };
      return { id: a.id, name: a.name, value: Number((sel.attrs && sel.attrs[a.id] != null) ? sel.attrs[a.id] : a.default),
        unit: a.unit || "", kind: a.kind,
        bounds: a.kind === "range" ? { min: isFinite(b.min) ? b.min : a.min, max: isFinite(b.max) ? b.max : a.max, clampedBy: b.fromRules || [] } : null };
    });
    const calculated = CALC.map(c => ({ id: c.id, name: c.name, expr: c.expr, unit: c.unit || "", value: evalExpr(c.expr, ctx.scope) }));

    const selections = [];
    for (const g of applicableGroups(sel)) {
      const ids = g.multi ? (sel[g.id] || []) : (sel[g.id] ? [sel[g.id]] : []);
      ids.forEach(id => { const o = opt(id); if (o) selections.push({ group: g.name, groupId: g.id, optionId: id, name: o.name,
        unitPrice: o.priceExpr ? Math.round(evalExpr(o.priceExpr, ctx.scope)) : (o.price || 0), priceFormula: o.priceExpr || null }); });
    }

    const rows = [];
    bom.systems.forEach(s => s.assemblies.forEach(a => {
      rows.push({ level: 1, system: s.name, source: a.source || "", code: a.code, name: a.name,
        qty: a.qty, qtyFormula: a.qtyExpr || null, unitPrice: a.price, ext: a.price * a.qty });
      (a.children || []).forEach(c => rows.push({ level: 2, system: s.name, source: a.source || "", code: c.code, name: c.name,
        qty: c.qty, qtyFormula: c.qtyExpr || null, unitPrice: c.price, ext: c.price * c.qty }));
    }));

    const priceLines = price.lines.map(l => {
      const o = l.label && opt0ByName(l.label);
      return { label: l.label, amount: l.amount, base: !!l.base, group: l.group || null,
        priceFormula: l.formula && o ? o.priceExpr : null };
    });

    const rules = CAR.rules.filter(r => !enabled || enabled.includes(r.id)).map(r => ({
      id: r.id, type: r.type, msg: r.msg,
      when: r.when || null, then: r.then || null, expr: r.expr || null, refs: r.refs || null }));

    // Configuration fingerprint (ISO 10007 traceability): deterministic short hash
    // over the resolved selections + attributes, so an identical config yields an
    // identical document-control id.
    const fpSrc = JSON.stringify({ p: CAR.platform.code, s: selections.map(s => s.optionId), a: attributes.map(a => a.id + ":" + a.value) });
    let h = 5381; for (let i = 0; i < fpSrc.length; i++) { h = ((h << 5) + h + fpSrc.charCodeAt(i)) >>> 0; }
    const configHash = ("00000000" + h.toString(16).toUpperCase()).slice(-8);
    const comp = CAR.compliance || {};
    const docNo = (comp.formNo || "QF-BOM-001") + "/" + CAR.platform.code + "-" + configHash;

    return {
      meta: { platform: { name: CAR.platform.name, code: CAR.platform.code }, basePrice: CAR.platform.basePrice,
        generatedAt: new Date().toISOString(), orderQty: oq,
        compliance: comp, configHash, docNo },
      attributes, calculated, selections,
      bom: { rows, partCount: bom.partCount, cost: bom.cost },
      price: { lines: priceLines, perUnit: price.total, orderQty: oq, orderTotal: price.total * oq },
      routing: generateRouting(sel).map((o, i) => ({ step: i + 1, code: o.code, name: o.name })),
      rules,
    };
  }
  function opt0ByName(name) { for (const g of CAR.groups) { const o = g.options.find(o => o.name === name); if (o) return o; } return null; }

  // CSV of the resolved multi-level BoM (one row per part, formula provenance kept).
  function bomToCSV(spec) {
    const head = ["Level", "System", "Source", "Code", "Part", "Qty", "QtyFormula", "UnitPrice", "ExtPrice"];
    const esc = (v) => { v = String(v == null ? "" : v); return /[",\n]/.test(v) ? '"' + v.replace(/"/g, '""') + '"' : v; };
    const lines = [head.join(",")];
    spec.bom.rows.forEach(r => lines.push([r.level, r.system, r.source, r.code, r.name, r.qty, r.qtyFormula || "", r.unitPrice, r.ext].map(esc).join(",")));
    return lines.join("\n");
  }
  function specToJSON(spec) { return JSON.stringify(spec, null, 2); }

  function dependencyEdges(enabledRuleIds) {
    const rules = CAR.rules.filter(r => !enabledRuleIds || enabledRuleIds.includes(r.id));
    const edges = [];
    for (const r of rules) {
      const froms = r.when || (r.refs ? [r.refs[0]] : []);
      const tos = r.then || (r.refs ? r.refs.slice(1) : []);
      for (const w of froms) for (const t of tos)
        if (opt(w) && opt(t))   // only draw between real option nodes; skip attribute/calc refs
          edges.push({ rule: r.id, type: r.type, from: w, to: t, msg: r.msg });
    }
    return edges;
  }

  window.CFG = { opt, groupOf, groupById, isSel, applicableGroups, defaultSelections,
    reconcile, applyChoice, resolveBom, priceRollup, generateRouting, status, dependencyEdges,
    context, evalExpr, computeCalc, validateExpr, setAttr, attrBounds, attrName, attrDef, exportSpec, bomToCSV, specToJSON, attributes: ATTRS, calc: CALC, fmt: fmtMoney };

  function fmtMoney(n) { return "$" + Math.round(n).toLocaleString("en-US"); }
})();
