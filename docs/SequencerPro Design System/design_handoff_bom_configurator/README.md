# Handoff: BoM Configurator (Product Configurator for Configurable Platforms)

## Overview
A visual product configurator for widely-configurable product platforms (D365-style "configurable products", but visual and rules-first). Built as a prototype for **SequencerPro / Process Manager** (an ERP for manufacturing process design). The example platform is a generic 6-axis industrial robot ("Sequencer RX-6"), but the entire model is data-driven — any product tree (robots, 3D printers, machinery) can be expressed in the same schema.

It covers **both audiences**:
- **Consumer (Configure)** — pick options step-by-step, rules fire live (incompatible options disable with explain-why tooltips), numeric attributes with sliders, live schematic/photo preview, resolved multi-level BoM, price rollup, generated routing, work-order release.
- **Author (Author rules / Preview images / Product tree)** — rule builder (requires / excludes / formula / min-max bounds with validation messages), editable calculated-attribute formulas with as-you-type validation, dependency canvas, author-uploaded preview images (base + per-option overlays), and a visual "150% BoM" product-tree diagram.

## About the Design Files
The files in this bundle are **design references created in HTML** — working prototypes showing intended look and behavior, **not production code to copy directly**. The task is to **recreate this design in your codebase's existing environment** using its established patterns. The natural target for SequencerPro is the **Process Manager stack: Blazor (.NET) + Bootstrap 5 + Bootstrap Icons** — map the React components to Razor components and the rules engine to a C# service in `ProcessManager.Domain`. If you're building elsewhere (React, Vue), the JSX files translate nearly 1:1.

## Fidelity
**High-fidelity.** Colors, typography, spacing, and interactions are final and use the SequencerPro design-system tokens (`design-system/` in this bundle). Recreate pixel-perfectly, substituting your codebase's component library where equivalents exist.

## Architecture (file → responsibility)
All app files are in `prototype/` (open `prototype/index.html` to run it — see Files section):

| File | Responsibility |
|---|---|
| `data.js` | **The entire product model** — platform meta, ISO compliance block, base BoM, attributes, calculated attributes, option groups (each option carries its BoM assemblies, routing ops, price or `priceExpr`), and declarative rules. Port this to your item master / product-model tables. |
| `engine.js` | Pure rules/resolution engine (no UI): expression evaluator (`has('optId')`, ceil/floor/min/max over attributes), calculated attributes, rule evaluation → disabled options + violations + fixes, BoM resolution with formula quantities, price rollup with `priceExpr`, attribute bounds derivation (rules clamp slider ranges), routing generation, `exportSpec` (full serializable resolved config), CSV/JSON serializers. **This is the file to port most carefully — it's framework-agnostic logic.** |
| `app.js` | Shell: mode tabs (Configure / Author rules / Preview images / Product tree), selection state, print-job portal. |
| `configure.js` | Consumer view: stepper, option cards (disabled + explain-why), attribute sliders (bound-rule clamping, blue lock note), violations panel with one-click fixes, preview pane. |
| `author.js` | Author view: rule list with enable/disable toggles, inline-editable formula expressions (validated as you type), calculated-attribute editor, 4-mode rule builder (requires / excludes / formula / min-max bounds + message), dependency canvas. |
| `bom.js` | Output panels: multi-level BoM tree, price rollup, routing, violations list, work-order modal (Spec / BoM / Rules & routing / Raw JSON tabs; Copy CSV / Copy JSON / Download CSV; Print), and the **ISO 9001 print spec sheet** (see below). |
| `tree.js` | Visual product-tree diagram ("150% BoM"): root → group nodes → option cards with connector lines; cards expand to show parts; badges for formulas (ƒ), rules, routing ops; gold ring = in current config. |
| `preview.js` + `imagestore.js` | Author-managed preview images: base image + per-option transparent-PNG overlays composited in selection order; localStorage persistence (replace with backend storage in production); falls back to the parametric SVG schematic. |
| `robot.js` | Parametric SVG schematic (fallback preview): link lengths scale with reach, effector swaps by selection, 7th-axis rail appears when configured. |

## The Data Model (port this schema)
```
platform: { code, name, tagline, basePrice }
compliance: { formNo, revision, standard, clauses, classification, owner,
              retention, approvals[{role,title}], revisionHistory[] }
baseBom: [{ system, code, name, qty|qtyExpr, price, children[] }]
attributes: [{ id, name, unit, kind:"range", min, max, step, default, hint }]
calc: [{ id, name, expr, unit, hint }]            // resolved top-to-bottom
groups: [{ id, name, icon, multi, required, options: [{
  id, name, price | priceExpr, sub, swatch?, badge?, ee?,
  bom: [assemblies as baseBom], ops: [{code,name,seq}] }] }]
rules: [
  { id, type:"requires"|"requiresOneOf"|"excludes", when[], then[], msg },
  { id, type:"formula", expr, msg, refs[], fixAttr?|fixChoose? } ]
```
Key behaviors:
- `qtyExpr` / `priceExpr` are expressions over attributes + calc values + `has('optId')`.
- Formula rules must evaluate **true**; `refs` ties a rule to attributes/options for inline display; `fixAttr`/`fixChoose` power one-click fixes.
- Bound-style formula clauses (`attr <= N`, `attr >= N`) are parsed to **clamp slider ranges** (tightest of rule bound and declared min/max wins) with a "Range limited … by rule Rn" note.
- Expression evaluation in the prototype uses a sandboxed `Function` over a whitelisted scope. In production use a real expression parser (e.g. NCalc in .NET) — never raw eval.

## Screens / Views

### 1. Configure (consumer)
- **Layout**: header (60px, dark `--slate` #212121, NodeMark logo + mode tabs + status) / left stepper nav (group list with completion ticks) / center preview + option grid / right rail: Issues panel, resolved BoM tree, price rollup, routing, "Release work order" button (gold `--gold` #f1c40f, dark text).
- **Option cards**: white `#ffffff`, 1px `#e6e6e6` border, 8px radius; selected = 2px gold border + gold-50 `#fef9e7` bg; disabled = 45% opacity + lock icon + tooltip naming the exact rule; price delta right-aligned mono.
- **Attribute sliders**: gold accent; **blue** (`--blue` #529bde) accent + lock note when a bound rule clamps the range; orange (`--orange` #e95b15) when violated, with inline message + fix link.
- **Preview**: author images (base + overlays) if set, else parametric SVG robot.

### 2. Author rules
- Rule cards listing all rules with type tag (teal requires / orange excludes / blue formula), enable toggle, inline-editable expression (mono, `--bg-sunken` #f0f0f0 well) with live validation (unknown identifiers, syntax) shown in orange before commit.
- Calculated attributes: name + editable formula + live value (gold mono).
- **New rule builder**: 4 modes — Requires (A→B selects), Excludes, Formula (free boolean expression), Min/Max bound (attribute picker + min/max inputs + custom validation message).

### 3. Preview images (author)
- Left sticky card: live composite preview + base-image drop slot (120px) + "Clear all" (uses `--status-fail` #d64545 text).
- Right: per-group sections, each option a 84px drop slot (drag-drop or click, downscaled to ≤1000px PNG, transparency preserved).

### 4. Product tree
- Org-chart diagram: root platform node (2px slate border) → connector lines (1.5px `#d4d4d4`, 6px radius elbows) → gold "Every build" base card + dark group nodes → vertical rails of 168px option cards. Click card = expand parts inline. Badges: ƒ teal, rules blue diagram icon w/ tooltip, ops signpost icon. Current-config cards get gold ring + floating check.

### 5. Work-order export modal
- 680px, 4 tabs (Spec / BoM / Rules & routing / Raw), 4 stat tiles, footer: Copy BoM CSV, Copy JSON, Download CSV, **Print / Save PDF** (dark button).
- CSV columns: `Level,System,Source,Code,Part,Qty,QtyFormula,UnitPrice,ExtPrice` — formula provenance included.

### 6. ISO 9001 print spec sheet (Letter, @page 14mm margins)
Rendered via portal to `<body>`; `@media print` hides the app. Sections in order:
1. Controlled-document header: bordered grid (Document No. = `formNo/PLATFORM-configHash`, Form, Revision, Standard, WO, config hash, issue date, page) + black classification bar with clause list.
2. Platform summary band; Configuration (2-col); Attributes (with clamped bounds `[min–max · ruleId]`) + Calculated (with formulas); full BoM table (ƒ-annotated quantities); Price rollup.
3. **Routing & Sequence Clearance Record** — one row per operation: seq bubble, op code, name, operator signature + date lines, dashed per-op "Stamp NN" clearance box; ISO §8.5.1/§8.5.2 language; skipped-stamp nonconformance note (§8.7).
4. Authorization & Approval: 4 signature blocks (Prepared/Reviewed/Approved/Quality release) each with dashed Stamp/Seal box; Quality Disposition checkboxes + circular Quality Approved stamp ring.
5. Revision history table; "UNCONTROLLED WHEN PRINTED" footer.
- The **config hash** is a deterministic djb2 hash of selections+attributes → same config, same document ID (ISO 10007 traceability).

## Interactions & Behavior
- Selecting an option re-evaluates all rules synchronously; disabled options show tooltip "Blocked by Rn: <msg>".
- Violations panel lists failures with one-click **Fix** (applies `fixAttr`/`fixChoose`).
- Formula edits hot-apply: BoM quantities, prices, and rule states all recompute on every keystroke commit.
- Slider clamping: dragging is physically limited to the rule-derived range.
- Transitions: 120–200ms, `cubic-bezier(0.4,0,0.2,1)`. Hover: borders darken to `#d4d4d4`, cards lift `--shadow-sm`.
- Print flow: button → render sheet → `window.print()` → `afterprint` clears the portal.

## State Management
- `sel` = `{ [groupId]: optionId | optionId[], attrs: { [attrId]: number } }` — single source of truth.
- `enabled` = array of active rule ids (author toggles).
- Derived (recomputed, never stored): disabled options, violations, calc values, resolved BoM, price, routing, bounds.
- Preview images: keyed blob store (base + per-option) — replace localStorage with your backend.

## Design Tokens (from the SequencerPro design system — full set in `design-system/`)
- **Brand**: gold `#f1c40f` (primary), teal `#1da086` (pass/requires), blue `#529bde` (info/formula), orange `#e95b15` (danger/excludes), slate `#212121`, charcoal `#3d3d3d`, grey `#adadad`, off-white `#f6f6f6`.
- **Status**: fail `#d64545` (distinct from orange).
- **Surfaces**: app bg `#f6f6f6`, card `#ffffff`, sunken `#f0f0f0`; borders `#e6e6e6` / strong `#d4d4d4`.
- **Type**: display 'Coda' (800), body 'Mulish' (400/600/700), mono 'IBM Plex Mono' (Google Fonts). Compact scale: 11/12/13/14/15px body sizes; 20px panel titles; 26px page titles.
- **Radius**: 3/5/8/12px (chip/button/card/modal). **Shadows**: neutral layered, e.g. `0 1px 3px rgba(33,33,33,.08)`.
- **Spacing**: 4px grid. Controls 30/38/46px heights.

## Assets
- Bootstrap Icons via CDN (`bootstrap-icons@1.11.3`) — the Process Manager app already uses these.
- SequencerPro NodeMark + Wordmark: React components in `design-system/components/brand/`.
- No raster images required; the robot preview is inline SVG; author preview images are user-supplied.

## Files
- `prototype/` — the full working prototype. **Note:** `index.html` references the design system at `../../styles.css` and `../../_ds_bundle.js`; in this bundle those live in `design-system/`, so either serve the original project structure or repoint those two paths.
- `design-system/styles.css` + `design-system/tokens/` — all design tokens (@import manifest + colors/typography/spacing/elevation/fonts).
- `design-system/components/` — Button, Tag, StatusBadge, Card, NodeMark, etc. (JSX + .d.ts props contracts) referenced by the prototype.
- `design-system/_ds_bundle.js` — compiled component bundle the prototype loads at runtime.
