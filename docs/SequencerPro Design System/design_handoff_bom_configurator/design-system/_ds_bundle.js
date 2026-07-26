/* @ds-bundle: {"format":4,"namespace":"SequencerProDesignSystem_5eb90b","components":[{"name":"NodeMark","sourcePath":"components/brand/NodeMark.jsx"},{"name":"Wordmark","sourcePath":"components/brand/Wordmark.jsx"},{"name":"Button","sourcePath":"components/buttons/Button.jsx"},{"name":"IconButton","sourcePath":"components/buttons/IconButton.jsx"},{"name":"Card","sourcePath":"components/data/Card.jsx"},{"name":"PortBadge","sourcePath":"components/data/PortBadge.jsx"},{"name":"StatCard","sourcePath":"components/data/StatCard.jsx"},{"name":"StatusBadge","sourcePath":"components/feedback/StatusBadge.jsx"},{"name":"Tag","sourcePath":"components/feedback/Tag.jsx"},{"name":"Field","sourcePath":"components/forms/Field.jsx"},{"name":"Input","sourcePath":"components/forms/Input.jsx"},{"name":"Select","sourcePath":"components/forms/Select.jsx"}],"sourceHashes":{"components/brand/NodeMark.jsx":"f247a3c294f2","components/brand/Wordmark.jsx":"da799c2f1490","components/buttons/Button.jsx":"7514a1524393","components/buttons/IconButton.jsx":"849b178161c8","components/data/Card.jsx":"b24f9b0adedd","components/data/PortBadge.jsx":"292f516a5a37","components/data/StatCard.jsx":"7da39fb16642","components/feedback/StatusBadge.jsx":"d6781d48b2c1","components/feedback/Tag.jsx":"d836024414f5","components/forms/Field.jsx":"0131e5c8a2e2","components/forms/Input.jsx":"2ddd3057268c","components/forms/Select.jsx":"23493e728b44","prototypes/bom-configurator/app.js":"75eddf3f31a1","prototypes/bom-configurator/author.js":"191dfd90580a","prototypes/bom-configurator/bom.js":"1c25d169413f","prototypes/bom-configurator/configure.js":"ef014e8587c2","prototypes/bom-configurator/data.js":"9ee27fa627e5","prototypes/bom-configurator/engine.js":"1cfc49eab39d","prototypes/bom-configurator/imagestore.js":"2eea120c334c","prototypes/bom-configurator/preview.js":"1e2ccfae138d","prototypes/bom-configurator/robot.js":"07560ee2353f","prototypes/bom-configurator/tree.js":"e0ac29734f33","ui_kits/process-manager/data.js":"f01856451002","ui_kits/process-manager/screens.jsx":"1c31a19a54b3","ui_kits/process-manager/shell.jsx":"b609ff86fae8"},"inlinedExternals":[],"unexposedExports":[]} */

(() => {

const __ds_ns = (window.SequencerProDesignSystem_5eb90b = window.SequencerProDesignSystem_5eb90b || {});

const __ds_scope = {};

(__ds_ns.__errors = __ds_ns.__errors || []);

// components/brand/NodeMark.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const NODES = [{
  c: "var(--gold)",
  x: 14,
  y: 76
}, {
  c: "var(--teal)",
  x: 52,
  y: 50
}, {
  c: "var(--blue)",
  x: 92,
  y: 62
}, {
  c: "var(--orange)",
  x: 134,
  y: 24
}];

/**
 * NodeMark — the SequencerPro logo motif: four process nodes
 * (gold → teal → blue → orange) connected in an ascending sequence.
 * Pure CSS/SVG recreation so it tints and scales cleanly.
 */
function NodeMark({
  size = 40,
  className = "",
  style = {},
  ...rest
}) {
  const r = [11, 15, 16, 21];
  return /*#__PURE__*/React.createElement("svg", _extends({
    className: className,
    width: size,
    height: size * 100 / 168,
    viewBox: "0 0 168 100",
    fill: "none",
    style: style,
    role: "img",
    "aria-label": "SequencerPro"
  }, rest), NODES.slice(0, -1).map((n, i) => {
    const m = NODES[i + 1];
    return /*#__PURE__*/React.createElement("line", {
      key: i,
      x1: n.x,
      y1: n.y,
      x2: m.x,
      y2: m.y,
      stroke: "var(--grey-300)",
      strokeWidth: "7",
      strokeLinecap: "round"
    });
  }), NODES.map((n, i) => /*#__PURE__*/React.createElement("circle", {
    key: i,
    cx: n.x,
    cy: n.y,
    r: r[i],
    fill: n.c,
    stroke: "var(--pure-white)",
    strokeWidth: "3"
  })));
}
Object.assign(__ds_scope, { NodeMark });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/brand/NodeMark.jsx", error: String((e && e.message) || e) }); }

// components/brand/Wordmark.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * SequencerPro wordmark. Coda body with Kelly-Slab "e" flourishes,
 * exactly as the product locks it up.
 */
function Wordmark({
  size = 28,
  color,
  className = "",
  style = {},
  ...rest
}) {
  const E = k => /*#__PURE__*/React.createElement("span", {
    key: k,
    style: {
      fontFamily: "var(--font-accent)",
      fontStyle: "normal",
      fontSize: "1.08em"
    }
  }, "e");
  return /*#__PURE__*/React.createElement("span", _extends({
    className: className,
    style: {
      fontFamily: "var(--font-display)",
      fontWeight: "var(--fw-display)",
      fontSize: typeof size === "number" ? `${size}px` : size,
      lineHeight: 1,
      whiteSpace: "nowrap",
      color: color || "var(--text-primary)",
      ...style
    }
  }, rest), "S", E("e1"), "qu", E("e2"), "nc", E("e3"), "rPro");
}
Object.assign(__ds_scope, { Wordmark });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/brand/Wordmark.jsx", error: String((e && e.message) || e) }); }

// components/buttons/Button.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const VARIANTS = {
  primary: {
    background: "var(--gold)",
    color: "var(--slate)",
    border: "1px solid var(--gold)"
  },
  accent: {
    background: "var(--orange)",
    color: "#fff",
    border: "1px solid var(--orange)"
  },
  secondary: {
    background: "var(--pure-white)",
    color: "var(--text-primary)",
    border: "1px solid var(--border-strong)"
  },
  ghost: {
    background: "transparent",
    color: "var(--text-primary)",
    border: "1px solid transparent"
  },
  danger: {
    background: "var(--status-fail)",
    color: "#fff",
    border: "1px solid var(--status-fail)"
  },
  dark: {
    background: "var(--slate)",
    color: "var(--white)",
    border: "1px solid var(--slate)"
  }
};
const SIZES = {
  sm: {
    height: "var(--control-h-sm)",
    padding: "0 10px",
    fontSize: "var(--text-xs)",
    gap: "5px"
  },
  md: {
    height: "var(--control-h)",
    padding: "0 14px",
    fontSize: "var(--text-sm)",
    gap: "6px"
  },
  lg: {
    height: "var(--control-h-lg)",
    padding: "0 20px",
    fontSize: "var(--text-md)",
    gap: "8px"
  }
};

/**
 * Primary action control. Variants map to the brand node colors;
 * `primary` is the gold-on-slate lockup used across the product.
 */
function Button({
  variant = "primary",
  size = "md",
  iconLeft,
  iconRight,
  block = false,
  disabled = false,
  className = "",
  style = {},
  children,
  ...rest
}) {
  const v = VARIANTS[variant] || VARIANTS.primary;
  const s = SIZES[size] || SIZES.md;
  return /*#__PURE__*/React.createElement("button", _extends({
    className: `sq-btn ${className}`,
    disabled: disabled,
    style: {
      display: block ? "flex" : "inline-flex",
      width: block ? "100%" : "auto",
      alignItems: "center",
      justifyContent: "center",
      gap: s.gap,
      height: s.height,
      padding: s.padding,
      fontSize: s.fontSize,
      fontFamily: "var(--font-sans)",
      fontWeight: "var(--fw-semibold)",
      letterSpacing: "0.01em",
      borderRadius: "var(--radius)",
      cursor: disabled ? "not-allowed" : "pointer",
      opacity: disabled ? 0.5 : 1,
      whiteSpace: "nowrap",
      transition: "filter var(--dur) var(--ease), box-shadow var(--dur) var(--ease)",
      ...v,
      ...style
    },
    onMouseEnter: e => {
      if (!disabled) e.currentTarget.style.filter = "brightness(0.94)";
    },
    onMouseLeave: e => {
      e.currentTarget.style.filter = "none";
    }
  }, rest), iconLeft, children, iconRight);
}
Object.assign(__ds_scope, { Button });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/buttons/Button.jsx", error: String((e && e.message) || e) }); }

// components/buttons/IconButton.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const SIZES = {
  sm: 28,
  md: 34,
  lg: 42
};

/**
 * Square icon-only button. Use for toolbar actions, table row actions,
 * and anywhere a label would be redundant. Always set `title` for a11y.
 */
function IconButton({
  size = "md",
  variant = "ghost",
  disabled = false,
  title,
  className = "",
  style = {},
  children,
  ...rest
}) {
  const d = SIZES[size] || SIZES.md;
  const variants = {
    ghost: {
      background: "transparent",
      color: "var(--text-secondary)",
      border: "1px solid transparent"
    },
    secondary: {
      background: "var(--pure-white)",
      color: "var(--text-primary)",
      border: "1px solid var(--border-strong)"
    },
    primary: {
      background: "var(--gold)",
      color: "var(--slate)",
      border: "1px solid var(--gold)"
    }
  };
  const v = variants[variant] || variants.ghost;
  return /*#__PURE__*/React.createElement("button", _extends({
    title: title,
    "aria-label": title,
    disabled: disabled,
    className: `sq-iconbtn ${className}`,
    style: {
      display: "inline-flex",
      alignItems: "center",
      justifyContent: "center",
      width: d,
      height: d,
      borderRadius: "var(--radius)",
      cursor: disabled ? "not-allowed" : "pointer",
      opacity: disabled ? 0.5 : 1,
      transition: "background var(--dur) var(--ease), color var(--dur) var(--ease)",
      ...v,
      ...style
    },
    onMouseEnter: e => {
      if (!disabled && variant === "ghost") e.currentTarget.style.background = "var(--grey-100)";
    },
    onMouseLeave: e => {
      if (variant === "ghost") e.currentTarget.style.background = "transparent";
    }
  }, rest), children);
}
Object.assign(__ds_scope, { IconButton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/buttons/IconButton.jsx", error: String((e && e.message) || e) }); }

// components/data/Card.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Surface container. Soft single-layer shadow, hairline border,
 * 6px radius. Optional header (title + actions) and footer.
 */
function Card({
  title,
  subtitle,
  actions,
  footer,
  accent,
  padding = "var(--space-5)",
  className = "",
  style = {},
  children,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("section", _extends({
    className: className,
    style: {
      background: "var(--bg-surface)",
      border: "var(--border)",
      borderTop: accent ? `3px solid ${accent}` : "var(--border)",
      borderRadius: "var(--radius-md)",
      boxShadow: "var(--shadow-sm)",
      overflow: "hidden",
      ...style
    }
  }, rest), (title || actions) && /*#__PURE__*/React.createElement("header", {
    style: {
      display: "flex",
      alignItems: "flex-start",
      justifyContent: "space-between",
      gap: "12px",
      padding: "var(--space-4) var(--space-5)",
      borderBottom: "var(--border)"
    }
  }, /*#__PURE__*/React.createElement("div", null, title && /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: "var(--text-md)",
      fontWeight: "var(--fw-bold)",
      color: "var(--text-primary)"
    }
  }, title), subtitle && /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: "var(--text-xs)",
      color: "var(--text-secondary)",
      marginTop: 2
    }
  }, subtitle)), actions && /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: "6px",
      flex: "none"
    }
  }, actions)), /*#__PURE__*/React.createElement("div", {
    style: {
      padding
    }
  }, children), footer && /*#__PURE__*/React.createElement("footer", {
    style: {
      padding: "var(--space-3) var(--space-5)",
      borderTop: "var(--border)",
      background: "var(--bg-sunken)",
      fontSize: "var(--text-xs)",
      color: "var(--text-secondary)"
    }
  }, footer));
}
Object.assign(__ds_scope, { Card });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/Card.jsx", error: String((e && e.message) || e) }); }

// components/data/PortBadge.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * PortBadge — domain primitive. Represents a Step's typed connection
 * point. PortType (Material / Parameter / Characteristic / Condition)
 * sets the color & glyph; direction sets the arrow. Mirrors the
 * Process Manager port model used in PFMEA / C&E / Control Plan tools.
 */
const TYPES = {
  Material: {
    color: "var(--blue)",
    glyph: "▣",
    abbr: "MAT"
  },
  Parameter: {
    color: "var(--gold)",
    glyph: "𝑥",
    abbr: "PARAM"
  },
  Characteristic: {
    color: "var(--teal)",
    glyph: "𝑦",
    abbr: "CHAR"
  },
  Condition: {
    color: "var(--orange)",
    glyph: "✓",
    abbr: "COND"
  }
};
function PortBadge({
  type = "Material",
  direction = "in",
  label,
  className = "",
  style = {},
  ...rest
}) {
  const t = TYPES[type] || TYPES.Material;
  const isIn = direction === "in";
  return /*#__PURE__*/React.createElement("span", _extends({
    className: className,
    style: {
      display: "inline-flex",
      alignItems: "center",
      gap: "6px",
      height: "22px",
      padding: "0 8px 0 6px",
      background: `color-mix(in srgb, ${t.color} 12%, white)`,
      border: `1px solid color-mix(in srgb, ${t.color} 45%, white)`,
      borderRadius: "var(--radius-pill)",
      fontSize: "var(--text-2xs)",
      fontWeight: "var(--fw-semibold)",
      color: "var(--text-primary)",
      whiteSpace: "nowrap",
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("span", {
    style: {
      color: t.color,
      fontWeight: "var(--fw-bold)",
      transform: isIn ? "none" : "scaleX(-1)",
      display: "inline-block"
    },
    "aria-hidden": true
  }, isIn ? "→" : "→"), /*#__PURE__*/React.createElement("span", {
    style: {
      width: 15,
      height: 15,
      borderRadius: "3px",
      flex: "none",
      display: "inline-flex",
      alignItems: "center",
      justifyContent: "center",
      background: t.color,
      color: "#fff",
      fontSize: "10px",
      lineHeight: 1
    }
  }, t.glyph), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "var(--font-mono)"
    }
  }, label || t.abbr));
}
Object.assign(__ds_scope, { PortBadge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/PortBadge.jsx", error: String((e && e.message) || e) }); }

// components/data/StatCard.jsx
try { (() => {
/**
 * KPI / metric tile for dashboards. Big display number (Coda),
 * a label, and an optional delta with up/down tone.
 */
function StatCard({
  label,
  value,
  unit,
  delta,
  deltaTone = "up",
  icon,
  accent = "var(--gold)",
  className = "",
  style = {}
}) {
  const deltaColor = deltaTone === "up" ? "var(--teal-700)" : deltaTone === "down" ? "#9a2b2b" : "var(--text-secondary)";
  return /*#__PURE__*/React.createElement("div", {
    className: className,
    style: {
      background: "var(--bg-surface)",
      border: "var(--border)",
      borderRadius: "var(--radius-md)",
      boxShadow: "var(--shadow-sm)",
      padding: "var(--space-4) var(--space-5)",
      display: "flex",
      flexDirection: "column",
      gap: "8px",
      ...style
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "center",
      justifyContent: "space-between"
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: "var(--text-2xs)",
      fontWeight: "var(--fw-bold)",
      letterSpacing: "var(--ls-caps)",
      textTransform: "uppercase",
      color: "var(--text-secondary)"
    }
  }, label), icon && /*#__PURE__*/React.createElement("span", {
    style: {
      width: 26,
      height: 26,
      borderRadius: "var(--radius-sm)",
      display: "inline-flex",
      alignItems: "center",
      justifyContent: "center",
      background: `color-mix(in srgb, ${accent} 16%, white)`,
      color: accent,
      fontSize: "14px"
    }
  }, icon)), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "baseline",
      gap: "6px"
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "var(--font-display)",
      fontWeight: "var(--fw-display)",
      fontSize: "var(--text-3xl)",
      color: "var(--text-primary)",
      lineHeight: 1
    }
  }, value), unit && /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: "var(--text-sm)",
      color: "var(--text-secondary)"
    }
  }, unit)), delta && /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: "var(--text-xs)",
      fontWeight: "var(--fw-semibold)",
      color: deltaColor
    }
  }, deltaTone === "up" ? "▲" : deltaTone === "down" ? "▼" : "•", " ", delta));
}
Object.assign(__ds_scope, { StatCard });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/StatCard.jsx", error: String((e && e.message) || e) }); }

// components/feedback/StatusBadge.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Status pill for the data-oriented domain: job lifecycle, item grade,
 * step execution state, QA verdicts. Tone sets the color from the
 * brand semantic palette; a leading dot reinforces it.
 */
const TONES = {
  pass: {
    label: "Pass",
    fg: "var(--teal-700)",
    bg: "var(--teal-100)",
    dot: "var(--teal)"
  },
  active: {
    label: "In Progress",
    fg: "var(--blue-700)",
    bg: "var(--blue-100)",
    dot: "var(--blue)"
  },
  hold: {
    label: "On Hold",
    fg: "var(--gold-700)",
    bg: "var(--gold-100)",
    dot: "var(--gold)"
  },
  rework: {
    label: "Rework",
    fg: "var(--orange-700)",
    bg: "var(--orange-100)",
    dot: "var(--orange)"
  },
  fail: {
    label: "Fail",
    fg: "#9a2b2b",
    bg: "#f8dada",
    dot: "var(--status-fail)"
  },
  pending: {
    label: "Pending",
    fg: "var(--ink-600)",
    bg: "var(--grey-100)",
    dot: "var(--grey)"
  },
  done: {
    label: "Completed",
    fg: "var(--ink-600)",
    bg: "var(--grey-100)",
    dot: "var(--charcoal)"
  }
};
function StatusBadge({
  tone = "pending",
  children,
  dot = true,
  className = "",
  style = {},
  ...rest
}) {
  const t = TONES[tone] || TONES.pending;
  return /*#__PURE__*/React.createElement("span", _extends({
    className: className,
    style: {
      display: "inline-flex",
      alignItems: "center",
      gap: "6px",
      height: "20px",
      padding: "0 9px",
      borderRadius: "var(--radius-pill)",
      background: t.bg,
      color: t.fg,
      fontSize: "var(--text-2xs)",
      fontWeight: "var(--fw-bold)",
      letterSpacing: "0.02em",
      textTransform: "uppercase",
      whiteSpace: "nowrap",
      ...style
    }
  }, rest), dot && /*#__PURE__*/React.createElement("span", {
    style: {
      width: 6,
      height: 6,
      borderRadius: "50%",
      background: t.dot,
      flex: "none"
    }
  }), children || t.label);
}
Object.assign(__ds_scope, { StatusBadge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/StatusBadge.jsx", error: String((e && e.message) || e) }); }

// components/feedback/Tag.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Tag / chip for codes, kinds, grades, and metadata. Monospace by
 * default because it most often carries an identifier (WDG-100, WO-2026-001).
 */
function Tag({
  color = "neutral",
  mono = true,
  removable = false,
  onRemove,
  className = "",
  style = {},
  children,
  ...rest
}) {
  const colors = {
    neutral: {
      bg: "var(--grey-100)",
      fg: "var(--ink-600)",
      bd: "var(--border-default)"
    },
    gold: {
      bg: "var(--gold-100)",
      fg: "var(--gold-700)",
      bd: "var(--gold)"
    },
    teal: {
      bg: "var(--teal-100)",
      fg: "var(--teal-700)",
      bd: "var(--teal)"
    },
    blue: {
      bg: "var(--blue-100)",
      fg: "var(--blue-700)",
      bd: "var(--blue)"
    },
    orange: {
      bg: "var(--orange-100)",
      fg: "var(--orange-700)",
      bd: "var(--orange)"
    }
  };
  const c = colors[color] || colors.neutral;
  return /*#__PURE__*/React.createElement("span", _extends({
    className: className,
    style: {
      display: "inline-flex",
      alignItems: "center",
      gap: "6px",
      height: "22px",
      padding: "0 8px",
      borderRadius: "var(--radius-sm)",
      background: c.bg,
      color: c.fg,
      border: `1px solid ${c.bd}33`,
      fontFamily: mono ? "var(--font-mono)" : "var(--font-sans)",
      fontSize: "var(--text-xs)",
      fontWeight: "var(--fw-medium)",
      whiteSpace: "nowrap",
      ...style
    }
  }, rest), children, removable && /*#__PURE__*/React.createElement("button", {
    onClick: onRemove,
    "aria-label": "Remove",
    style: {
      border: "none",
      background: "none",
      cursor: "pointer",
      color: "inherit",
      opacity: 0.6,
      padding: 0,
      lineHeight: 1,
      fontSize: "13px"
    }
  }, "\xD7"));
}
Object.assign(__ds_scope, { Tag });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Tag.jsx", error: String((e && e.message) || e) }); }

// components/forms/Field.jsx
try { (() => {
/** Form field wrapper: label, optional hint/required marker, and error text. */
function Field({
  label,
  hint,
  error,
  required = false,
  htmlFor,
  className = "",
  style = {},
  children
}) {
  return /*#__PURE__*/React.createElement("div", {
    className: className,
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "5px",
      ...style
    }
  }, label && /*#__PURE__*/React.createElement("label", {
    htmlFor: htmlFor,
    style: {
      fontSize: "var(--text-xs)",
      fontWeight: "var(--fw-semibold)",
      color: "var(--text-primary)"
    }
  }, label, required && /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--status-fail)",
      marginLeft: 3
    }
  }, "*")), children, error ? /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: "var(--text-2xs)",
      color: "var(--status-fail)"
    }
  }, error) : hint && /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: "var(--text-2xs)",
      color: "var(--text-muted)"
    }
  }, hint));
}
Object.assign(__ds_scope, { Field });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Field.jsx", error: String((e && e.message) || e) }); }

// components/forms/Input.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Text input. Compact, 1px hairline, blue focus ring — Bootstrap-5
 * lineage tuned to the brand. Supports invalid state and prefix/suffix.
 */
function Input({
  size = "md",
  invalid = false,
  prefix,
  suffix,
  mono = false,
  className = "",
  style = {},
  disabled = false,
  ...rest
}) {
  const heights = {
    sm: "var(--control-h-sm)",
    md: "var(--control-h)",
    lg: "var(--control-h-lg)"
  };
  const [focus, setFocus] = React.useState(false);
  return /*#__PURE__*/React.createElement("div", {
    className: className,
    style: {
      display: "flex",
      alignItems: "center",
      gap: "8px",
      height: heights[size],
      padding: "0 10px",
      background: disabled ? "var(--grey-100)" : "var(--pure-white)",
      border: `1px solid ${invalid ? "var(--status-fail)" : focus ? "var(--blue)" : "var(--border-strong)"}`,
      borderRadius: "var(--radius)",
      boxShadow: focus ? "var(--ring)" : "none",
      transition: "border-color var(--dur) var(--ease), box-shadow var(--dur) var(--ease)",
      ...style
    }
  }, prefix && /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--text-muted)",
      display: "inline-flex"
    }
  }, prefix), /*#__PURE__*/React.createElement("input", _extends({
    disabled: disabled,
    onFocus: e => {
      setFocus(true);
      rest.onFocus?.(e);
    },
    onBlur: e => {
      setFocus(false);
      rest.onBlur?.(e);
    }
  }, rest, {
    style: {
      flex: 1,
      minWidth: 0,
      border: "none",
      outline: "none",
      background: "transparent",
      color: "var(--text-primary)",
      fontFamily: mono ? "var(--font-mono)" : "var(--font-sans)",
      fontSize: "var(--text-sm)"
    }
  })), suffix && /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--text-muted)",
      display: "inline-flex"
    }
  }, suffix));
}
Object.assign(__ds_scope, { Input });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Input.jsx", error: String((e && e.message) || e) }); }

// components/forms/Select.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/** Native select styled to match Input. */
function Select({
  size = "md",
  invalid = false,
  className = "",
  style = {},
  children,
  ...rest
}) {
  const heights = {
    sm: "var(--control-h-sm)",
    md: "var(--control-h)",
    lg: "var(--control-h-lg)"
  };
  return /*#__PURE__*/React.createElement("div", {
    className: className,
    style: {
      position: "relative",
      display: "inline-flex",
      width: "100%",
      ...style
    }
  }, /*#__PURE__*/React.createElement("select", _extends({}, rest, {
    style: {
      width: "100%",
      height: heights[size],
      padding: "0 32px 0 10px",
      appearance: "none",
      WebkitAppearance: "none",
      background: "var(--pure-white)",
      border: `1px solid ${invalid ? "var(--status-fail)" : "var(--border-strong)"}`,
      borderRadius: "var(--radius)",
      color: "var(--text-primary)",
      fontFamily: "var(--font-sans)",
      fontSize: "var(--text-sm)",
      cursor: "pointer",
      outline: "none"
    }
  }), children), /*#__PURE__*/React.createElement("span", {
    style: {
      position: "absolute",
      right: 10,
      top: "50%",
      transform: "translateY(-50%)",
      pointerEvents: "none",
      color: "var(--text-muted)",
      fontSize: "10px"
    }
  }, "\u25BC"));
}
Object.assign(__ds_scope, { Select });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Select.jsx", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/app.js
try { (() => {
/* ============================================================
   BoM Configurator — app shell (mode switch + state wiring)
   ============================================================ */
(function () {
  const {
    useState,
    useMemo,
    useEffect
  } = React;
  function ModeTab({
    id,
    mode,
    setMode,
    icon,
    children
  }) {
    const on = mode === id;
    return /*#__PURE__*/React.createElement("button", {
      onClick: () => setMode(id),
      style: {
        display: "inline-flex",
        alignItems: "center",
        gap: 7,
        padding: "7px 16px",
        border: "none",
        cursor: "pointer",
        borderRadius: "var(--radius)",
        fontFamily: "var(--font-sans)",
        fontSize: "var(--text-sm)",
        fontWeight: 600,
        background: on ? "var(--slate)" : "transparent",
        color: on ? "var(--white)" : "var(--text-secondary)",
        transition: "background .15s"
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: `bi ${icon}`
    }), children);
  }
  function App() {
    const C = window.CFG,
      CAR = window.CAR;
    const {
      NodeMark,
      Button
    } = window.SequencerProDesignSystem_5eb90b;
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
      const id = requestAnimationFrame(() => requestAnimationFrame(() => {
        window.print();
      }));
      const after = () => setPrintJob(null);
      window.addEventListener("afterprint", after);
      return () => {
        cancelAnimationFrame(id);
        window.removeEventListener("afterprint", after);
      };
    }, [printJob]);
    const rec = useMemo(() => C.reconcile(rawSel, enabled), [rawSel, enabled]);
    const sel = rec.sel;
    const onChoose = id => setRawSel(C.applyChoice(sel, id));
    const onAttr = (id, v) => setRawSel(C.setAttr(sel, id, v));
    const onFix = v => setRawSel(v.fix(sel));
    const onToggleRule = rid => setEnabled(e => e.includes(rid) ? e.filter(x => x !== rid) : [...e, rid]);
    const onAddRule = rule => {
      CAR.rules.push(rule);
      setEnabled(e => [...e, rule.id]);
      setBump(b => b + 1);
    };
    const reset = () => {
      setRawSel(C.defaultSelections());
    };
    const st = C.status(sel, rec.violations);
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        height: "100vh",
        overflow: "hidden",
        background: "var(--bg-app)"
      }
    }, /*#__PURE__*/React.createElement("header", {
      style: {
        height: 58,
        flex: "none",
        display: "flex",
        alignItems: "center",
        gap: 16,
        padding: "0 20px",
        background: "var(--bg-surface)",
        borderBottom: "var(--border)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 10
      }
    }, /*#__PURE__*/React.createElement(NodeMark, {
      size: 30
    }), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-md)",
        fontWeight: 700,
        lineHeight: 1.1
      }
    }, "Platform Configurator"), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)"
      }
    }, CAR.platform.name, " \xB7 ", CAR.platform.code))), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 2,
        background: "var(--bg-sunken)",
        borderRadius: "var(--radius)",
        padding: 3,
        marginLeft: 12
      }
    }, /*#__PURE__*/React.createElement(ModeTab, {
      id: "configure",
      mode: mode,
      setMode: setMode,
      icon: "bi-sliders2"
    }, "Configure"), /*#__PURE__*/React.createElement(ModeTab, {
      id: "author",
      mode: mode,
      setMode: setMode,
      icon: "bi-diagram-3"
    }, "Author rules"), /*#__PURE__*/React.createElement(ModeTab, {
      id: "preview",
      mode: mode,
      setMode: setMode,
      icon: "bi-images"
    }, "Preview images"), /*#__PURE__*/React.createElement(ModeTab, {
      id: "tree",
      mode: mode,
      setMode: setMode,
      icon: "bi-diagram-2"
    }, "Product tree")), /*#__PURE__*/React.createElement("div", {
      style: {
        marginLeft: "auto",
        display: "flex",
        alignItems: "center",
        gap: 12
      }
    }, mode === "configure" && /*#__PURE__*/React.createElement("span", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontWeight: 700,
        color: "var(--text-primary)"
      }
    }, C.fmt(C.priceRollup(sel).total)), " MSRP"), /*#__PURE__*/React.createElement(Button, {
      variant: "ghost",
      size: "sm",
      onClick: reset,
      iconLeft: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-arrow-counterclockwise"
      })
    }, "Reset"))), /*#__PURE__*/React.createElement("main", {
      style: {
        flex: 1,
        overflow: "hidden"
      }
    }, mode === "configure" ? /*#__PURE__*/React.createElement(window.ConfigureView, {
      sel: sel,
      disabled: rec.disabled,
      violations: rec.violations,
      notes: rec.notes,
      onChoose: onChoose,
      onFix: onFix,
      onAttr: onAttr,
      onCreateWO: () => setWoOpen(true),
      enabled: enabled
    }) : mode === "preview" ? /*#__PURE__*/React.createElement(window.PreviewAuthor, null) : mode === "tree" ? /*#__PURE__*/React.createElement(window.ProductTree, {
      sel: sel
    }) : /*#__PURE__*/React.createElement(window.AuthorView, {
      enabled: enabled,
      onToggleRule: onToggleRule,
      onAddRule: onAddRule,
      onRefresh: () => setBump(b => b + 1)
    })), woOpen && /*#__PURE__*/React.createElement(window.WorkOrderModal, {
      sel: sel,
      enabled: enabled,
      onClose: () => setWoOpen(false),
      onPrint: (spec, wo) => setPrintJob({
        spec,
        wo
      })
    }), printJob && ReactDOM.createPortal(/*#__PURE__*/React.createElement("div", {
      id: "print-root"
    }, /*#__PURE__*/React.createElement(window.SpecSheet, {
      spec: printJob.spec,
      wo: printJob.wo
    })), document.body));
  }

  // Expose App for the page's inline mount script. NOTE: no auto-mount here —
  // this file is also concatenated into _ds_bundle.js, and auto-mounting would
  // render the configurator onto any DS page that has a #root. The configurator
  // page (index.html) mounts <BomApp> into #bom-app explicitly.
  window.BomApp = App;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/app.js", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/author.js
try { (() => {
/* ============================================================
   BoM Configurator — Author experience
   Node/graph canvas with visual dependency lines + rule builder.
   window.AuthorView
   ============================================================ */
(function () {
  const {
    useState
  } = React;
  const DS = window.SequencerProDesignSystem_5eb90b;
  const {
    Button
  } = DS;
  const C = window.CFG,
    CAR = window.CAR,
    fmt = C.fmt;
  const COLW = 178,
    COLGAP = 66,
    PADX = 26,
    HEADY = 22,
    NODEH = 44,
    NODEGAP = 13,
    TOPY = 50;
  const colX = i => PADX + i * (COLW + COLGAP);
  const nodeY = r => TOPY + r * (NODEH + NODEGAP);
  function layout() {
    const pos = {};
    CAR.groups.forEach((g, ci) => g.options.forEach((o, ri) => {
      pos[o.id] = {
        x: colX(ci),
        y: nodeY(ri),
        w: COLW,
        h: NODEH,
        cx: colX(ci) + COLW / 2,
        cyMid: nodeY(ri) + NODEH / 2
      };
    }));
    const totalW = colX(CAR.groups.length - 1) + COLW + PADX;
    const maxRows = Math.max(...CAR.groups.map(g => g.options.length));
    const totalH = nodeY(maxRows - 1) + NODEH + 30;
    return {
      pos,
      totalW,
      totalH
    };
  }
  function edgePath(a, b) {
    const fromX = a.x + a.w,
      fromY = a.cyMid,
      toX = b.x,
      toY = b.cyMid;
    const dir = toX >= fromX ? 1 : -1;
    return `M${fromX},${fromY} C${fromX + 48 * dir},${fromY} ${toX - 48 * dir},${toY} ${toX},${toY}`;
  }

  // Inline editable expression with live validation (commits on blur / Enter).
  function EditableExpr({
    value,
    onCommit,
    placeholder,
    boolean
  }) {
    const [v, setV] = useState(value);
    React.useEffect(() => setV(value), [value]);
    const res = C.validateExpr(v, {
      boolean
    });
    const bad = !res.ok;
    return /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("input", {
      value: v,
      placeholder: placeholder || "expression",
      spellCheck: false,
      onChange: e => setV(e.target.value),
      onBlur: () => {
        if (res.ok) onCommit(v);else setV(value);
      },
      onKeyDown: e => {
        if (e.key === "Enter") e.currentTarget.blur();
        if (e.key === "Escape") {
          setV(value);
          e.currentTarget.blur();
        }
      },
      style: {
        width: "100%",
        boxSizing: "border-box",
        fontFamily: "var(--font-mono)",
        fontSize: 10,
        background: "var(--bg-surface)",
        border: `1px solid ${bad ? "var(--orange)" : "var(--border-strong)"}`,
        borderRadius: 3,
        padding: "4px 6px",
        color: "var(--text-primary)",
        outlineColor: bad ? "var(--orange)" : "var(--blue)"
      }
    }), bad ? /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: 9.5,
        color: "var(--orange-700)",
        marginTop: 2,
        display: "flex",
        alignItems: "center",
        gap: 4
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-exclamation-triangle-fill"
    }), res.error, ". Press Esc to revert.") : /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: 9.5,
        color: "var(--text-muted)",
        marginTop: 2
      }
    }, "= ", String(res.value), " ", /*#__PURE__*/React.createElement("span", {
      style: {
        opacity: .6
      }
    }, "(sample)")));
  }
  function AddCalc({
    onRefresh
  }) {
    const [open, setOpen] = useState(false);
    const [name, setName] = useState("");
    const [expr, setExpr] = useState("");
    if (!open) return /*#__PURE__*/React.createElement("button", {
      onClick: () => setOpen(true),
      style: {
        marginTop: 4,
        background: "none",
        border: "none",
        color: "var(--text-link)",
        cursor: "pointer",
        fontSize: 11,
        fontWeight: 600,
        padding: 0,
        fontFamily: "var(--font-sans)"
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-plus-lg"
    }), " Add calculated attribute");
    const res = C.validateExpr(expr);
    const sty = {
      width: "100%",
      boxSizing: "border-box",
      height: 28,
      borderRadius: 3,
      border: "1px solid var(--border-strong)",
      fontSize: 11,
      padding: "0 7px",
      marginBottom: 5,
      fontFamily: "var(--font-sans)"
    };
    return /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: 6,
        paddingTop: 8,
        borderTop: "1px solid var(--border-default)"
      }
    }, /*#__PURE__*/React.createElement("input", {
      value: name,
      onChange: e => setName(e.target.value),
      placeholder: "Name (e.g. Floor mats)",
      style: sty
    }), /*#__PURE__*/React.createElement("input", {
      value: expr,
      onChange: e => setExpr(e.target.value),
      placeholder: "Formula (e.g. seats)",
      spellCheck: false,
      style: {
        ...sty,
        fontFamily: "var(--font-mono)",
        fontSize: 10,
        marginBottom: 2,
        borderColor: expr && !res.ok ? "var(--orange)" : "var(--border-strong)"
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: 9.5,
        color: expr && !res.ok ? "var(--orange-700)" : "var(--text-muted)",
        marginBottom: 6,
        minHeight: 12
      }
    }, !expr ? "" : res.ok ? "= " + String(res.value) + " (sample)" : res.error), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 6
      }
    }, /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary",
      onClick: () => setOpen(false)
    }, "Cancel"), /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      block: true,
      disabled: !name || !res.ok,
      onClick: () => {
        CAR.calc.push({
          id: "c" + (CAR.calc.length + 1),
          name: name || "New value",
          expr: expr || "0",
          unit: ""
        });
        onRefresh();
        setOpen(false);
        setName("");
        setExpr("");
      }
    }, "Add")));
  }
  function AuthorView({
    enabled,
    onToggleRule,
    onAddRule,
    onRefresh
  }) {
    const {
      pos,
      totalW,
      totalH
    } = layout();
    const [hover, setHover] = useState(null); // ruleId hovered
    const [sel, setSel] = useState(null); // optionId selected on canvas
    const edges = C.dependencyEdges(enabled);
    const sampleVals = {};
    C.computeCalc(C.defaultSelections()).forEach(c => sampleVals[c.id] = c.value);
    const colorFor = t => t === "excludes" ? "var(--orange)" : "var(--teal)";

    // node is highlighted if part of hovered rule or selected
    const hoveredRule = hover ? CAR.rules.find(r => r.id === hover) : null;
    const nodeHot = id => hoveredRule && ((hoveredRule.when || []).includes(id) || (hoveredRule.then || []).includes(id) || (hoveredRule.refs || []).includes(id)) || sel === id;
    const selRules = sel ? CAR.rules.filter(r => (r.when || []).includes(sel) || (r.then || []).includes(sel) || (r.refs || []).includes(sel)) : [];
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "1fr 320px",
        height: "100%",
        overflow: "hidden"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        overflow: "auto",
        background: "var(--bg-app)",
        backgroundImage: "radial-gradient(var(--border-default) 1px, transparent 1px)",
        backgroundSize: "22px 22px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        position: "relative",
        width: totalW,
        height: totalH,
        minHeight: "100%"
      }
    }, CAR.groups.map((g, ci) => /*#__PURE__*/React.createElement("div", {
      key: g.id,
      style: {
        position: "absolute",
        left: colX(ci),
        top: 14,
        width: COLW,
        display: "flex",
        alignItems: "center",
        gap: 6,
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        letterSpacing: "0.06em",
        textTransform: "uppercase",
        color: "var(--text-muted)"
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: `bi ${g.icon}`
    }), " ", g.name, g.appliesIf && /*#__PURE__*/React.createElement("i", {
      className: "bi bi-funnel-fill",
      title: "Conditional group",
      style: {
        fontSize: 9
      }
    }))), /*#__PURE__*/React.createElement("svg", {
      width: totalW,
      height: totalH,
      style: {
        position: "absolute",
        inset: 0,
        pointerEvents: "none"
      }
    }, /*#__PURE__*/React.createElement("defs", null, /*#__PURE__*/React.createElement("marker", {
      id: "ah-teal",
      markerWidth: "9",
      markerHeight: "9",
      refX: "7",
      refY: "4.5",
      orient: "auto"
    }, /*#__PURE__*/React.createElement("path", {
      d: "M0,0 L8,4.5 L0,9 Z",
      fill: "var(--teal)"
    })), /*#__PURE__*/React.createElement("marker", {
      id: "ah-orange",
      markerWidth: "9",
      markerHeight: "9",
      refX: "7",
      refY: "4.5",
      orient: "auto"
    }, /*#__PURE__*/React.createElement("path", {
      d: "M0,0 L8,4.5 L0,9 Z",
      fill: "var(--orange)"
    }))), edges.map((e, i) => {
      const a = pos[e.from],
        b = pos[e.to];
      if (!a || !b) return null;
      const hot = hover === e.rule || sel && (e.from === sel || e.to === sel);
      const dim = (hover || sel) && !hot;
      return /*#__PURE__*/React.createElement("path", {
        key: i,
        d: edgePath(a, b),
        fill: "none",
        stroke: colorFor(e.type),
        strokeWidth: hot ? 2.6 : 1.6,
        strokeDasharray: e.type === "excludes" ? "5 4" : "none",
        opacity: dim ? 0.12 : hot ? 1 : 0.5,
        markerEnd: `url(#ah-${e.type === "excludes" ? "orange" : "teal"})`
      });
    })), CAR.groups.map(g => g.options.map(o => {
      const p = pos[o.id],
        hot = nodeHot(o.id);
      return /*#__PURE__*/React.createElement("div", {
        key: o.id,
        onClick: () => setSel(s => s === o.id ? null : o.id),
        style: {
          position: "absolute",
          left: p.x,
          top: p.y,
          width: p.w,
          height: p.h,
          boxSizing: "border-box",
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "0 10px",
          cursor: "pointer",
          background: "var(--bg-surface)",
          borderRadius: "var(--radius)",
          border: hot ? "2px solid var(--gold)" : "1px solid var(--border-default)",
          boxShadow: hot ? "var(--shadow-md)" : "var(--shadow-xs)",
          transition: "box-shadow .12s, border-color .12s",
          zIndex: hot ? 3 : 2
        }
      }, /*#__PURE__*/React.createElement("span", {
        style: {
          width: 8,
          height: 8,
          borderRadius: "50%",
          background: o.swatch || "var(--ink-400)",
          flex: "none",
          border: "1px solid rgba(0,0,0,0.1)"
        }
      }), /*#__PURE__*/React.createElement("div", {
        style: {
          flex: 1,
          minWidth: 0
        }
      }, /*#__PURE__*/React.createElement("div", {
        style: {
          fontSize: "var(--text-xs)",
          fontWeight: 600,
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis"
        }
      }, o.name), /*#__PURE__*/React.createElement("div", {
        style: {
          fontFamily: "var(--font-mono)",
          fontSize: 10,
          color: "var(--text-muted)"
        }
      }, o.id)), o.price > 0 && /*#__PURE__*/React.createElement("span", {
        style: {
          fontFamily: "var(--font-mono)",
          fontSize: 10,
          color: "var(--text-secondary)",
          flex: "none"
        }
      }, "+", fmt(o.price)));
    })))), /*#__PURE__*/React.createElement("aside", {
      style: {
        borderLeft: "var(--border)",
        background: "var(--bg-surface)",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        padding: "14px 16px",
        borderBottom: "var(--border)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-md)",
        fontWeight: 700
      }
    }, "Constraint rules"), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)",
        marginTop: 2
      }
    }, "Toggle, edit or add rules & formulas \u2014 changes apply live in ", /*#__PURE__*/React.createElement("b", null, "Configure"), ". ", sel ? `Showing rules on ${C.opt(sel).name}.` : "Hover a rule to trace it on the canvas.")), /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1,
        overflowY: "auto",
        padding: 12
      }
    }, !sel && /*#__PURE__*/React.createElement("div", {
      style: {
        marginBottom: 12,
        padding: "10px 12px",
        borderRadius: "var(--radius-sm)",
        background: "var(--bg-sunken)",
        border: "1px dashed var(--border-strong)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        textTransform: "uppercase",
        letterSpacing: "0.06em",
        color: "var(--text-muted)",
        marginBottom: 8,
        display: "flex",
        alignItems: "center",
        gap: 6
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-calculator"
    }), "Calculated attributes"), CAR.calc.map(c => /*#__PURE__*/React.createElement("div", {
      key: c.id,
      style: {
        marginBottom: 8
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "baseline",
        marginBottom: 3
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontWeight: 600,
        fontSize: 11
      }
    }, c.name), /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 10,
        fontWeight: 700,
        color: "var(--gold-700)"
      }
    }, "= ", sampleVals[c.id])), /*#__PURE__*/React.createElement(EditableExpr, {
      value: c.expr,
      onCommit: v => {
        c.expr = v;
        onRefresh();
      }
    }))), /*#__PURE__*/React.createElement(AddCalc, {
      onRefresh: onRefresh
    })), (sel ? selRules : CAR.rules).map(r => {
      const on = enabled.includes(r.id);
      return /*#__PURE__*/React.createElement("div", {
        key: r.id,
        onMouseEnter: () => setHover(r.id),
        onMouseLeave: () => setHover(null),
        style: {
          padding: "10px 12px",
          marginBottom: 8,
          borderRadius: "var(--radius-sm)",
          border: "1px solid var(--border-default)",
          background: hover === r.id ? "var(--bg-sunken)" : "var(--bg-surface)",
          opacity: on ? 1 : 0.55
        }
      }, /*#__PURE__*/React.createElement("div", {
        style: {
          display: "flex",
          alignItems: "center",
          gap: 8,
          marginBottom: 5
        }
      }, /*#__PURE__*/React.createElement("span", {
        style: {
          fontFamily: "var(--font-mono)",
          fontSize: 10,
          fontWeight: 700,
          color: "#fff",
          background: r.type === "excludes" ? "var(--orange)" : "var(--teal)",
          borderRadius: 4,
          padding: "1px 5px"
        }
      }, r.id), /*#__PURE__*/React.createElement("span", {
        style: {
          fontSize: "var(--text-2xs)",
          fontWeight: 600,
          textTransform: "uppercase",
          letterSpacing: "0.05em",
          color: "var(--text-muted)"
        }
      }, r.type === "requiresOneOf" ? "requires one of" : r.type), /*#__PURE__*/React.createElement("button", {
        onClick: () => onToggleRule(r.id),
        title: on ? "Disable" : "Enable",
        style: {
          marginLeft: "auto",
          width: 34,
          height: 18,
          borderRadius: 9,
          border: "none",
          cursor: "pointer",
          background: on ? "var(--teal)" : "var(--grey-300)",
          position: "relative",
          flex: "none",
          transition: "background .15s"
        }
      }, /*#__PURE__*/React.createElement("span", {
        style: {
          position: "absolute",
          top: 2,
          left: on ? 18 : 2,
          width: 14,
          height: 14,
          borderRadius: "50%",
          background: "#fff",
          transition: "left .15s"
        }
      }))), /*#__PURE__*/React.createElement("div", {
        style: {
          fontSize: "var(--text-2xs)",
          color: "var(--text-secondary)",
          lineHeight: 1.45,
          marginBottom: 6
        }
      }, r.msg), r.type === "formula" ? /*#__PURE__*/React.createElement(EditableExpr, {
        value: r.expr,
        onCommit: v => {
          r.expr = v;
          onRefresh();
        }
      }) : /*#__PURE__*/React.createElement("div", {
        style: {
          display: "flex",
          alignItems: "center",
          gap: 5,
          flexWrap: "wrap",
          fontSize: 10
        }
      }, (r.when || []).map(w => /*#__PURE__*/React.createElement("span", {
        key: w,
        style: {
          fontFamily: "var(--font-mono)",
          background: "var(--blue-50)",
          color: "var(--blue-700)",
          borderRadius: 3,
          padding: "1px 5px"
        }
      }, w)), /*#__PURE__*/React.createElement("i", {
        className: "bi bi-arrow-right",
        style: {
          color: "var(--text-muted)"
        }
      }), (r.then || []).map(t => /*#__PURE__*/React.createElement("span", {
        key: t,
        style: {
          fontFamily: "var(--font-mono)",
          background: r.type === "excludes" ? "var(--orange-50)" : "var(--teal-50)",
          color: r.type === "excludes" ? "var(--orange-700)" : "var(--teal-700)",
          borderRadius: 3,
          padding: "1px 5px"
        }
      }, t))));
    })), /*#__PURE__*/React.createElement(RuleBuilder, {
      onAddRule: onAddRule,
      onRefresh: onRefresh
    })));
  }
  function RuleBuilder({
    onAddRule,
    onRefresh
  }) {
    const [open, setOpen] = useState(false);
    const [mode, setMode] = useState("requires"); // requires | excludes | formula | bounds
    const allOpts = CAR.groups.flatMap(g => g.options);
    const attrs = C.attributes;
    const [whenId, setWhenId] = useState(allOpts[0].id);
    const [thenId, setThenId] = useState(allOpts[3].id);
    const [expr, setExpr] = useState("reach <= 1400 || has('arm-20')");
    const [msg, setMsg] = useState("");
    const [attrId, setAttrId] = useState(attrs[0] ? attrs[0].id : "");
    const [lo, setLo] = useState("");
    const [hi, setHi] = useState("");
    const sty = {
      width: "100%",
      boxSizing: "border-box",
      height: 30,
      borderRadius: "var(--radius-sm)",
      border: "1px solid var(--border-strong)",
      fontFamily: "var(--font-sans)",
      fontSize: "var(--text-xs)",
      padding: "0 8px",
      background: "#fff"
    };
    const mono = {
      ...sty,
      fontFamily: "var(--font-mono)",
      fontSize: 10
    };
    const uid = () => "U" + (CAR.rules.length + 1);
    const fres = C.validateExpr(expr, {
      boolean: true
    });
    const canAdd = mode === "formula" ? fres.ok : mode === "bounds" ? lo !== "" || hi !== "" : true;
    if (!open) return /*#__PURE__*/React.createElement("div", {
      style: {
        padding: 12,
        borderTop: "var(--border)"
      }
    }, /*#__PURE__*/React.createElement(Button, {
      block: true,
      variant: "secondary",
      onClick: () => setOpen(true),
      iconLeft: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-plus-lg"
      })
    }, "New rule"));
    const add = () => {
      let rule;
      if (mode === "requires" || mode === "excludes") rule = {
        id: uid(),
        type: mode,
        when: [whenId],
        then: [thenId],
        msg: msg || `${C.opt(whenId).name} ${mode === "excludes" ? "excludes" : "requires"} ${C.opt(thenId).name}.`
      };else if (mode === "formula") rule = {
        id: uid(),
        type: "formula",
        expr: expr || "true",
        msg: msg || "Configuration constraint not met.",
        refs: []
      };else {
        const a = [...attrs, ...CAR.calc].find(x => x.id === attrId) || {
          name: attrId
        };
        const parts = [];
        if (lo !== "") parts.push(`${attrId} >= ${lo}`);
        if (hi !== "") parts.push(`${attrId} <= ${hi}`);
        const auto = `${a.name} must be ${lo !== "" ? "\u2265 " + lo : ""}${lo !== "" && hi !== "" ? " and " : ""}${hi !== "" ? "\u2264 " + hi : ""}.`;
        rule = {
          id: uid(),
          type: "formula",
          expr: parts.join(" && ") || "true",
          msg: msg || auto,
          refs: [attrId]
        };
      }
      onAddRule(rule);
      setOpen(false);
      setMsg("");
      setLo("");
      setHi("");
    };
    return /*#__PURE__*/React.createElement("div", {
      style: {
        padding: 14,
        borderTop: "var(--border)",
        background: "var(--bg-sunken)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        textTransform: "uppercase",
        letterSpacing: "0.06em",
        color: "var(--text-muted)",
        marginBottom: 8
      }
    }, "New constraint"), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: 7
      }
    }, /*#__PURE__*/React.createElement("select", {
      value: mode,
      onChange: e => setMode(e.target.value),
      style: sty
    }, /*#__PURE__*/React.createElement("option", {
      value: "requires"
    }, "Requires (if A then B)"), /*#__PURE__*/React.createElement("option", {
      value: "excludes"
    }, "Excludes (A blocks B)"), /*#__PURE__*/React.createElement("option", {
      value: "formula"
    }, "Formula (boolean expression)"), /*#__PURE__*/React.createElement("option", {
      value: "bounds"
    }, "Min / max bound on an attribute")), (mode === "requires" || mode === "excludes") && /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("select", {
      value: whenId,
      onChange: e => setWhenId(e.target.value),
      style: sty
    }, allOpts.map(o => /*#__PURE__*/React.createElement("option", {
      key: o.id,
      value: o.id
    }, o.name, " (", o.id, ")"))), /*#__PURE__*/React.createElement("select", {
      value: thenId,
      onChange: e => setThenId(e.target.value),
      style: sty
    }, allOpts.map(o => /*#__PURE__*/React.createElement("option", {
      key: o.id,
      value: o.id
    }, o.name, " (", o.id, ")")))), mode === "formula" && /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("input", {
      value: expr,
      onChange: e => setExpr(e.target.value),
      spellCheck: false,
      placeholder: "boolean expression, must be true",
      style: {
        ...mono,
        borderColor: !fres.ok ? "var(--orange)" : "var(--border-strong)"
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: 10,
        color: !fres.ok ? "var(--orange-700)" : "var(--text-muted)",
        lineHeight: 1.4
      }
    }, !fres.ok ? /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-exclamation-triangle-fill"
    }), " ", fres.error) : /*#__PURE__*/React.createElement("span", null, "Use attributes (seats, rackLen\u2026), calc values, ", /*#__PURE__*/React.createElement("code", {
      style: {
        fontFamily: "var(--font-mono)"
      }
    }, "has('optId')"), ", ceil/floor/min/max. Must evaluate true."))), mode === "bounds" && /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("select", {
      value: attrId,
      onChange: e => setAttrId(e.target.value),
      style: sty
    }, [...attrs, ...CAR.calc].map(a => /*#__PURE__*/React.createElement("option", {
      key: a.id,
      value: a.id
    }, a.name, " (", a.id, ")"))), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 6
      }
    }, /*#__PURE__*/React.createElement("input", {
      value: lo,
      onChange: e => setLo(e.target.value),
      placeholder: "min",
      style: {
        ...sty,
        width: "50%"
      }
    }), /*#__PURE__*/React.createElement("input", {
      value: hi,
      onChange: e => setHi(e.target.value),
      placeholder: "max",
      style: {
        ...sty,
        width: "50%"
      }
    }))), /*#__PURE__*/React.createElement("input", {
      value: msg,
      onChange: e => setMsg(e.target.value),
      placeholder: "Validation message (optional)",
      style: sty
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 7
      }
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "sm",
      onClick: () => setOpen(false)
    }, "Cancel"), /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      block: true,
      disabled: !canAdd,
      onClick: add
    }, "Add rule"))));
  }
  window.AuthorView = AuthorView;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/author.js", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/bom.js
try { (() => {
/* ============================================================
   BoM Configurator — output panels (BoM tree, price, routing, WO)
   window.PriceRollup, BomTree, Routing, ViolationsList, WorkOrderModal
   ============================================================ */
(function () {
  const DS = window.SequencerProDesignSystem_5eb90b;
  const {
    Button,
    StatusBadge,
    Tag
  } = DS;
  const fmt = window.CFG.fmt;
  const {
    useState
  } = React;
  const SectionLabel = ({
    children,
    right
  }) => React.createElement("div", {
    style: {
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      fontSize: "var(--text-2xs)",
      fontWeight: 700,
      letterSpacing: "var(--ls-caps)",
      textTransform: "uppercase",
      color: "var(--text-muted)",
      margin: "0 0 8px"
    }
  }, React.createElement("span", null, children), right);
  function PriceRollup({
    sel
  }) {
    const {
      lines,
      total
    } = window.CFG.priceRollup(sel);
    const oq = sel.attrs && sel.attrs.orderQty || 1;
    return React.createElement("div", null, React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: 1
      }
    }, lines.map((l, i) => React.createElement("div", {
      key: i,
      style: {
        display: "flex",
        justifyContent: "space-between",
        gap: 12,
        padding: "7px 0",
        borderBottom: "1px solid var(--border-default)",
        fontSize: "var(--text-sm)"
      }
    }, React.createElement("span", {
      style: {
        color: l.base ? "var(--text-primary)" : "var(--text-secondary)",
        fontWeight: l.base ? 600 : 400
      }
    }, l.label, l.group && React.createElement("span", {
      style: {
        color: "var(--text-muted)",
        fontSize: "var(--text-2xs)",
        marginLeft: 6
      }
    }, l.group)), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontWeight: l.base ? 600 : 400,
        display: "flex",
        alignItems: "center",
        gap: 3
      }
    }, l.formula && React.createElement("span", {
      title: "per-unit formula price",
      style: {
        color: "var(--teal)",
        fontWeight: 700
      }
    }, "ƒ"), l.base ? fmt(l.amount) : "+" + fmt(l.amount))))), React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "baseline",
        marginTop: 12,
        paddingTop: 12,
        borderTop: "2px solid var(--border-strong)"
      }
    }, React.createElement("span", {
      style: {
        fontSize: "var(--text-sm)",
        fontWeight: 700,
        textTransform: "uppercase",
        letterSpacing: "var(--ls-caps)"
      }
    }, "Per unit"), React.createElement("span", {
      style: {
        fontFamily: "var(--font-display)",
        fontWeight: 800,
        fontSize: "var(--text-2xl)",
        color: "var(--text-primary)"
      }
    }, fmt(total))), oq > 1 && React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "baseline",
        marginTop: 8,
        padding: "8px 10px",
        background: "var(--gold-50)",
        border: "1px solid var(--gold-200)",
        borderRadius: "var(--radius-sm)"
      }
    }, React.createElement("span", {
      style: {
        fontSize: "var(--text-xs)",
        fontWeight: 700,
        color: "var(--gold-700)"
      }
    }, "Order × " + oq + " robots"), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontWeight: 700,
        color: "var(--gold-700)"
      }
    }, fmt(total * oq))));
  }
  function AssemblyRow({
    a,
    lineTotal
  }) {
    const [open, setOpen] = useState(false);
    const has = a.children && a.children.length > 0;
    return React.createElement("div", null, React.createElement("div", {
      onClick: () => has && setOpen(o => !o),
      style: {
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "7px 8px",
        borderRadius: "var(--radius-sm)",
        cursor: has ? "pointer" : "default"
      },
      onMouseEnter: e => e.currentTarget.style.background = "var(--grey-100)",
      onMouseLeave: e => e.currentTarget.style.background = "transparent"
    }, React.createElement("i", {
      className: `bi ${has ? open ? "bi-caret-down-fill" : "bi-caret-right-fill" : "bi-dot"}`,
      style: {
        fontSize: has ? 9 : 14,
        color: "var(--text-muted)",
        width: 12
      }
    }), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: "var(--text-link)",
        flex: "none",
        width: 72
      }
    }, a.code), React.createElement("span", {
      style: {
        flex: 1,
        fontSize: "var(--text-sm)",
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis"
      }
    }, a.name), React.createElement("span", {
      title: a.qtyExpr || undefined,
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: a.qtyExpr ? "var(--teal-700)" : "var(--text-muted)",
        flex: "none"
      }
    }, "×" + a.qty, a.qtyExpr ? " ƒ" : ""), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-xs)",
        flex: "none",
        width: 64,
        textAlign: "right"
      }
    }, fmt(lineTotal(a)))), open && has && React.createElement("div", {
      style: {
        marginLeft: 32,
        borderLeft: "1px solid var(--border-default)",
        paddingLeft: 8
      }
    }, a.children.map((c, i) => React.createElement("div", {
      key: i,
      style: {
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "5px 8px"
      }
    }, React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)",
        flex: "none",
        width: 72
      }
    }, c.code), React.createElement("span", {
      style: {
        flex: 1,
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)"
      }
    }, c.name), React.createElement("span", {
      title: c.qtyExpr || undefined,
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: c.qtyExpr ? "var(--teal-700)" : "var(--text-muted)",
        flex: "none"
      }
    }, "×" + c.qty, c.qtyExpr ? " ƒ" : ""), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)",
        flex: "none",
        width: 64,
        textAlign: "right"
      }
    }, fmt(c.price * c.qty))))));
  }
  function BomTree({
    sel
  }) {
    const {
      systems,
      partCount,
      cost,
      lineTotal
    } = window.CFG.resolveBom(sel);
    return React.createElement("div", null, React.createElement(SectionLabel, {
      right: React.createElement("span", {
        style: {
          fontFamily: "var(--font-mono)",
          color: "var(--text-secondary)"
        }
      }, partCount + " parts · " + fmt(cost) + " cost")
    }, "Resolved multi-level BoM"), systems.map((s, i) => React.createElement("div", {
      key: i,
      style: {
        marginBottom: 10
      }
    }, React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        padding: "4px 8px",
        background: "var(--bg-sunken)",
        borderRadius: "var(--radius-sm)",
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        letterSpacing: "0.04em",
        textTransform: "uppercase",
        color: "var(--text-secondary)"
      }
    }, React.createElement("span", null, s.name), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)"
      }
    }, fmt(s.total))), s.assemblies.map((a, j) => React.createElement(AssemblyRow, {
      key: j,
      a,
      lineTotal
    })))));
  }
  function Routing({
    sel
  }) {
    const ops = window.CFG.generateRouting(sel);
    return React.createElement("div", null, React.createElement(SectionLabel, {
      right: React.createElement("span", {
        style: {
          fontFamily: "var(--font-mono)",
          color: "var(--text-secondary)"
        }
      }, ops.length + " operations")
    }, "Generated routing → Process Manager"), React.createElement("div", {
      style: {
        position: "relative"
      }
    }, ops.map((o, i) => React.createElement("div", {
      key: o.code,
      style: {
        display: "flex",
        gap: 10,
        alignItems: "flex-start",
        padding: "2px 0"
      }
    }, React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        flex: "none"
      }
    }, React.createElement("span", {
      style: {
        width: 22,
        height: 22,
        borderRadius: "50%",
        background: "var(--slate)",
        color: "#fff",
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center"
      }
    }, i + 1), i < ops.length - 1 && React.createElement("span", {
      style: {
        width: 2,
        height: 16,
        background: "var(--border-strong)"
      }
    })), React.createElement("div", {
      style: {
        paddingBottom: 6
      }
    }, React.createElement("div", {
      style: {
        fontSize: "var(--text-sm)",
        fontWeight: 600
      }
    }, o.name), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)"
      }
    }, o.code))))));
  }
  function ViolationsList({
    violations,
    onFix
  }) {
    if (!violations.length) return React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 8,
        padding: "28px 16px",
        textAlign: "center"
      }
    }, React.createElement("span", {
      style: {
        width: 40,
        height: 40,
        borderRadius: "50%",
        background: "var(--teal-50)",
        color: "var(--teal-600)",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: 20
      }
    }, React.createElement("i", {
      className: "bi bi-check-lg"
    })), React.createElement("div", {
      style: {
        fontSize: "var(--text-sm)",
        fontWeight: 600,
        color: "var(--text-primary)"
      }
    }, "No conflicts"), React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-muted)"
      }
    }, "This configuration is valid and buildable."));
    return React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: 8
      }
    }, violations.map((v, i) => React.createElement("div", {
      key: i,
      style: {
        display: "flex",
        gap: 10,
        padding: "10px 12px",
        borderRadius: "var(--radius-sm)",
        background: "#fdecec",
        border: "1px solid #f3c0c0"
      }
    }, React.createElement("i", {
      className: "bi bi-exclamation-triangle-fill",
      style: {
        color: "var(--status-fail)",
        fontSize: 14,
        marginTop: 1
      }
    }), React.createElement("div", {
      style: {
        flex: 1
      }
    }, React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        color: "#7a2222",
        lineHeight: 1.4
      }
    }, v.msg), React.createElement("button", {
      onClick: () => onFix(v),
      style: {
        marginTop: 6,
        padding: "3px 10px",
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        fontFamily: "var(--font-sans)",
        color: "#fff",
        background: "var(--status-fail)",
        border: "none",
        borderRadius: "var(--radius-xs)",
        cursor: "pointer"
      }
    }, React.createElement("i", {
      className: "bi bi-magic",
      style: {
        marginRight: 4
      }
    }), v.fixLabel)))));
  }
  function ExportRow({
    label,
    children,
    mono
  }) {
    return React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        gap: 12,
        padding: "5px 0",
        borderBottom: "1px solid var(--border-default)",
        fontSize: "var(--text-xs)"
      }
    }, React.createElement("span", {
      style: {
        color: "var(--text-secondary)"
      }
    }, label), React.createElement("span", {
      style: {
        fontFamily: mono ? "var(--font-mono)" : "var(--font-sans)",
        fontWeight: 600,
        textAlign: "right"
      }
    }, children));
  }
  const ExportHead = ({
    icon,
    children,
    right
  }) => React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "center",
      gap: 7,
      margin: "16px 0 6px"
    }
  }, React.createElement("i", {
    className: "bi " + icon,
    style: {
      fontSize: 13,
      color: "var(--text-muted)"
    }
  }), React.createElement("span", {
    style: {
      fontSize: "var(--text-2xs)",
      fontWeight: 700,
      letterSpacing: "var(--ls-caps)",
      textTransform: "uppercase",
      color: "var(--text-muted)",
      flex: 1
    }
  }, children), right);
  function WorkOrderModal({
    sel,
    enabled,
    onClose,
    onPrint
  }) {
    const spec = window.CFG.exportSpec(sel, enabled);
    const wo = React.useMemo(() => "WO-2026-" + String(Math.floor(Math.random() * 900) + 100), []);
    const [tab, setTab] = useState("summary");
    const [copied, setCopied] = useState("");
    const copy = (text, what) => {
      const done = () => {
        setCopied(what);
        setTimeout(() => setCopied(""), 1400);
      };
      try {
        navigator.clipboard.writeText(text).then(done, () => {
          fallbackCopy(text);
          done();
        });
      } catch (e) {
        fallbackCopy(text);
        done();
      }
    };
    function fallbackCopy(text) {
      const ta = document.createElement("textarea");
      ta.value = text;
      document.body.appendChild(ta);
      ta.select();
      try {
        document.execCommand("copy");
      } catch (e) {}
      document.body.removeChild(ta);
    }
    const download = (text, name, type) => {
      const blob = new Blob([text], {
        type: type || "text/plain"
      });
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = name;
      document.body.appendChild(a);
      a.click();
      setTimeout(() => {
        URL.revokeObjectURL(a.href);
        document.body.removeChild(a);
      }, 100);
    };
    const TabBtn = ({
      id,
      children
    }) => React.createElement("button", {
      onClick: () => setTab(id),
      style: {
        flex: 1,
        padding: "8px 0",
        border: "none",
        borderBottom: tab === id ? "2px solid var(--gold)" : "2px solid transparent",
        background: "none",
        cursor: "pointer",
        fontFamily: "var(--font-sans)",
        fontSize: "var(--text-xs)",
        fontWeight: tab === id ? 700 : 500,
        color: tab === id ? "var(--text-primary)" : "var(--text-muted)"
      }
    }, children);
    const money = fmt;
    const qtyCell = r => React.createElement("span", {
      title: r.qtyFormula || undefined,
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: r.qtyFormula ? "var(--teal-700)" : "var(--text-secondary)"
      }
    }, "×" + r.qty, r.qtyFormula ? " ƒ" : "");
    return React.createElement("div", {
      onClick: onClose,
      style: {
        position: "fixed",
        inset: 0,
        background: "rgba(33,33,33,0.55)",
        display: "grid",
        placeItems: "center",
        zIndex: 50,
        padding: 20
      }
    }, React.createElement("div", {
      onClick: e => e.stopPropagation(),
      style: {
        width: 680,
        maxWidth: "100%",
        maxHeight: "90vh",
        background: "var(--bg-surface)",
        borderRadius: "var(--radius-lg)",
        boxShadow: "var(--shadow-lg)",
        overflow: "hidden",
        display: "flex",
        flexDirection: "column"
      }
    },
    // header
    React.createElement("div", {
      style: {
        padding: "16px 22px",
        borderBottom: "var(--border)",
        display: "flex",
        alignItems: "center",
        gap: 11,
        flex: "none"
      }
    }, React.createElement("span", {
      style: {
        width: 34,
        height: 34,
        borderRadius: "50%",
        background: "var(--teal-50)",
        color: "var(--teal-600)",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: 18,
        flex: "none"
      }
    }, React.createElement("i", {
      className: "bi bi-check-circle-fill"
    })), React.createElement("div", {
      style: {
        flex: 1
      }
    }, React.createElement("div", {
      style: {
        fontSize: "var(--text-lg)",
        fontWeight: 700
      }
    }, "Resolved configuration"), React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)"
      }
    }, "Released to Process Manager as a work order")), React.createElement(Tag, {
      color: "gold"
    }, wo), React.createElement("button", {
      onClick: onClose,
      "aria-label": "Close",
      style: {
        border: "none",
        background: "none",
        cursor: "pointer",
        fontSize: 20,
        color: "var(--text-muted)",
        lineHeight: 1
      }
    }, "×")),
    // stat tiles
    React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "repeat(4,1fr)",
        gap: 8,
        padding: "14px 22px 0",
        flex: "none"
      }
    }, [["Parts", spec.bom.partCount], ["Operations", spec.routing.length], ["Per unit", money(spec.price.perUnit)], [spec.meta.orderQty > 1 ? "Order × " + spec.meta.orderQty : "Order total", money(spec.price.orderTotal)]].map(([k, v], i) => React.createElement("div", {
      key: i,
      style: {
        background: "var(--bg-sunken)",
        borderRadius: "var(--radius-sm)",
        padding: "9px 10px",
        textAlign: "center"
      }
    }, React.createElement("div", {
      style: {
        fontFamily: "var(--font-display)",
        fontWeight: 800,
        fontSize: "var(--text-lg)"
      }
    }, v), React.createElement("div", {
      style: {
        fontSize: 9.5,
        color: "var(--text-muted)",
        textTransform: "uppercase",
        letterSpacing: "0.05em"
      }
    }, k)))),
    // tabs
    React.createElement("div", {
      style: {
        display: "flex",
        padding: "10px 22px 0",
        gap: 4,
        flex: "none",
        borderBottom: "var(--border)"
      }
    }, React.createElement(TabBtn, {
      id: "summary"
    }, "Spec"), React.createElement(TabBtn, {
      id: "bom"
    }, "BoM"), React.createElement(TabBtn, {
      id: "rules"
    }, "Rules & routing"), React.createElement(TabBtn, {
      id: "data"
    }, "Raw")),
    // body (scroll)
    React.createElement("div", {
      style: {
        padding: "4px 22px 18px",
        overflow: "auto",
        flex: 1
      }
    }, tab === "summary" && React.createElement("div", null, React.createElement(ExportHead, {
      icon: "bi-sliders"
    }, "Attributes"), spec.attributes.map(a => React.createElement(ExportRow, {
      key: a.id,
      label: a.name,
      mono: true
    }, a.value + (a.unit ? " " + a.unit : ""), a.bounds && a.bounds.clampedBy.length ? React.createElement("span", {
      style: {
        color: "var(--blue-700)",
        marginLeft: 6,
        fontSize: 10
      }
    }, "[" + a.bounds.min + "–" + a.bounds.max + " · " + a.bounds.clampedBy.join(",") + "]") : null)), React.createElement(ExportHead, {
      icon: "bi-calculator"
    }, "Calculated"), spec.calculated.map(c => React.createElement("div", {
      key: c.id,
      style: {
        display: "flex",
        justifyContent: "space-between",
        gap: 10,
        alignItems: "center",
        padding: "5px 0",
        borderBottom: "1px solid var(--border-default)"
      }
    }, React.createElement("span", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)",
        flex: "none",
        width: 110
      }
    }, c.name), React.createElement("code", {
      style: {
        flex: 1,
        minWidth: 0,
        fontFamily: "var(--font-mono)",
        fontSize: 10,
        color: "var(--text-muted)",
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis"
      }
    }, c.expr), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontWeight: 700,
        color: "var(--gold-700)",
        flex: "none"
      }
    }, c.value))), React.createElement(ExportHead, {
      icon: "bi-list-check"
    }, "Selections"), spec.selections.map((s, i) => React.createElement(ExportRow, {
      key: i,
      label: s.group + " · " + s.name
    }, React.createElement("span", {
      style: {
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        fontFamily: "var(--font-mono)"
      }
    }, s.priceFormula ? React.createElement("span", {
      title: s.priceFormula,
      style: {
        color: "var(--teal)",
        fontWeight: 700
      }
    }, "ƒ") : null, s.unitPrice ? money(s.unitPrice) : "Incl."))), React.createElement(ExportHead, {
      icon: "bi-cash-stack"
    }, "Price rollup"), spec.price.lines.map((l, i) => React.createElement(ExportRow, {
      key: i,
      label: l.label + (l.group ? " · " + l.group : ""),
      mono: true
    }, React.createElement("span", {
      style: {
        display: "inline-flex",
        alignItems: "center",
        gap: 4
      }
    }, l.priceFormula ? React.createElement("span", {
      title: l.priceFormula,
      style: {
        color: "var(--teal)",
        fontWeight: 700
      }
    }, "ƒ") : null, (l.base ? "" : "+") + money(l.amount)))), React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        paddingTop: 9,
        marginTop: 3,
        fontWeight: 800
      }
    }, React.createElement("span", {
      style: {
        fontSize: "var(--text-sm)"
      }
    }, "Order total (×" + spec.meta.orderQty + ")"), React.createElement("span", {
      style: {
        fontFamily: "var(--font-display)",
        fontSize: "var(--text-xl)"
      }
    }, money(spec.price.orderTotal)))), tab === "bom" && React.createElement("div", {
      style: {
        marginTop: 8
      }
    }, React.createElement("div", {
      style: {
        display: "flex",
        padding: "6px 8px",
        fontSize: 9.5,
        fontWeight: 700,
        textTransform: "uppercase",
        letterSpacing: "0.05em",
        color: "var(--text-muted)",
        borderBottom: "1px solid var(--border-strong)"
      }
    }, React.createElement("span", {
      style: {
        width: 80,
        flex: "none"
      }
    }, "Code"), React.createElement("span", {
      style: {
        flex: 1
      }
    }, "Part"), React.createElement("span", {
      style: {
        width: 54,
        flex: "none",
        textAlign: "right"
      }
    }, "Qty"), React.createElement("span", {
      style: {
        width: 66,
        flex: "none",
        textAlign: "right"
      }
    }, "Ext")), spec.bom.rows.map((r, i) => React.createElement("div", {
      key: i,
      style: {
        display: "flex",
        alignItems: "center",
        padding: "5px 8px",
        paddingLeft: r.level === 2 ? 22 : 8,
        borderBottom: "1px solid var(--border-default)",
        background: r.level === 1 ? "transparent" : "var(--grey-50)"
      }
    }, React.createElement("span", {
      style: {
        width: r.level === 2 ? 66 : 80,
        flex: "none",
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: r.level === 1 ? "var(--text-link)" : "var(--text-muted)"
      }
    }, r.code), React.createElement("span", {
      style: {
        flex: 1,
        fontSize: r.level === 1 ? "var(--text-xs)" : "var(--text-2xs)",
        color: r.level === 1 ? "var(--text-primary)" : "var(--text-secondary)",
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis"
      }
    }, r.name, React.createElement("span", {
      style: {
        color: "var(--text-muted)",
        marginLeft: 6,
        fontSize: 9.5
      }
    }, r.system)), React.createElement("span", {
      style: {
        width: 54,
        flex: "none",
        textAlign: "right"
      }
    }, qtyCell(r)), React.createElement("span", {
      style: {
        width: 66,
        flex: "none",
        textAlign: "right",
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)"
      }
    }, money(r.ext)))), React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        padding: "9px 8px",
        fontWeight: 700,
        fontSize: "var(--text-xs)"
      }
    }, React.createElement("span", null, spec.bom.partCount + " parts"), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)"
      }
    }, money(spec.bom.cost) + " cost")), React.createElement("div", {
      style: {
        fontSize: 10,
        color: "var(--text-muted)",
        marginTop: 4
      }
    }, "ƒ marks a formula-driven quantity.")), tab === "rules" && React.createElement("div", null, React.createElement(ExportHead, {
      icon: "bi-diagram-3"
    }, "Active rules (" + spec.rules.length + ")"), spec.rules.map((r, i) => React.createElement("div", {
      key: i,
      style: {
        padding: "7px 0",
        borderBottom: "1px solid var(--border-default)"
      }
    }, React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 6,
        marginBottom: 3
      }
    }, React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 9.5,
        fontWeight: 700,
        color: "var(--text-muted)"
      }
    }, r.id), React.createElement(Tag, {
      color: r.type === "excludes" ? "orange" : r.type === "formula" ? "blue" : "teal",
      mono: false
    }, r.type), React.createElement("span", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--text-secondary)",
        flex: 1
      }
    }, r.msg)), r.expr ? React.createElement("code", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 10,
        color: "var(--text-secondary)",
        background: "var(--bg-sunken)",
        borderRadius: 3,
        padding: "2px 6px",
        display: "inline-block"
      }
    }, r.expr) : React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 10,
        color: "var(--text-muted)"
      }
    }, (r.when || []).join(", ") + " → " + (r.then || []).join(", ")))), React.createElement(ExportHead, {
      icon: "bi-signpost-split"
    }, "Routing → Process Manager"), spec.routing.map(o => React.createElement("div", {
      key: o.code,
      style: {
        display: "flex",
        alignItems: "center",
        gap: 9,
        padding: "4px 0"
      }
    }, React.createElement("span", {
      style: {
        width: 20,
        height: 20,
        borderRadius: "50%",
        background: "var(--slate)",
        color: "#fff",
        fontSize: 10,
        fontWeight: 700,
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        flex: "none"
      }
    }, o.step), React.createElement("span", {
      style: {
        fontSize: "var(--text-xs)",
        flex: 1
      }
    }, o.name), React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)"
      }
    }, o.code)))), tab === "data" && React.createElement("pre", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 10,
        lineHeight: 1.5,
        color: "var(--text-secondary)",
        background: "var(--bg-sunken)",
        borderRadius: "var(--radius-sm)",
        padding: 12,
        margin: "12px 0 0",
        overflow: "auto",
        whiteSpace: "pre-wrap",
        wordBreak: "break-word"
      }
    }, window.CFG.specToJSON(spec))),
    // footer actions
    React.createElement("div", {
      style: {
        padding: "12px 22px",
        borderTop: "var(--border)",
        display: "flex",
        gap: 8,
        flex: "none",
        alignItems: "center"
      }
    }, React.createElement("button", {
      onClick: () => copy(window.CFG.bomToCSV(spec), "csv"),
      style: actBtn
    }, React.createElement("i", {
      className: "bi bi-clipboard"
    }), copied === "csv" ? "Copied!" : "Copy BoM CSV"), React.createElement("button", {
      onClick: () => copy(window.CFG.specToJSON(spec), "json"),
      style: actBtn
    }, React.createElement("i", {
      className: "bi bi-clipboard-data"
    }), copied === "json" ? "Copied!" : "Copy JSON"), React.createElement("button", {
      onClick: () => download(window.CFG.bomToCSV(spec), wo + "-bom.csv", "text/csv"),
      style: actBtn
    }, React.createElement("i", {
      className: "bi bi-download"
    }), "CSV"), React.createElement("button", {
      onClick: () => onPrint(spec, wo),
      style: {
        ...actBtn,
        borderColor: "var(--slate)",
        background: "var(--slate)",
        color: "#fff"
      }
    }, React.createElement("i", {
      className: "bi bi-printer-fill"
    }), "Print / Save PDF"), React.createElement("div", {
      style: {
        flex: 1
      }
    }), React.createElement(Button, {
      onClick: onClose
    }, "Done"))));
  }
  const actBtn = {
    display: "inline-flex",
    alignItems: "center",
    gap: 5,
    padding: "8px 11px",
    borderRadius: "var(--radius-sm)",
    border: "1px solid var(--border-strong)",
    background: "var(--bg-surface)",
    cursor: "pointer",
    fontFamily: "var(--font-sans)",
    fontSize: "var(--text-xs)",
    fontWeight: 600,
    color: "var(--text-primary)"
  };

  // ── Print-ready spec sheet ────────────────────────────────────────────────
  // A clean paper document (Letter) rendered off-screen and revealed only under
  // @media print. Captures the full resolved spec for Save-as-PDF / hard copy.
  function SheetSection({
    title,
    count,
    children
  }) {
    return React.createElement("section", {
      style: {
        marginTop: 18,
        breakInside: "avoid"
      }
    }, React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "baseline",
        borderBottom: "1.5px solid #212121",
        paddingBottom: 3,
        marginBottom: 7
      }
    }, React.createElement("h2", {
      style: {
        margin: 0,
        fontFamily: "var(--font-display)",
        fontSize: "12pt",
        fontWeight: 800,
        letterSpacing: "0.02em",
        textTransform: "uppercase"
      }
    }, title), count != null && React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "8.5pt",
        color: "#5e5e5e"
      }
    }, count)), children);
  }
  const cellTh = {
    textAlign: "left",
    fontFamily: "var(--font-sans)",
    fontSize: "7pt",
    fontWeight: 700,
    letterSpacing: "0.05em",
    textTransform: "uppercase",
    color: "#5e5e5e",
    padding: "3px 6px",
    borderBottom: "1px solid #adadad"
  };
  const cellTd = {
    fontFamily: "var(--font-sans)",
    fontSize: "8.5pt",
    padding: "3px 6px",
    borderBottom: "1px solid #e6e6e6",
    verticalAlign: "top"
  };
  const mono = {
    fontFamily: "var(--font-mono)"
  };
  function SpecSheet({
    spec,
    wo
  }) {
    const money = fmt;
    const d = new Date(spec.meta.generatedAt);
    const dateStr = d.toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric"
    }) + " " + d.toLocaleTimeString("en-US", {
      hour: "2-digit",
      minute: "2-digit"
    });
    const NodeMark = DS.NodeMark;
    const comp = spec.meta.compliance || {};
    const ctrlCell = {
      padding: "3px 8px",
      borderRight: "1px solid #d4d4d4",
      borderBottom: "1px solid #d4d4d4"
    };
    const ctrlK = {
      fontSize: "6pt",
      textTransform: "uppercase",
      letterSpacing: "0.06em",
      color: "#8a8a8a",
      fontWeight: 700
    };
    const ctrlV = {
      fontSize: "8pt",
      fontWeight: 600,
      fontFamily: "var(--font-mono)"
    };
    return React.createElement("div", {
      className: "spec-sheet",
      style: {
        width: "7.5in",
        margin: "0 auto",
        padding: "0",
        color: "#212121",
        fontFamily: "var(--font-sans)",
        background: "#fff",
        lineHeight: 1.4
      }
    },
    // ISO controlled-document header
    React.createElement("div", {
      style: {
        border: "1.5px solid #212121",
        marginBottom: 12
      }
    }, React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "stretch",
        borderBottom: "1.5px solid #212121"
      }
    }, React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 10,
        padding: "9px 12px",
        borderRight: "1.5px solid #212121",
        flex: 1
      }
    }, NodeMark ? React.createElement(NodeMark, {
      size: 38
    }) : null, React.createElement("div", null, React.createElement("div", {
      style: {
        fontFamily: "var(--font-display)",
        fontSize: "15pt",
        fontWeight: 800,
        lineHeight: 1
      }
    }, "SequencerPro"), React.createElement("div", {
      style: {
        fontSize: "7.5pt",
        color: "#5e5e5e",
        marginTop: 1
      }
    }, "Manufacturing Engineering · Process Manager"))), React.createElement("div", {
      style: {
        padding: "7px 12px",
        textAlign: "center",
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        minWidth: "2.5in"
      }
    }, React.createElement("div", {
      style: {
        fontFamily: "var(--font-display)",
        fontSize: "11.5pt",
        fontWeight: 800,
        textTransform: "uppercase",
        letterSpacing: "0.02em",
        lineHeight: 1.1
      }
    }, "Configuration & BoM"), React.createElement("div", {
      style: {
        fontFamily: "var(--font-display)",
        fontSize: "11.5pt",
        fontWeight: 800,
        textTransform: "uppercase",
        letterSpacing: "0.02em",
        lineHeight: 1.1
      }
    }, "Specification Record"))),
    // control grid
    React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "repeat(4, 1fr)",
        borderBottom: "0"
      }
    }, [["Document No.", spec.meta.docNo], ["Form / Template", comp.formNo || "QF-BOM-001"], ["Revision", comp.revision || "A"], ["Standard", comp.standard || "ISO 9001:2015"], ["Work Order", wo], ["Config Hash", spec.meta.configHash], ["Issue Date", dateStr], ["Page", "1 of 1"]].map(([k, v], i) => React.createElement("div", {
      key: i,
      style: {
        ...ctrlCell,
        borderBottom: i < 4 ? "1px solid #d4d4d4" : "0",
        borderRight: i % 4 === 3 ? "0" : "1px solid #d4d4d4"
      }
    }, React.createElement("div", {
      style: ctrlK
    }, k), React.createElement("div", {
      style: ctrlV
    }, v)))), React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        padding: "3px 12px",
        background: "#212121",
        color: "#fff"
      }
    }, React.createElement("span", {
      style: {
        fontSize: "7pt",
        fontWeight: 700,
        letterSpacing: "0.08em",
        textTransform: "uppercase"
      }
    }, comp.classification || "Controlled Document"), React.createElement("span", {
      style: {
        fontSize: "7pt",
        color: "#f1c40f",
        letterSpacing: "0.04em"
      }
    }, "Conforms to " + (comp.clauses || "ISO 9001:2015")))),
    // platform + summary band
    React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        background: "#f6f6f6",
        border: "1px solid #e6e6e6",
        borderRadius: 4,
        padding: "9px 13px",
        marginTop: 0
      }
    }, React.createElement("div", null, React.createElement("div", {
      style: {
        fontFamily: "var(--font-display)",
        fontSize: "12.5pt",
        fontWeight: 800
      }
    }, spec.meta.platform.name), React.createElement("div", {
      style: {
        ...mono,
        fontSize: "8pt",
        color: "#5e5e5e"
      }
    }, spec.meta.platform.code)), React.createElement("div", {
      style: {
        display: "flex",
        gap: 20
      }
    }, [["Parts", spec.bom.partCount], ["Ops", spec.routing.length], ["Per unit", money(spec.price.perUnit)], ["Qty", spec.meta.orderQty], ["Order total", money(spec.price.orderTotal)]].map(([k, v], i) => React.createElement("div", {
      key: i,
      style: {
        textAlign: "right"
      }
    }, React.createElement("div", {
      style: {
        fontFamily: "var(--font-display)",
        fontSize: "12pt",
        fontWeight: 800
      }
    }, v), React.createElement("div", {
      style: {
        fontSize: "6.5pt",
        textTransform: "uppercase",
        letterSpacing: "0.06em",
        color: "#8a8a8a"
      }
    }, k))))),
    // configuration selections
    React.createElement(SheetSection, {
      title: "Configuration"
    }, React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "1fr 1fr",
        gap: "2px 24px"
      }
    }, spec.selections.map((s, i) => React.createElement("div", {
      key: i,
      style: {
        display: "flex",
        justifyContent: "space-between",
        gap: 10,
        padding: "3px 0",
        borderBottom: "1px solid #f0f0f0",
        fontSize: "8.5pt"
      }
    }, React.createElement("span", {
      style: {
        color: "#5e5e5e"
      }
    }, s.group), React.createElement("span", {
      style: {
        fontWeight: 600,
        textAlign: "right",
        flex: 1
      }
    }, s.name, s.unitPrice ? React.createElement("span", {
      style: {
        ...mono,
        color: "#5e5e5e",
        marginLeft: 6,
        fontWeight: 400
      }
    }, "+" + money(s.unitPrice)) : null))))),
    // attributes + calculated, two columns
    React.createElement("div", {
      style: {
        display: "flex",
        gap: 24,
        marginTop: 0
      }
    }, React.createElement("div", {
      style: {
        flex: 1
      }
    }, React.createElement(SheetSection, {
      title: "Attributes"
    }, spec.attributes.map(a => React.createElement("div", {
      key: a.id,
      style: {
        display: "flex",
        justifyContent: "space-between",
        padding: "3px 0",
        borderBottom: "1px solid #f0f0f0",
        fontSize: "8.5pt"
      }
    }, React.createElement("span", {
      style: {
        color: "#5e5e5e"
      }
    }, a.name, a.bounds && a.bounds.clampedBy.length ? React.createElement("span", {
      style: {
        ...mono,
        color: "#2769ab",
        fontSize: "7pt",
        marginLeft: 5
      }
    }, "[" + a.bounds.min + "–" + a.bounds.max + "]") : null), React.createElement("span", {
      style: {
        ...mono,
        fontWeight: 700
      }
    }, a.value + (a.unit ? " " + a.unit : "")))))), React.createElement("div", {
      style: {
        flex: 1
      }
    }, React.createElement(SheetSection, {
      title: "Calculated"
    }, spec.calculated.map(c => React.createElement("div", {
      key: c.id,
      style: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "baseline",
        gap: 8,
        padding: "3px 0",
        borderBottom: "1px solid #f0f0f0",
        fontSize: "8.5pt"
      }
    }, React.createElement("span", null, c.name, React.createElement("code", {
      style: {
        ...mono,
        display: "block",
        fontSize: "6.5pt",
        color: "#8a8a8a"
      }
    }, c.expr)), React.createElement("span", {
      style: {
        ...mono,
        fontWeight: 700,
        color: "#93750a"
      }
    }, c.value)))))),
    // BoM table
    React.createElement(SheetSection, {
      title: "Bill of Materials",
      count: spec.bom.partCount + " parts · " + money(spec.bom.cost)
    }, React.createElement("table", {
      style: {
        width: "100%",
        borderCollapse: "collapse"
      }
    }, React.createElement("thead", null, React.createElement("tr", null, React.createElement("th", {
      style: {
        ...cellTh,
        width: "13%"
      }
    }, "Code"), React.createElement("th", {
      style: cellTh
    }, "Part"), React.createElement("th", {
      style: {
        ...cellTh,
        width: "16%"
      }
    }, "Source"), React.createElement("th", {
      style: {
        ...cellTh,
        width: "12%",
        textAlign: "right"
      }
    }, "Qty"), React.createElement("th", {
      style: {
        ...cellTh,
        width: "11%",
        textAlign: "right"
      }
    }, "Unit"), React.createElement("th", {
      style: {
        ...cellTh,
        width: "12%",
        textAlign: "right"
      }
    }, "Ext"))), React.createElement("tbody", null, spec.bom.rows.map((r, i) => React.createElement("tr", {
      key: i
    }, React.createElement("td", {
      style: {
        ...cellTd,
        ...mono,
        fontSize: "7.5pt",
        color: "#2769ab",
        paddingLeft: r.level === 2 ? 16 : 6
      }
    }, r.code), React.createElement("td", {
      style: {
        ...cellTd,
        color: r.level === 2 ? "#5e5e5e" : "#212121"
      }
    }, r.name), React.createElement("td", {
      style: {
        ...cellTd,
        fontSize: "7.5pt",
        color: "#5e5e5e"
      }
    }, r.source), React.createElement("td", {
      style: {
        ...cellTd,
        ...mono,
        textAlign: "right",
        color: r.qtyFormula ? "#16806b" : "#212121"
      }
    }, "×" + r.qty, r.qtyFormula ? React.createElement("span", {
      style: {
        fontSize: "6.5pt"
      }
    }, " ƒ " + r.qtyFormula) : null), React.createElement("td", {
      style: {
        ...cellTd,
        ...mono,
        textAlign: "right"
      }
    }, money(r.unitPrice)), React.createElement("td", {
      style: {
        ...cellTd,
        ...mono,
        textAlign: "right"
      }
    }, money(r.ext)))))), React.createElement("div", {
      style: {
        fontSize: "7pt",
        color: "#8a8a8a",
        marginTop: 4
      }
    }, "ƒ denotes a formula-driven quantity.")),
    // price rollup (full width, capped measure)
    React.createElement(SheetSection, {
      title: "Price Rollup"
    }, React.createElement("div", {
      style: {
        maxWidth: "4in"
      }
    }, spec.price.lines.map((l, i) => React.createElement("div", {
      key: i,
      style: {
        display: "flex",
        justifyContent: "space-between",
        padding: "3px 0",
        borderBottom: "1px solid #f0f0f0",
        fontSize: "8.5pt"
      }
    }, React.createElement("span", {
      style: {
        color: l.base ? "#212121" : "#5e5e5e",
        fontWeight: l.base ? 600 : 400
      }
    }, l.label, l.priceFormula ? React.createElement("code", {
      style: {
        ...mono,
        display: "block",
        fontSize: "6.5pt",
        color: "#16806b"
      }
    }, "ƒ " + l.priceFormula) : null), React.createElement("span", {
      style: {
        ...mono,
        fontWeight: l.base ? 600 : 400
      }
    }, (l.base ? "" : "+") + money(l.amount)))), React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        paddingTop: 6,
        marginTop: 2,
        borderTop: "1.5px solid #212121",
        fontWeight: 800
      }
    }, React.createElement("span", {
      style: {
        fontSize: "9pt"
      }
    }, "Order total (×" + spec.meta.orderQty + ")"), React.createElement("span", {
      style: {
        fontFamily: "var(--font-display)",
        fontSize: "11pt"
      }
    }, money(spec.price.orderTotal))))),
    // ── Routing & Sequence Clearance Record — one clearance stamp per operation ──
    React.createElement(SheetSection, {
      title: "Routing & Sequence Clearance Record",
      count: spec.routing.length + " operations"
    }, React.createElement("div", {
      style: {
        fontSize: "7.5pt",
        color: "#5e5e5e",
        marginBottom: 7
      }
    }, "Operations must be cleared in sequence. On completion of each operation the operator signs and dates the row and the authorized inspector applies the sequence clearance stamp; the next operation may not begin until the preceding stamp is applied (ISO 9001:2015 §8.5.1 production control, §8.5.2 identification & traceability)."), React.createElement("table", {
      style: {
        width: "100%",
        borderCollapse: "collapse",
        border: "1px solid #adadad"
      }
    }, React.createElement("thead", null, React.createElement("tr", {
      style: {
        background: "#212121"
      }
    }, React.createElement("th", {
      style: {
        ...cellTh,
        color: "#fff",
        borderBottom: "1px solid #212121",
        width: "7%",
        textAlign: "center"
      }
    }, "Seq"), React.createElement("th", {
      style: {
        ...cellTh,
        color: "#fff",
        borderBottom: "1px solid #212121",
        width: "12%"
      }
    }, "Op Code"), React.createElement("th", {
      style: {
        ...cellTh,
        color: "#fff",
        borderBottom: "1px solid #212121"
      }
    }, "Operation"), React.createElement("th", {
      style: {
        ...cellTh,
        color: "#fff",
        borderBottom: "1px solid #212121",
        width: "17%"
      }
    }, "Operator"), React.createElement("th", {
      style: {
        ...cellTh,
        color: "#fff",
        borderBottom: "1px solid #212121",
        width: "11%"
      }
    }, "Date"), React.createElement("th", {
      style: {
        ...cellTh,
        color: "#fff",
        borderBottom: "1px solid #212121",
        width: "16%",
        textAlign: "center"
      }
    }, "Sequence Clearance"))), React.createElement("tbody", null, spec.routing.map(o => React.createElement("tr", {
      key: o.code,
      style: {
        breakInside: "avoid"
      }
    }, React.createElement("td", {
      style: {
        ...cellTd,
        textAlign: "center",
        verticalAlign: "middle"
      }
    }, React.createElement("span", {
      style: {
        width: 15,
        height: 15,
        borderRadius: "50%",
        background: "#212121",
        color: "#fff",
        fontSize: "6.5pt",
        fontWeight: 700,
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center"
      }
    }, o.step)), React.createElement("td", {
      style: {
        ...cellTd,
        ...mono,
        fontSize: "7.5pt",
        verticalAlign: "middle"
      }
    }, o.code), React.createElement("td", {
      style: {
        ...cellTd,
        verticalAlign: "middle"
      }
    }, o.name), React.createElement("td", {
      style: {
        ...cellTd
      }
    }, React.createElement("div", {
      style: {
        height: 22,
        borderBottom: "1px solid #c4c4c4"
      }
    }), React.createElement("div", {
      style: {
        fontSize: "5.5pt",
        color: "#adadad",
        textTransform: "uppercase",
        letterSpacing: "0.05em"
      }
    }, "Sign")), React.createElement("td", {
      style: {
        ...cellTd
      }
    }, React.createElement("div", {
      style: {
        height: 22,
        borderBottom: "1px solid #c4c4c4"
      }
    })), React.createElement("td", {
      style: {
        ...cellTd,
        padding: "3px"
      }
    }, React.createElement("div", {
      style: {
        height: 34,
        border: "1px dashed #adadad",
        borderRadius: 2,
        display: "flex",
        alignItems: "center",
        justifyContent: "center"
      }
    }, React.createElement("span", {
      style: {
        fontSize: "5.5pt",
        textTransform: "uppercase",
        letterSpacing: "0.07em",
        color: "#c4c4c4",
        fontWeight: 700
      }
    }, "Stamp " + String(o.step).padStart(2, "0")))))))), React.createElement("div", {
      style: {
        fontSize: "6.5pt",
        color: "#8a8a8a",
        marginTop: 4
      }
    }, "A skipped or out-of-sequence stamp voids the record — raise a nonconformance report (§8.7) and re-inspect before proceeding.")),
    // rules
    React.createElement(SheetSection, {
      title: "Applied Rules",
      count: spec.rules.length
    }, React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "1fr 1fr",
        gap: "2px 24px"
      }
    }, spec.rules.map((r, i) => {
      const detail = r.expr || (r.when || []).join(",") + " \u2192 " + (r.then || []).join(",");
      const tone = r.type === "excludes" ? "#93380c" : r.type === "formula" ? "#1c4d7e" : "#115f50";
      return React.createElement("div", {
        key: i,
        style: {
          padding: "3px 0",
          borderBottom: "1px solid #f0f0f0"
        }
      }, React.createElement("div", {
        style: {
          fontSize: "8pt",
          display: "flex",
          gap: 5
        }
      }, React.createElement("span", {
        style: {
          ...mono,
          fontWeight: 700,
          color: "#8a8a8a",
          fontSize: "7pt"
        }
      }, r.id), React.createElement("span", {
        style: {
          fontWeight: 600,
          textTransform: "uppercase",
          fontSize: "6.5pt",
          letterSpacing: "0.04em",
          color: tone
        }
      }, r.type), React.createElement("span", {
        style: {
          color: "#5e5e5e",
          flex: 1
        }
      }, r.msg)), React.createElement("code", {
        style: {
          ...mono,
          fontSize: "6.5pt",
          color: "#8a8a8a"
        }
      }, detail));
    }))),
    // ── ISO authorization, approval & inspection stamps ──
    React.createElement(SheetSection, {
      title: "Authorization & Approval"
    }, React.createElement("div", {
      style: {
        fontSize: "7.5pt",
        color: "#5e5e5e",
        marginBottom: 8
      }
    }, "By signing and stamping below, each authority confirms this configuration record has been verified per ", comp.standard || "ISO 9001:2015", ". Document is not valid for production release until all required signatures and the Quality stamp are applied."), React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "repeat(2, 1fr)",
        gap: 10
      }
    }, (comp.approvals || []).map((a, i) => React.createElement("div", {
      key: i,
      style: {
        border: "1px solid #adadad",
        borderRadius: 3,
        display: "flex",
        minHeight: 78,
        breakInside: "avoid"
      }
    },
    // signature column
    React.createElement("div", {
      style: {
        flex: 1,
        padding: "6px 9px",
        display: "flex",
        flexDirection: "column"
      }
    }, React.createElement("div", {
      style: {
        fontSize: "6.5pt",
        fontWeight: 700,
        textTransform: "uppercase",
        letterSpacing: "0.06em",
        color: "#8a8a8a"
      }
    }, a.role), React.createElement("div", {
      style: {
        flex: 1
      }
    }), React.createElement("div", {
      style: {
        borderBottom: "1px solid #212121",
        marginBottom: 2,
        minHeight: 16
      }
    }), React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        fontSize: "6.5pt",
        color: "#8a8a8a"
      }
    }, React.createElement("span", null, "Name / Signature"), React.createElement("span", null, a.title)), React.createElement("div", {
      style: {
        display: "flex",
        gap: 6,
        marginTop: 5
      }
    }, React.createElement("div", {
      style: {
        flex: 1,
        borderBottom: "1px solid #adadad",
        fontSize: "6.5pt",
        color: "#8a8a8a",
        paddingBottom: 1
      }
    }, "Date"))),
    // stamp box
    React.createElement("div", {
      style: {
        width: 78,
        flex: "none",
        borderLeft: "1px dashed #adadad",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        textAlign: "center"
      }
    }, React.createElement("span", {
      style: {
        fontSize: "6pt",
        textTransform: "uppercase",
        letterSpacing: "0.08em",
        color: "#c4c4c4",
        fontWeight: 700,
        lineHeight: 1.3
      }
    }, "Stamp /", React.createElement("br"), "Seal")))))),
    // Quality release — prominent controlled stamp
    React.createElement("div", {
      style: {
        display: "flex",
        gap: 12,
        marginTop: 14,
        breakInside: "avoid"
      }
    }, React.createElement("div", {
      style: {
        flex: 1,
        border: "1.5px solid #212121",
        borderRadius: 3,
        padding: "8px 11px"
      }
    }, React.createElement("div", {
      style: {
        fontSize: "7pt",
        fontWeight: 700,
        textTransform: "uppercase",
        letterSpacing: "0.06em",
        color: "#5e5e5e",
        marginBottom: 4
      }
    }, "Quality Disposition"), React.createElement("div", {
      style: {
        display: "flex",
        gap: 14,
        marginBottom: 8
      }
    }, ["Accepted", "Accepted w/ deviation", "Rejected"].map((opt, i) => React.createElement("span", {
      key: i,
      style: {
        display: "inline-flex",
        alignItems: "center",
        gap: 5,
        fontSize: "8pt"
      }
    }, React.createElement("span", {
      style: {
        width: 11,
        height: 11,
        border: "1.5px solid #212121",
        borderRadius: 2,
        flex: "none"
      }
    }), opt))), React.createElement("div", {
      style: {
        display: "flex",
        gap: 12
      }
    }, React.createElement("div", {
      style: {
        flex: 2,
        borderBottom: "1px solid #212121",
        fontSize: "6.5pt",
        color: "#8a8a8a"
      }
    }, "QA Inspector"), React.createElement("div", {
      style: {
        flex: 1,
        borderBottom: "1px solid #212121",
        fontSize: "6.5pt",
        color: "#8a8a8a"
      }
    }, "Insp. ID"), React.createElement("div", {
      style: {
        flex: 1,
        borderBottom: "1px solid #212121",
        fontSize: "6.5pt",
        color: "#8a8a8a"
      }
    }, "Date"))), React.createElement("div", {
      style: {
        width: 118,
        flex: "none",
        border: "1.5px solid #212121",
        borderRadius: "50%",
        aspectRatio: "1 / 1",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        textAlign: "center"
      }
    }, React.createElement("div", {
      style: {
        fontSize: "6.5pt",
        fontWeight: 700,
        letterSpacing: "0.08em",
        color: "#c4c4c4",
        textTransform: "uppercase"
      }
    }, "Quality"), React.createElement("div", {
      style: {
        fontSize: "6.5pt",
        fontWeight: 700,
        letterSpacing: "0.08em",
        color: "#c4c4c4",
        textTransform: "uppercase"
      }
    }, "Approved"), React.createElement("div", {
      style: {
        width: 70,
        borderTop: "1px solid #d4d4d4",
        margin: "6px 0",
        paddingTop: 4,
        fontSize: "5.5pt",
        color: "#c4c4c4"
      }
    }, "Controlled stamp here"))),
    // Revision history (ISO 10007 configuration management record)
    React.createElement(SheetSection, {
      title: "Revision History"
    }, React.createElement("table", {
      style: {
        width: "100%",
        borderCollapse: "collapse"
      }
    }, React.createElement("thead", null, React.createElement("tr", null, React.createElement("th", {
      style: {
        ...cellTh,
        width: "9%"
      }
    }, "Rev"), React.createElement("th", {
      style: {
        ...cellTh,
        width: "16%"
      }
    }, "Date"), React.createElement("th", {
      style: cellTh
    }, "Description of change"), React.createElement("th", {
      style: {
        ...cellTh,
        width: "26%"
      }
    }, "Author / System"))), React.createElement("tbody", null, (comp.revisionHistory || []).map((r, i) => React.createElement("tr", {
      key: i
    }, React.createElement("td", {
      style: {
        ...cellTd,
        ...mono,
        fontWeight: 700
      }
    }, r.rev), React.createElement("td", {
      style: {
        ...cellTd,
        ...mono,
        fontSize: "7.5pt"
      }
    }, r.date), React.createElement("td", {
      style: cellTd
    }, r.description), React.createElement("td", {
      style: {
        ...cellTd,
        fontSize: "7.5pt",
        color: "#5e5e5e"
      }
    }, r.author))))), React.createElement("div", {
      style: {
        display: "flex",
        gap: 24,
        marginTop: 6,
        fontSize: "7pt",
        color: "#5e5e5e"
      }
    }, React.createElement("span", null, React.createElement("strong", null, "Document owner: "), comp.owner || "Manufacturing Engineering"), React.createElement("span", null, React.createElement("strong", null, "Retention: "), comp.retention || "Retain 7 years"))),
    // ── ISO controlled-document footer ──
    React.createElement("div", {
      style: {
        marginTop: 20,
        borderTop: "1.5px solid #212121",
        paddingTop: 6,
        display: "flex",
        justifyContent: "space-between",
        alignItems: "flex-start",
        fontSize: "6.5pt",
        color: "#8a8a8a"
      }
    }, React.createElement("div", {
      style: {
        maxWidth: "3.6in",
        lineHeight: 1.4
      }
    }, React.createElement("strong", {
      style: {
        color: "#5e5e5e"
      }
    }, "UNCONTROLLED WHEN PRINTED. "), "The controlled master of this record is held in SequencerPro Process Manager. Verify revision against the system register before use. © SequencerPro — confidential."), React.createElement("div", {
      style: {
        textAlign: "right",
        ...mono,
        lineHeight: 1.5
      }
    }, React.createElement("div", null, spec.meta.docNo), React.createElement("div", null, (comp.formNo || "QF-BOM-001") + " Rev " + (comp.revision || "A")), React.createElement("div", null, "Page 1 of 1"))));
  }
  Object.assign(window, {
    PriceRollup,
    BomTree,
    Routing,
    ViolationsList,
    WorkOrderModal,
    SpecSheet
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/bom.js", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/configure.js
try { (() => {
/* ============================================================
   BoM Configurator — Configure (consuming) experience
   window.ConfigureView
   ============================================================ */
(function () {
  const {
    useState
  } = React;
  const DS = window.SequencerProDesignSystem_5eb90b;
  const {
    Button,
    StatusBadge
  } = DS;
  const C = window.CFG,
    CAR = window.CAR,
    fmt = C.fmt;
  function rulesFor(optId, enabled) {
    return CAR.rules.filter(r => (!enabled || enabled.includes(r.id)) && ((r.when || []).includes(optId) || (r.then || []).includes(optId)));
  }
  function OptionCard({
    group,
    o,
    selected,
    disabled,
    reason,
    onChoose,
    enabled
  }) {
    const deps = rulesFor(o.id, enabled);
    const lead = o.swatch ? /*#__PURE__*/React.createElement("span", {
      style: {
        width: 28,
        height: 28,
        borderRadius: "50%",
        background: o.swatch,
        border: "1px solid var(--border-strong)",
        flex: "none"
      }
    }) : /*#__PURE__*/React.createElement("span", {
      style: {
        width: 28,
        height: 28,
        borderRadius: 6,
        background: selected ? "var(--gold-100)" : "var(--bg-sunken)",
        color: selected ? "var(--gold-700)" : "var(--text-muted)",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        flex: "none"
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: `bi ${group.icon}`
    }));
    return /*#__PURE__*/React.createElement("button", {
      onClick: () => !disabled && onChoose(o.id),
      disabled: disabled,
      title: disabled ? reason : undefined,
      style: {
        display: "flex",
        alignItems: "center",
        gap: 12,
        width: "100%",
        textAlign: "left",
        padding: "11px 13px",
        borderRadius: "var(--radius-md)",
        cursor: disabled ? "not-allowed" : "pointer",
        background: selected ? "var(--gold-50)" : "var(--bg-surface)",
        border: selected ? "2px solid var(--gold)" : "1px solid var(--border-default)",
        margin: selected ? 0 : 1,
        opacity: disabled ? 0.55 : 1,
        transition: "var(--transition, all .15s)",
        position: "relative",
        fontFamily: "var(--font-sans)"
      }
    }, lead, /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1,
        minWidth: 0
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 7
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontSize: "var(--text-sm)",
        fontWeight: 600,
        color: "var(--text-primary)"
      }
    }, o.name), deps.length > 0 && /*#__PURE__*/React.createElement("i", {
      className: "bi bi-link-45deg",
      title: deps.map(d => d.msg).join("\n"),
      style: {
        fontSize: 13,
        color: "var(--text-muted)"
      }
    })), (o.sub || disabled) && /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        color: disabled ? "var(--status-fail)" : "var(--text-muted)",
        marginTop: 1,
        lineHeight: 1.35
      }
    }, disabled ? /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-lock-fill",
      style: {
        marginRight: 3
      }
    }), reason) : o.sub)), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        alignItems: "flex-end",
        gap: 4,
        flex: "none"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-xs)",
        fontWeight: 600,
        color: o.price ? "var(--text-primary)" : "var(--text-muted)"
      }
    }, o.price ? "+" + fmt(o.price) : "Incl."), /*#__PURE__*/React.createElement("span", {
      style: {
        width: 18,
        height: 18,
        borderRadius: group.multi ? 4 : "50%",
        flex: "none",
        border: selected ? "none" : "2px solid var(--border-strong)",
        background: selected ? "var(--gold)" : "transparent",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center"
      }
    }, selected && /*#__PURE__*/React.createElement("i", {
      className: "bi bi-check-lg",
      style: {
        fontSize: 12,
        color: "var(--slate)"
      }
    }))));
  }
  function AttributesPanel({
    sel,
    onAttr,
    onFix,
    violations,
    enabled
  }) {
    const attrs = C.attributes,
      calc = C.computeCalc(sel);
    const val = id => sel.attrs && sel.attrs[id] != null ? sel.attrs[id] : C.attrDef(id).default;
    const ruleById = id => CAR.rules.find(r => r.id === id);
    const attrViolations = aid => (violations || []).filter(v => {
      const r = ruleById(v.id);
      return r && (r.refs || []).includes(aid);
    });
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: 14
      }
    }, attrs.map(a => {
      const vio = attrViolations(a.id);
      const bad = vio.length > 0;
      return /*#__PURE__*/React.createElement("div", {
        key: a.id,
        style: {
          background: "var(--bg-surface)",
          border: `1px solid ${bad ? "var(--orange)" : "var(--border-default)"}`,
          borderRadius: "var(--radius-md)",
          padding: "13px 15px"
        }
      }, /*#__PURE__*/React.createElement("div", {
        style: {
          display: "flex",
          justifyContent: "space-between",
          alignItems: "baseline"
        }
      }, /*#__PURE__*/React.createElement("span", {
        style: {
          fontSize: "var(--text-sm)",
          fontWeight: 600
        }
      }, a.name), /*#__PURE__*/React.createElement("span", {
        style: {
          fontFamily: "var(--font-mono)",
          fontSize: "var(--text-md)",
          fontWeight: 700,
          color: bad ? "var(--orange-700)" : "var(--text-primary)"
        }
      }, val(a.id), " ", /*#__PURE__*/React.createElement("span", {
        style: {
          color: "var(--text-muted)",
          fontWeight: 400,
          fontSize: "var(--text-2xs)"
        }
      }, a.unit))), /*#__PURE__*/React.createElement("div", {
        style: {
          fontSize: "var(--text-2xs)",
          color: "var(--text-muted)",
          margin: "2px 0 11px"
        }
      }, a.hint), a.kind === "choice" ? /*#__PURE__*/React.createElement("div", {
        style: {
          display: "flex",
          gap: 6
        }
      }, a.options.map(o => {
        const on = val(a.id) === o;
        return /*#__PURE__*/React.createElement("button", {
          key: o,
          onClick: () => onAttr(a.id, o),
          style: {
            flex: 1,
            padding: "8px 0",
            borderRadius: "var(--radius-sm)",
            cursor: "pointer",
            border: on ? "2px solid var(--gold)" : "1px solid var(--border-strong)",
            background: on ? "var(--gold-50)" : "var(--bg-surface)",
            fontFamily: "var(--font-sans)",
            fontWeight: 600,
            fontSize: "var(--text-sm)"
          }
        }, o);
      })) : (() => {
        const b = a.kind === "range" ? C.attrBounds(a.id, enabled) : {
          clamped: false
        };
        const lo = b.clamped && isFinite(b.min) ? Math.max(a.min, b.min) : a.min;
        const hi = b.clamped && isFinite(b.max) ? Math.min(a.max, b.max) : a.max;
        const clampSet = raw => onAttr(a.id, Math.min(hi, Math.max(lo, raw)));
        return /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("input", {
          type: "range",
          min: lo,
          max: hi,
          step: a.step,
          value: Math.min(hi, Math.max(lo, val(a.id))),
          onChange: e => clampSet(Number(e.target.value)),
          style: {
            width: "100%",
            accentColor: bad ? "var(--orange)" : b.clamped ? "var(--blue)" : "var(--gold)"
          }
        }), /*#__PURE__*/React.createElement("div", {
          style: {
            display: "flex",
            justifyContent: "space-between",
            fontSize: "var(--text-2xs)",
            color: "var(--text-muted)",
            fontFamily: "var(--font-mono)"
          }
        }, /*#__PURE__*/React.createElement("span", null, lo, a.unit ? " " + a.unit : ""), /*#__PURE__*/React.createElement("span", null, hi, a.unit ? " " + a.unit : "")), b.clamped && /*#__PURE__*/React.createElement("div", {
          style: {
            display: "flex",
            alignItems: "center",
            gap: 5,
            marginTop: 7,
            fontSize: "var(--text-2xs)",
            color: "var(--blue-700)"
          }
        }, /*#__PURE__*/React.createElement("i", {
          className: "bi bi-lock-fill"
        }), /*#__PURE__*/React.createElement("span", null, "Range limited to ", isFinite(b.min) ? lo : "–∞", "\u2013", isFinite(b.max) ? hi : "∞", a.unit ? " " + a.unit : "", " by rule ", b.fromRules.join(", "))));
      })(), bad && vio.map((v, i) => /*#__PURE__*/React.createElement("div", {
        key: i,
        style: {
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          gap: 8,
          marginTop: 9,
          padding: "6px 9px",
          background: "var(--orange-50)",
          border: "1px solid var(--orange-200)",
          borderRadius: "var(--radius-sm)"
        }
      }, /*#__PURE__*/React.createElement("span", {
        style: {
          fontSize: "var(--text-2xs)",
          color: "var(--orange-700)",
          fontWeight: 600,
          display: "flex",
          alignItems: "center",
          gap: 5
        }
      }, /*#__PURE__*/React.createElement("i", {
        className: "bi bi-exclamation-triangle-fill"
      }), v.msg), v.fix && /*#__PURE__*/React.createElement("button", {
        onClick: () => onFix(v),
        style: {
          flex: "none",
          border: "none",
          background: "var(--orange)",
          color: "#fff",
          borderRadius: "var(--radius-xs)",
          fontSize: "var(--text-2xs)",
          fontWeight: 700,
          padding: "3px 8px",
          cursor: "pointer",
          fontFamily: "var(--font-sans)"
        }
      }, v.fixLabel))));
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        background: "var(--bg-sunken)",
        borderRadius: "var(--radius-md)",
        padding: "13px 15px",
        border: "1px dashed var(--border-strong)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        letterSpacing: "var(--ls-caps)",
        textTransform: "uppercase",
        color: "var(--text-muted)",
        marginBottom: 10,
        display: "flex",
        alignItems: "center",
        gap: 6
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-calculator"
    }), "Calculated attributes"), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: 9
      }
    }, calc.map(c => /*#__PURE__*/React.createElement("div", {
      key: c.id,
      style: {
        display: "flex",
        alignItems: "center",
        gap: 10
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: 120,
        fontSize: "var(--text-sm)",
        fontWeight: 600,
        flex: "none"
      }
    }, c.name), /*#__PURE__*/React.createElement("code", {
      style: {
        flex: 1,
        minWidth: 0,
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-2xs)",
        color: "var(--text-secondary)",
        background: "var(--bg-surface)",
        border: "1px solid var(--border-default)",
        borderRadius: "var(--radius-xs)",
        padding: "3px 7px",
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis"
      }
    }, c.expr), /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "var(--font-display)",
        fontSize: "var(--text-lg)",
        fontWeight: 800,
        color: "var(--gold-700)",
        width: 34,
        textAlign: "right",
        flex: "none"
      }
    }, c.value))))));
  }
  function ConfigureView({
    sel,
    disabled,
    violations,
    notes,
    onChoose,
    onFix,
    onAttr,
    onCreateWO,
    enabled
  }) {
    const groups = C.applicableGroups(sel);
    const steps = [...groups, {
      id: "__dims",
      name: "Dimensions & Quantity",
      icon: "bi-rulers",
      dims: true
    }];
    const [stepId, setStepId] = useState(groups[0].id);
    const [tab, setTab] = useState("bom");
    let idx = steps.findIndex(s => s.id === stepId);
    if (idx < 0) idx = 0;
    const group = steps[idx];
    const st = C.status(sel, violations);
    const colorOpt = C.opt(sel.color),
      armOpt = C.opt(sel.arm),
      eeOpt = C.opt(sel.effector),
      ctlOpt = C.opt(sel.controller);
    const reachVal = sel.attrs && sel.attrs.reach != null ? sel.attrs.reach : 1300;
    const railVal = sel.attrs && sel.attrs.railLen != null ? sel.attrs.railLen : 0;
    const hasRail = C.isSel(sel, "pkg-track") && railVal > 0;
    const stepDone = g => g.dims ? true : g.multi ? (sel[g.id] || []).length >= 0 : !!sel[g.id];
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "210px 1fr 340px",
        height: "100%",
        overflow: "hidden"
      }
    }, /*#__PURE__*/React.createElement("nav", {
      style: {
        borderRight: "var(--border)",
        background: "var(--bg-surface)",
        overflowY: "auto",
        padding: "14px 10px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        letterSpacing: "var(--ls-caps)",
        textTransform: "uppercase",
        color: "var(--text-muted)",
        padding: "0 8px 8px"
      }
    }, "Configuration"), steps.map((g, i) => {
      const on = g.id === stepId;
      return /*#__PURE__*/React.createElement("button", {
        key: g.id,
        onClick: () => setStepId(g.id),
        style: {
          display: "flex",
          alignItems: "center",
          gap: 10,
          width: "100%",
          padding: "9px 10px",
          border: "none",
          borderRadius: "var(--radius)",
          cursor: "pointer",
          background: on ? "var(--bg-sunken)" : "transparent",
          fontFamily: "var(--font-sans)",
          fontSize: "var(--text-sm)",
          fontWeight: on ? 600 : 500,
          color: on ? "var(--text-primary)" : "var(--text-secondary)",
          textAlign: "left",
          marginBottom: 2
        }
      }, /*#__PURE__*/React.createElement("span", {
        style: {
          width: 22,
          height: 22,
          borderRadius: "50%",
          flex: "none",
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          background: stepDone(g) ? "var(--teal)" : "var(--grey-200)",
          color: stepDone(g) ? "#fff" : "var(--text-muted)",
          fontSize: 11,
          fontWeight: 700
        }
      }, g.dims ? /*#__PURE__*/React.createElement("i", {
        className: "bi bi-rulers"
      }) : stepDone(g) ? /*#__PURE__*/React.createElement("i", {
        className: "bi bi-check-lg"
      }) : i + 1), /*#__PURE__*/React.createElement("span", {
        style: {
          flex: 1,
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis"
        }
      }, g.name), !g.dims && !g.multi && /*#__PURE__*/React.createElement("span", {
        style: {
          fontSize: "var(--text-2xs)",
          color: "var(--text-muted)",
          fontFamily: "var(--font-mono)"
        }
      }, C.opt(sel[g.id]) ? "" : "—"));
    })), /*#__PURE__*/React.createElement("div", {
      style: {
        overflowY: "auto",
        background: "var(--bg-app)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        background: "linear-gradient(180deg, var(--bg-surface), var(--bg-app))",
        borderBottom: "var(--border)",
        padding: "18px 26px 6px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        maxWidth: 520,
        margin: "0 auto"
      }
    }, /*#__PURE__*/React.createElement(window.ProductPreview, {
      sel: sel,
      schematic: {
        color: colorOpt ? colorOpt.swatch : "#e95b15",
        reach: reachVal,
        effector: eeOpt ? eeOpt.ee : "none",
        badge: armOpt ? armOpt.badge : "10 KG",
        hasRail: hasRail
      }
    })), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "center",
        gap: 7,
        flexWrap: "wrap",
        paddingBottom: 12
      }
    }, [armOpt, ctlOpt, eeOpt, colorOpt].filter(Boolean).map((o, i) => /*#__PURE__*/React.createElement("span", {
      key: i,
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 600,
        color: "var(--text-secondary)",
        background: "var(--bg-surface)",
        border: "var(--border)",
        borderRadius: "var(--radius-pill)",
        padding: "3px 10px"
      }
    }, o.name)))), /*#__PURE__*/React.createElement("div", {
      style: {
        padding: "20px 26px",
        maxWidth: 660,
        margin: "0 auto"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: 4
      }
    }, /*#__PURE__*/React.createElement("h2", {
      style: {
        fontSize: "var(--text-xl)",
        fontWeight: 700,
        margin: 0,
        display: "flex",
        alignItems: "center",
        gap: 9
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: `bi ${group.icon}`,
      style: {
        color: "var(--gold-600)"
      }
    }), group.name), /*#__PURE__*/React.createElement("span", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)",
        textTransform: "uppercase",
        letterSpacing: "0.06em"
      }
    }, group.dims ? "Computed live" : group.multi ? "Select any" : "Select one")), /*#__PURE__*/React.createElement("p", {
      style: {
        fontSize: "var(--text-sm)",
        color: "var(--text-secondary)",
        margin: "0 0 16px"
      }
    }, group.dims ? "Numeric attributes drive calculated values, BoM quantities and per-unit pricing by formula — watch the BoM update as you change them." : group.hint), group.dims ? /*#__PURE__*/React.createElement(AttributesPanel, {
      sel: sel,
      onAttr: onAttr,
      onFix: onFix,
      violations: violations,
      enabled: enabled
    }) : /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: 8
      }
    }, group.options.map(o => /*#__PURE__*/React.createElement(OptionCard, {
      key: o.id,
      group: group,
      o: o,
      selected: C.isSel(sel, o.id),
      disabled: !!disabled[o.id] && !C.isSel(sel, o.id),
      reason: disabled[o.id],
      onChoose: onChoose,
      enabled: enabled
    }))), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        justifyContent: "space-between",
        marginTop: 20
      }
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      disabled: idx === 0,
      onClick: () => setStepId(steps[Math.max(0, idx - 1)].id),
      iconLeft: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-arrow-left"
      })
    }, "Back"), idx < steps.length - 1 ? /*#__PURE__*/React.createElement(Button, {
      onClick: () => setStepId(steps[idx + 1].id),
      iconRight: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-arrow-right"
      })
    }, "Next: ", steps[idx + 1].name) : /*#__PURE__*/React.createElement(Button, {
      variant: "accent",
      onClick: onCreateWO,
      disabled: !st.complete,
      iconRight: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-box-arrow-up-right"
      })
    }, "Release work order")))), /*#__PURE__*/React.createElement("aside", {
      style: {
        borderLeft: "var(--border)",
        background: "var(--bg-surface)",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        padding: "16px 18px 12px",
        borderBottom: "var(--border)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        letterSpacing: "var(--ls-caps)",
        textTransform: "uppercase",
        color: "var(--text-muted)"
      }
    }, "Total MSRP"), /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "var(--font-display)",
        fontWeight: 800,
        fontSize: "var(--text-3xl)",
        color: "var(--text-primary)",
        lineHeight: 1.1
      }
    }, fmt(C.priceRollup(sel).total)), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 8,
        marginTop: 6
      }
    }, st.complete ? /*#__PURE__*/React.createElement(StatusBadge, {
      tone: "pass"
    }, "Buildable") : violations.length ? /*#__PURE__*/React.createElement(StatusBadge, {
      tone: "fail"
    }, violations.length, " conflict", violations.length > 1 ? "s" : "") : /*#__PURE__*/React.createElement(StatusBadge, {
      tone: "active"
    }, "In progress"), /*#__PURE__*/React.createElement("span", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)"
      }
    }, st.done, "/", st.total, " required set"))), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 2,
        padding: "8px 10px 0"
      }
    }, [["bom", "BoM", "bi-diagram-3"], ["route", "Routing", "bi-signpost-split"], ["issues", "Issues", "bi-exclamation-triangle"]].map(([id, label, ic]) => /*#__PURE__*/React.createElement("button", {
      key: id,
      onClick: () => setTab(id),
      style: {
        flex: 1,
        padding: "7px 4px",
        border: "none",
        borderRadius: "var(--radius) var(--radius) 0 0",
        cursor: "pointer",
        background: tab === id ? "var(--bg-sunken)" : "transparent",
        fontFamily: "var(--font-sans)",
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        textTransform: "uppercase",
        letterSpacing: "0.05em",
        color: tab === id ? "var(--text-primary)" : "var(--text-muted)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        gap: 5
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: `bi ${ic}`
    }), label, id === "issues" && violations.length > 0 && /*#__PURE__*/React.createElement("span", {
      style: {
        background: "var(--status-fail)",
        color: "#fff",
        borderRadius: 8,
        fontSize: 9,
        padding: "0 5px",
        marginLeft: 2
      }
    }, violations.length)))), /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1,
        overflowY: "auto",
        padding: "14px 16px",
        background: "var(--bg-sunken)"
      }
    }, tab === "bom" && /*#__PURE__*/React.createElement(window.BomTree, {
      sel: sel
    }), tab === "route" && /*#__PURE__*/React.createElement(window.Routing, {
      sel: sel
    }), tab === "issues" && /*#__PURE__*/React.createElement(window.ViolationsList, {
      violations: violations,
      onFix: onFix
    }), notes && notes.length > 0 && tab !== "issues" && /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: 12,
        padding: "8px 10px",
        background: "var(--blue-50)",
        border: "1px solid var(--blue-100)",
        borderRadius: "var(--radius-sm)",
        fontSize: "var(--text-2xs)",
        color: "var(--blue-700)"
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-info-circle",
      style: {
        marginRight: 4
      }
    }), notes[notes.length - 1])), /*#__PURE__*/React.createElement("div", {
      style: {
        padding: 14,
        borderTop: "var(--border)"
      }
    }, /*#__PURE__*/React.createElement(Button, {
      block: true,
      variant: "accent",
      disabled: !st.complete,
      onClick: onCreateWO,
      iconLeft: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-box-arrow-up-right"
      })
    }, "Release work order"))));
  }
  window.ConfigureView = ConfigureView;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/configure.js", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/data.js
try { (() => {
/* ============================================================
   BoM Configurator — Robot platform data
   "Sequencer RX-6" — a widely-configurable 6-axis industrial
   robot (generic; not modeled on any vendor's product).
   Option groups → options; each option carries the BoM assemblies
   it adds, the routing operations it triggers, and a price delta.
   Rules are declarative so the same list drives the live engine,
   the inline disabling, the validation panel AND the author canvas.

   window.CAR — legacy global name kept so the engine/views don't
   need renaming; the content is the robot platform.
   ============================================================ */
window.CAR = {
  platform: {
    code: "SEQ-RX6",
    name: "Sequencer RX-6",
    tagline: "Configurable 6-Axis Industrial Robot",
    basePrice: 28000
  },
  // ISO 9001:2015 / ISO 10007 controlled-document metadata for the spec sheet.
  compliance: {
    formNo: "QF-BOM-001",
    revision: "A",
    standard: "ISO 9001:2015",
    clauses: "§8.5.1 Production control · §8.5.2 Identification & traceability · ISO 10007 Configuration management",
    classification: "Controlled — Confidential",
    owner: "Manufacturing Engineering",
    retention: "Retain 7 years",
    approvals: [{
      role: "Prepared by",
      name: "",
      title: "Configuration Engineer"
    }, {
      role: "Reviewed by",
      name: "",
      title: "Manufacturing Engineer"
    }, {
      role: "Approved by",
      name: "",
      title: "Engineering Manager"
    }, {
      role: "Quality release",
      name: "",
      title: "QA Inspector"
    }],
    revisionHistory: [{
      rev: "A",
      date: "2026-07-01",
      description: "Initial release from configurator",
      author: "Sequencer Process Manager"
    }]
  },
  // Base assemblies every build includes (multi-level BoM root).
  baseBom: [{
    system: "Structure",
    code: "BASE-001",
    name: "Base & swing assembly",
    qty: 1,
    price: 4600,
    children: [{
      code: "PED-01",
      name: "Pedestal casting",
      qty: 1,
      price: 1700
    }, {
      code: "SWG-01",
      name: "Swing bearing (axis 1)",
      qty: 1,
      price: 1400
    }]
  }, {
    system: "Structure",
    code: "LINK-001",
    name: "Arm linkage set",
    qty: 1,
    price: 3900,
    children: [{
      code: "ARM-LWR",
      name: "Lower arm casting",
      qty: 1,
      price: 1500
    }, {
      code: "ARM-UPR",
      name: "Upper arm casting",
      qty: 1,
      price: 1300
    }, {
      code: "WRST-HSG",
      name: "Wrist housing",
      qty: 1,
      price: 1100
    }]
  }, {
    system: "Motion",
    code: "DRV-001",
    name: "Axis drive package",
    qty: 1,
    price: 0,
    children: [{
      code: "SRV-AX",
      name: "AC servo motor",
      qtyExpr: "axisMotors",
      price: 850
    }, {
      code: "RED-AX",
      name: "Harmonic reducer",
      qtyExpr: "axisMotors",
      price: 1150
    }]
  }, {
    system: "Electrical",
    code: "ELEC-001",
    name: "Electrical base kit",
    qty: 1,
    price: 1900,
    children: [{
      code: "PND-01",
      name: "Teach pendant",
      qty: 1,
      price: 900
    }, {
      code: "SFC-01",
      name: "Safety I/O module",
      qty: 1,
      price: 600
    }, {
      code: "HARN-01",
      name: "Internal harness set",
      qty: 1,
      price: 400
    }]
  }],
  // Base routing operations (always present). seq = order weight.
  baseOps: [{
    code: "MACH-01",
    name: "Casting Machining",
    seq: 10
  }, {
    code: "PAINT-01",
    name: "Prime & Paint",
    seq: 20
  }, {
    code: "ARM-01",
    name: "Arm & Joint Assembly",
    seq: 30
  }, {
    code: "WIRE-01",
    name: "Harness Routing",
    seq: 70
  }, {
    code: "CAL-01",
    name: "Axis Calibration",
    seq: 90
  }, {
    code: "QA-01",
    name: "End-of-Line Test",
    seq: 95
  }],
  // Numeric ATTRIBUTES that feed formulas (D365-style calculated config).
  attributes: [{
    id: "reach",
    name: "Working reach",
    unit: "mm",
    kind: "range",
    min: 600,
    max: 2200,
    step: 100,
    default: 1300,
    hint: "Arm reach at full extension. Drives dress-pack segments; beyond 1400 mm requires the 20 kg arm."
  }, {
    id: "railLen",
    name: "7th-axis rail length",
    unit: "mm",
    kind: "range",
    min: 0,
    max: 6000,
    step: 500,
    default: 0,
    hint: "0 = no track. Rail segments are computed as ceil(length ÷ 1000); a fitted track adds a 7th servo axis."
  }, {
    id: "orderQty",
    name: "Order quantity",
    unit: "robots",
    kind: "range",
    min: 1,
    max: 25,
    step: 1,
    default: 1,
    hint: "Robots built by this work order. Multiplies the order-level rollup."
  }],
  // CALCULATED attributes — read-only, value derived from a formula over attributes
  // and selections (has('id') tests a selected option). Resolved top-to-bottom.
  calc: [{
    id: "axisMotors",
    name: "Servo axes",
    expr: "6 + (railLen > 0 ? 1 : 0)",
    unit: "",
    hint: "Six arm axes, plus the rail axis when a track is fitted."
  }, {
    id: "railSegs",
    name: "Rail segments",
    expr: "railLen > 0 ? ceil(railLen / 1000) : 0",
    unit: "",
    hint: "ceil(rail length ÷ 1000 mm)."
  }, {
    id: "dressSegs",
    name: "Dress-pack segments",
    expr: "ceil(reach / 700)",
    unit: "",
    hint: "Cable dress segments scale with reach: ceil(reach ÷ 700 mm)."
  }],
  groups: [{
    id: "arm",
    name: "Arm Class",
    icon: "bi-robot",
    multi: false,
    required: true,
    hint: "Payload capacity class — castings, wrist and counterbalance.",
    options: [{
      id: "arm-5",
      name: "RX-6/5 · 5 kg",
      price: 0,
      sub: "Light handling & assembly",
      badge: "5 KG",
      bom: [{
        system: "Structure",
        code: "ARM-C5",
        name: "5 kg arm kit",
        qty: 1,
        price: 3200,
        children: [{
          code: "WRST-5",
          name: "Wrist unit (5 kg)",
          qty: 1,
          price: 1100
        }]
      }],
      ops: []
    }, {
      id: "arm-10",
      name: "RX-6/10 · 10 kg",
      price: 5800,
      sub: "General purpose",
      badge: "10 KG",
      bom: [{
        system: "Structure",
        code: "ARM-C10",
        name: "10 kg arm kit",
        qty: 1,
        price: 5100,
        children: [{
          code: "WRST-10",
          name: "Wrist unit (10 kg)",
          qty: 1,
          price: 1600
        }, {
          code: "ELB-R",
          name: "Reinforced elbow",
          qty: 1,
          price: 700
        }]
      }],
      ops: []
    }, {
      id: "arm-20",
      name: "RX-6/20 · 20 kg",
      price: 12500,
      sub: "Heavy payload / long reach",
      badge: "20 KG",
      bom: [{
        system: "Structure",
        code: "ARM-C20",
        name: "20 kg arm kit",
        qty: 1,
        price: 8400,
        children: [{
          code: "WRST-20",
          name: "Wrist unit (20 kg)",
          qty: 1,
          price: 2300
        }, {
          code: "CBAL-1",
          name: "Counterbalance cylinder",
          qty: 2,
          price: 650
        }]
      }],
      ops: [{
        code: "CBAL-01",
        name: "Counterbalance Fit",
        seq: 35
      }]
    }]
  }, {
    id: "controller",
    name: "Controller",
    icon: "bi-cpu",
    multi: false,
    required: true,
    hint: "Cabinet, drives and path computer.",
    options: [{
      id: "ctl-compact",
      name: "SC-1 Compact",
      price: 0,
      sub: "Cell-mount cabinet",
      bom: [{
        system: "Electrical",
        code: "CTL-SC1",
        name: "SC-1 compact cabinet",
        qty: 1,
        price: 2600,
        children: [{
          code: "PSU-1",
          name: "Drive PSU",
          qty: 1,
          price: 600
        }, {
          code: "IO-16",
          name: "I/O board (16ch)",
          qty: 1,
          price: 300
        }]
      }],
      ops: [{
        code: "CTRL-01",
        name: "Controller Marriage",
        seq: 60
      }]
    }, {
      id: "ctl-std",
      name: "SC-3 Standard",
      price: 3400,
      sub: "Floor cabinet, expandable I/O",
      bom: [{
        system: "Electrical",
        code: "CTL-SC3",
        name: "SC-3 standard cabinet",
        qty: 1,
        price: 4200,
        children: [{
          code: "PSU-2",
          name: "Drive PSU (heavy)",
          qty: 1,
          price: 900
        }, {
          code: "IO-64",
          name: "I/O rack (64ch)",
          qty: 1,
          price: 700
        }]
      }],
      ops: [{
        code: "CTRL-01",
        name: "Controller Marriage",
        seq: 60
      }]
    }, {
      id: "ctl-perf",
      name: "SC-5 High-Path",
      price: 8200,
      sub: "Path co-processor, 7-axis ready",
      bom: [{
        system: "Electrical",
        code: "CTL-SC5",
        name: "SC-5 high-path cabinet",
        qty: 1,
        price: 7400,
        children: [{
          code: "PSU-3",
          name: "Drive PSU (perf)",
          qty: 1,
          price: 1100
        }, {
          code: "CPU-P",
          name: "Path co-processor",
          qty: 1,
          price: 1800
        }, {
          code: "IO-64",
          name: "I/O rack (64ch)",
          qty: 1,
          price: 700
        }]
      }],
      ops: [{
        code: "CTRL-01",
        name: "Controller Marriage",
        seq: 60
      }]
    }]
  }, {
    id: "mount",
    name: "Mounting",
    icon: "bi-arrows-move",
    multi: false,
    required: true,
    hint: "How the robot is installed in the cell.",
    options: [{
      id: "mnt-floor",
      name: "Floor Mount",
      price: 0,
      sub: "Standard baseplate",
      bom: [{
        system: "Structure",
        code: "MNT-FL",
        name: "Floor baseplate kit",
        qty: 1,
        price: 450,
        children: []
      }],
      ops: []
    }, {
      id: "mnt-wall",
      name: "Wall Mount",
      price: 600,
      sub: "Side bracket + shimming",
      bom: [{
        system: "Structure",
        code: "MNT-WL",
        name: "Wall bracket kit",
        qty: 1,
        price: 900,
        children: []
      }],
      ops: [{
        code: "MNT-02",
        name: "Bracket Prep & Shim",
        seq: 15
      }]
    }, {
      id: "mnt-ceiling",
      name: "Ceiling Mount",
      price: 900,
      sub: "Inverted kit, drip protection",
      bom: [{
        system: "Structure",
        code: "MNT-CL",
        name: "Inverted-mount kit",
        qty: 1,
        price: 1200,
        children: [{
          code: "DRIP-1",
          name: "Drip shield",
          qty: 1,
          price: 250
        }]
      }],
      ops: [{
        code: "MNT-02",
        name: "Inverted-Mount Prep",
        seq: 15
      }]
    }]
  }, {
    id: "effector",
    name: "End Effector",
    icon: "bi-magic",
    multi: false,
    required: true,
    hint: "Tooling on the wrist flange.",
    options: [{
      id: "ee-none",
      name: "Customer-Supplied",
      price: 0,
      sub: "Bare ISO 9409 flange",
      ee: "none",
      bom: [],
      ops: []
    }, {
      id: "ee-grip",
      name: "2-Finger Servo Gripper",
      price: 2800,
      sub: "Parallel, force-controlled",
      ee: "grip",
      bom: [{
        system: "Tooling",
        code: "EE-GRIP",
        name: "Servo gripper",
        qty: 1,
        price: 2400,
        children: [{
          code: "FING-2",
          name: "Finger set",
          qty: 2,
          price: 180
        }, {
          code: "SRV-G",
          name: "Gripper servo",
          qty: 1,
          price: 600
        }]
      }],
      ops: []
    }, {
      id: "ee-vac",
      name: "Vacuum Plate (4-cup)",
      price: 2200,
      sub: "Venturi vacuum, foam seal",
      ee: "vac",
      bom: [{
        system: "Tooling",
        code: "EE-VAC",
        name: "Vacuum plate",
        qty: 1,
        price: 1500,
        children: [{
          code: "CUP-4",
          name: "Suction cup",
          qty: 4,
          price: 60
        }, {
          code: "VENT-1",
          name: "Venturi generator",
          qty: 1,
          price: 420
        }]
      }],
      ops: []
    }, {
      id: "ee-weld",
      name: "MIG Weld Torch",
      price: 4800,
      sub: "Water-cooled, wire feeder",
      ee: "weld",
      bom: [{
        system: "Tooling",
        code: "EE-WELD",
        name: "MIG torch package",
        qty: 1,
        price: 3900,
        children: [{
          code: "TRCH-1",
          name: "Water-cooled torch",
          qty: 1,
          price: 1600
        }, {
          code: "FEED-1",
          name: "Wire feeder",
          qty: 1,
          price: 1300
        }]
      }],
      ops: [{
        code: "WELD-01",
        name: "Torch Fit & Purge Test",
        seq: 65
      }]
    }]
  }, {
    id: "color",
    name: "Finish",
    icon: "bi-palette",
    multi: false,
    required: true,
    hint: "Paint finish.",
    options: [{
      id: "col-orange",
      name: "Safety Orange",
      price: 0,
      swatch: "#e95b15",
      bom: [{
        system: "Structure",
        code: "PNT-ORG",
        name: "Paint — Safety Orange",
        qty: 1,
        price: 250,
        children: []
      }],
      ops: []
    }, {
      id: "col-graphite",
      name: "Graphite",
      price: 400,
      swatch: "#3d3d3d",
      bom: [{
        system: "Structure",
        code: "PNT-GPH",
        name: "Paint — Graphite",
        qty: 1,
        price: 400,
        children: []
      }],
      ops: []
    }, {
      id: "col-white",
      name: "Cleanroom White",
      price: 900,
      swatch: "#eef0f2",
      bom: [{
        system: "Structure",
        code: "PNT-WHT",
        name: "Paint — Cleanroom White (low-particle)",
        qty: 1,
        price: 900,
        children: []
      }],
      ops: []
    }, {
      id: "col-gold",
      name: "Heritage Gold",
      price: 1200,
      swatch: "#f1c40f",
      bom: [{
        system: "Structure",
        code: "PNT-GLD",
        name: "Paint — Heritage Gold (signature)",
        qty: 1,
        price: 1200,
        children: []
      }],
      ops: [{
        code: "PAINT-02",
        name: "Signature Paint Cell",
        seq: 22
      }]
    }]
  }, {
    id: "packages",
    name: "Packages",
    icon: "bi-box-seam",
    multi: true,
    required: false,
    hint: "Bundled hardware options.",
    options: [{
      id: "pkg-vision",
      name: "Vision Package",
      price: 5500,
      sub: "2D cameras, lighting, GPU",
      bom: [{
        system: "Electrical",
        code: "PKG-VIS",
        name: "Vision kit",
        qty: 1,
        price: 4800,
        children: [{
          code: "CAM-2D",
          name: "2D camera",
          qty: 2,
          price: 1200
        }, {
          code: "LGT-BAR",
          name: "LED light bar",
          qty: 2,
          price: 300
        }, {
          code: "GPU-1",
          name: "Vision GPU module",
          qty: 1,
          price: 1800
        }]
      }],
      ops: [{
        code: "VIS-01",
        name: "Vision Calibration",
        seq: 92
      }]
    }, {
      id: "pkg-track",
      name: "7th-Axis Track",
      priceExpr: "2400 + 1.5 * railLen",
      price: 2400,
      sub: "Carriage + rail, priced per mm",
      bom: [{
        system: "Motion",
        code: "PKG-TRK",
        name: "Linear track system",
        qty: 1,
        price: 1800,
        children: [{
          code: "TRK-CAR",
          name: "Track carriage",
          qty: 1,
          price: 1800
        }, {
          code: "RAIL-SEG",
          name: "Rail segment (1 m)",
          qtyExpr: "railSegs",
          price: 650
        }]
      }],
      ops: [{
        code: "TRK-01",
        name: "Track Install & Align",
        seq: 12
      }]
    }, {
      id: "pkg-dress",
      name: "Dress Pack",
      price: 900,
      sub: "Cable chain, segments scale with reach",
      bom: [{
        system: "Electrical",
        code: "PKG-DRS",
        name: "Dress-pack kit",
        qty: 1,
        price: 400,
        children: [{
          code: "DRS-SEG",
          name: "Dress segment",
          qtyExpr: "dressSegs",
          price: 180
        }]
      }],
      ops: []
    }, {
      id: "pkg-clean",
      name: "Cleanroom Package",
      price: 3200,
      sub: "ISO 5 — sealed bellows, LP grease",
      bom: [{
        system: "Structure",
        code: "PKG-CLN",
        name: "Cleanroom kit",
        qty: 1,
        price: 3000,
        children: [{
          code: "BELW-1",
          name: "Sealed bellows set",
          qty: 1,
          price: 1400
        }, {
          code: "GRS-LP",
          name: "Low-particle grease service",
          qty: 1,
          price: 600
        }]
      }],
      ops: []
    }, {
      id: "pkg-foundry",
      name: "Foundry Package",
      price: 2600,
      sub: "IP67 — heat jacket, sealed connectors",
      bom: [{
        system: "Structure",
        code: "PKG-FDY",
        name: "Foundry protection kit",
        qty: 1,
        price: 2400,
        children: [{
          code: "JKT-HT",
          name: "Heat-resist jacket",
          qty: 1,
          price: 1200
        }, {
          code: "CONN-67",
          name: "IP67 connector set",
          qty: 1,
          price: 700
        }]
      }],
      ops: []
    }]
  }, {
    id: "software",
    name: "Software",
    icon: "bi-code-square",
    multi: true,
    required: false,
    hint: "Licensed application suites.",
    options: [{
      id: "sw-path",
      name: "Path Optimization Suite",
      price: 1900,
      sub: "Cycle-time & jerk tuning",
      bom: [{
        system: "Software",
        code: "LIC-PATH",
        name: "Path optimization license",
        qty: 1,
        price: 1900,
        children: []
      }],
      ops: []
    }, {
      id: "sw-pallet",
      name: "Palletizing Suite",
      price: 2400,
      sub: "Pattern builder + mixed-SKU",
      bom: [{
        system: "Software",
        code: "LIC-PAL",
        name: "Palletizing license",
        qty: 1,
        price: 2400,
        children: []
      }],
      ops: []
    }, {
      id: "sw-weld",
      name: "Welding Suite",
      price: 2100,
      sub: "Seam tracking, weave control",
      bom: [{
        system: "Software",
        code: "LIC-WELD",
        name: "Welding license",
        qty: 1,
        price: 2100,
        children: []
      }],
      ops: [{
        code: "WELD-02",
        name: "Weld Package Commissioning",
        seq: 93
      }]
    }]
  }],
  // Declarative constraint rules. Types: requires | requiresOneOf | excludes | formula
  rules: [{
    id: "R1",
    type: "requires",
    when: ["ee-weld"],
    then: ["sw-weld"],
    msg: "MIG torch requires the Welding software suite."
  }, {
    id: "R2",
    type: "requires",
    when: ["ee-weld"],
    then: ["pkg-dress"],
    msg: "MIG torch requires the Dress Pack for torch cabling."
  }, {
    id: "R3",
    type: "requiresOneOf",
    when: ["pkg-vision"],
    then: ["ctl-std", "ctl-perf"],
    msg: "Vision Package requires the SC-3 or SC-5 controller."
  }, {
    id: "R4",
    type: "requires",
    when: ["col-white"],
    then: ["pkg-clean"],
    msg: "Cleanroom White requires the Cleanroom Package."
  }, {
    id: "R5",
    type: "requiresOneOf",
    when: ["sw-pallet"],
    then: ["ee-grip", "ee-vac"],
    msg: "Palletizing Suite requires a gripper or vacuum effector."
  }, {
    id: "R6",
    type: "requires",
    when: ["arm-20"],
    then: ["ctl-perf"],
    msg: "The 20 kg arm requires the SC-5 High-Path controller."
  }, {
    id: "R7",
    type: "requires",
    when: ["pkg-track"],
    then: ["pkg-dress"],
    msg: "The 7th-axis track requires the Dress Pack cable chain."
  }, {
    id: "R8",
    type: "requires",
    when: ["pkg-track"],
    then: ["ctl-perf"],
    msg: "The 7th-axis track requires the SC-5 controller (7-axis drives)."
  }, {
    id: "X1",
    type: "excludes",
    when: ["pkg-clean"],
    then: ["pkg-foundry"],
    msg: "Cleanroom and Foundry packages are mutually exclusive."
  }, {
    id: "X2",
    type: "excludes",
    when: ["mnt-ceiling"],
    then: ["pkg-track"],
    msg: "Ceiling mount excludes the 7th-axis track."
  }, {
    id: "F1",
    type: "formula",
    expr: "has('pkg-track') ? railLen > 0 : true",
    msg: "7th-Axis Track needs a rail length above 0 mm.",
    fixAttr: {
      id: "railLen",
      value: 2000
    },
    refs: ["pkg-track", "railLen"]
  }, {
    id: "F2",
    type: "formula",
    expr: "reach <= 1400 || has('arm-20')",
    msg: "Reach beyond 1400 mm requires the 20 kg arm class.",
    fixChoose: "arm-20",
    refs: ["reach", "arm-20"]
  }]
};
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/data.js", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/engine.js
try { (() => {
/* ============================================================
   BoM Configurator — constraint engine (pure logic)
   window.CFG
   ============================================================ */
(function () {
  const CAR = window.CAR;
  const opt = id => {
    for (const g of CAR.groups) {
      const o = g.options.find(o => o.id === id);
      if (o) return o;
    }
    return null;
  };
  const groupOf = id => CAR.groups.find(g => g.options.some(o => o.id === id));
  const groupById = gid => CAR.groups.find(g => g.id === gid);

  // ── Formula / calculated-attribute engine (D365-style) ─────────────────────
  const ATTRS = CAR.attributes || [];
  const CALC = CAR.calc || [];
  const HELPERS = {
    ceil: Math.ceil,
    floor: Math.floor,
    round: Math.round,
    abs: Math.abs,
    min: Math.min,
    max: Math.max,
    roundup: Math.ceil,
    rounddown: Math.floor
  };
  const attrName = id => {
    const a = ATTRS.find(x => x.id === id);
    return a ? a.name : id;
  };
  const attrDef = id => ATTRS.find(x => x.id === id);

  // Build the evaluation scope: helpers + has('optId') + each attribute value +
  // each calculated value (resolved in declaration order so later calc can use earlier).
  function context(sel) {
    const scope = Object.assign({}, HELPERS);
    scope.has = id => isSel(sel, id);
    const attrs = sel && sel.attrs || {};
    for (const a of ATTRS) scope[a.id] = Number(attrs[a.id] != null ? attrs[a.id] : a.default);
    const calcVals = {};
    for (const c of CALC) {
      const v = evalExpr(c.expr, scope);
      scope[c.id] = v;
      calcVals[c.id] = v;
    }
    return {
      scope,
      calcVals
    };
  }

  // Safe-ish expression eval over a fixed scope (prototype). Non-finite -> 0.
  function evalExpr(expr, scope) {
    try {
      const keys = Object.keys(scope);
      const fn = new Function(...keys, "return (" + expr + ");");
      const v = fn(...keys.map(k => scope[k]));
      return typeof v === "number" && !isFinite(v) ? 0 : v;
    } catch (e) {
      return 0;
    }
  }

  // Calculated attributes with their resolved values (for the UI read-outs).
  function computeCalc(sel) {
    const {
      scope
    } = context(sel);
    return CALC.map(c => ({
      id: c.id,
      name: c.name,
      unit: c.unit || "",
      expr: c.expr,
      hint: c.hint,
      value: evalExpr(c.expr, scope)
    }));
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
    const out = {
      ok: true,
      error: "",
      unknown: [],
      value: undefined
    };
    const src = String(expr || "").trim();
    if (!src) {
      out.ok = false;
      out.error = "Empty expression";
      return out;
    }
    // strip string + number literals so their contents aren't read as identifiers
    const stripped = src.replace(/'[^']*'|"[^"]*"/g, " ").replace(/\b\d+(\.\d+)?\b/g, " ");
    // an identifier preceded by '.' is a property access (e.g. Math.ceil) — drop those
    const idents = (stripped.match(/(?:\.)?[A-Za-z_$][A-Za-z0-9_$]*/g) || []).filter(t => t[0] !== ".");
    const known = knownIdents();
    const unknown = [...new Set(idents.filter(id => !known.has(id)))];
    if (unknown.length) {
      out.ok = false;
      out.unknown = unknown;
      out.error = "Unknown: " + unknown.join(", ");
    }
    // syntax check via compile + sample eval against default selection
    try {
      const {
        scope
      } = context(defaultSelections());
      const keys = Object.keys(scope),
        fn = new Function(...keys, "return (" + src + ");");
      const v = fn(...keys.map(k => scope[k]));
      out.value = v;
      if (opts.boolean && typeof v !== "boolean") {/* allow truthy, just note */}
    } catch (e) {
      out.ok = false;
      out.error = (out.unknown.length ? out.error + " · " : "") + "Syntax error";
    }
    return out;
  }
  function setAttr(sel, id, val) {
    sel = clone(sel);
    sel.attrs = Object.assign({}, sel.attrs, {
      [id]: val
    });
    return sel;
  }

  // Derive the effective [min,max] for a numeric attribute from active bound rules.
  // Recognises simple comparisons that reference ONLY this attribute, e.g.
  // "rackLen <= 180", "seats >= 4", "orderQty < 20". Combines with the attribute's
  // own declared min/max (tightest wins). Returns { min, max, fromRules:[ruleIds] }.
  function attrBounds(attrId, enabledRuleIds) {
    const a = attrDef(attrId) || {};
    let lo = a.min != null ? a.min : -Infinity;
    let hi = a.max != null ? a.max : Infinity;
    const fromRules = [];
    const rules = CAR.rules.filter(r => r.type === "formula" && (!enabledRuleIds || enabledRuleIds.includes(r.id)));
    const re = /^\s*([A-Za-z_$][\w$]*)\s*(<=|>=|<|>)\s*(-?\d+(?:\.\d+)?)\s*$/;
    for (const r of rules) {
      // each &&-joined clause may carry one bound on this attribute
      for (const clause of String(r.expr).split("&&")) {
        const m = clause.match(re);
        if (!m || m[1] !== attrId) continue;
        const op = m[2],
          n = Number(m[3]),
          step = a.step || 1;
        if (op === "<=") hi = Math.min(hi, n);else if (op === "<") hi = Math.min(hi, n - step);else if (op === ">=") lo = Math.max(lo, n);else if (op === ">") lo = Math.max(lo, n + step);
        if (!fromRules.includes(r.id)) fromRules.push(r.id);
      }
    }
    return {
      min: lo,
      max: hi,
      fromRules,
      clamped: fromRules.length > 0
    };
  }

  // selection helpers — sel: { groupId: optionId | [optionIds] }
  const isSel = (sel, id) => {
    const g = groupOf(id);
    if (!g) return false;
    const v = sel[g.id];
    return g.multi ? (v || []).includes(id) : v === id;
  };
  const allSel = (sel, ids) => ids.every(id => isSel(sel, id));
  const anySel = (sel, ids) => ids.some(id => isSel(sel, id));

  // Which groups are applicable given current selections (e.g. battery only for EV)
  function applicableGroups(sel) {
    return CAR.groups.filter(g => !g.appliesIf || anySel(sel, g.appliesIf));
  }
  const defaultSelections = () => {
    const sel = {};
    for (const g of CAR.groups) {
      if (g.multi) sel[g.id] = [];else if (g.required) sel[g.id] = g.options[0].id;else sel[g.id] = null;
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
                violations.push({
                  id: r.id,
                  msg: r.msg,
                  fixLabel: `Add ${opt(tid).name}`,
                  fix: s => {
                    s = clone(s);
                    if (!s[tg.id].includes(tid)) s[tg.id] = [...s[tg.id], tid];
                    return s;
                  }
                });
              }
            } else {
              // single-select: force the required option; disable siblings
              for (const o of tg.options) if (o.id !== tid) disabled[o.id] = r.msg;
              if (sel[tg.id] !== tid && appIds.includes(tg.id)) {
                sel[tg.id] = tid;
                changed = true;
                notes.push(`${r.msg} — set ${tg.name} to ${opt(tid).name}.`);
              }
            }
          }
        } else if (r.type === "requiresOneOf") {
          if (!anySel(sel, r.then)) {
            const first = r.then[0];
            violations.push({
              id: r.id,
              msg: r.msg,
              fixLabel: `Choose ${opt(first).name}`,
              fix: s => applyChoice(s, first)
            });
          }
        } else if (r.type === "excludes") {
          // disable the 'then' options while 'when' active; flag if already on
          for (const tid of r.then) {
            disabled[tid] = r.msg;
            if (isSel(sel, tid)) {
              const tg = groupOf(tid);
              violations.push({
                id: r.id,
                msg: r.msg,
                fixLabel: `Remove ${opt(tid).name}`,
                fix: s => {
                  s = clone(s);
                  if (tg.multi) s[tg.id] = s[tg.id].filter(x => x !== tid);else s[tg.id] = tg.required ? tg.options[0].id : null;
                  return s;
                }
              });
            }
          }
        }
      }
      if (!changed) break;
    }

    // Formula constraints: the expression must evaluate truthy, else it's a violation.
    for (const r of rules) {
      if (r.type !== "formula") continue;
      const {
        scope
      } = context(sel);
      if (!evalExpr(r.expr, scope)) {
        const v = {
          id: r.id,
          msg: r.msg
        };
        if (r.fixChoose) {
          v.fixLabel = `Choose ${opt(r.fixChoose).name}`;
          v.fix = s => applyChoice(s, r.fixChoose);
        } else if (r.fixAttr) {
          v.fixLabel = `Set ${attrName(r.fixAttr.id)} = ${r.fixAttr.value}`;
          v.fix = s => setAttr(s, r.fixAttr.id, r.fixAttr.value);
        } else {
          v.fixLabel = "Review";
          v.fix = s => s;
        }
        violations.push(v);
      }
    }

    // de-dupe violations by id+msg
    const seen = new Set();
    const uniqV = violations.filter(v => {
      const k = v.id + v.msg;
      if (seen.has(k)) return false;
      seen.add(k);
      return true;
    });
    return {
      sel,
      disabled,
      violations: uniqV,
      notes
    };
  }

  // Apply a single choice (respecting single/multi) WITHOUT reconcile.
  function applyChoice(sel, id) {
    sel = clone(sel);
    const g = groupOf(id);
    if (!g) return sel;
    if (g.multi) sel[g.id] = sel[g.id].includes(id) ? sel[g.id].filter(x => x !== id) : [...sel[g.id], id];else sel[g.id] = id;
    return sel;
  }

  // Resolve the multi-level BoM grouped by system. Quantities may be formula-driven
  // (qtyExpr) and are evaluated against the current attribute/selection context.
  function resolveBom(sel) {
    const {
      scope
    } = context(sel);
    const qv = l => l.qtyExpr ? Math.max(0, Math.round(evalExpr(l.qtyExpr, scope))) : l.qty;
    const resolve = l => Object.assign({}, l, {
      qty: qv(l),
      children: (l.children || []).map(c => Object.assign({}, c, {
        qty: qv(c)
      }))
    });
    const lines = CAR.baseBom.map(b => Object.assign(resolve(b), {
      source: "Platform"
    }));
    for (const g of applicableGroups(sel)) {
      const chosen = g.multi ? sel[g.id] : sel[g.id] ? [sel[g.id]] : [];
      for (const id of chosen) {
        const o = opt(id);
        if (o && o.bom) for (const b of o.bom) lines.push(Object.assign(resolve(b), {
          source: o.name
        }));
      }
    }
    const bySystem = {};
    for (const l of lines) {
      (bySystem[l.system] = bySystem[l.system] || []).push(l);
    }
    const lineTotal = l => l.price * l.qty + (l.children || []).reduce((s, c) => s + c.price * c.qty, 0);
    const systems = Object.keys(bySystem).map(name => ({
      name,
      assemblies: bySystem[name],
      total: bySystem[name].reduce((s, l) => s + lineTotal(l), 0)
    }));
    const partCount = lines.reduce((s, l) => s + l.qty + (l.children || []).reduce((t, c) => t + c.qty, 0), 0);
    const cost = systems.reduce((s, x) => s + x.total, 0);
    return {
      systems,
      partCount,
      cost,
      lineTotal
    };
  }

  // Price rollup: base + each option delta (priceExpr overrides a fixed price).
  function priceRollup(sel) {
    const {
      scope
    } = context(sel);
    const lines = [{
      label: `${CAR.platform.name} base`,
      amount: CAR.platform.basePrice,
      base: true
    }];
    for (const g of applicableGroups(sel)) {
      const chosen = g.multi ? sel[g.id] : sel[g.id] ? [sel[g.id]] : [];
      for (const id of chosen) {
        const o = opt(id);
        if (!o) continue;
        const amt = o.priceExpr ? Math.round(evalExpr(o.priceExpr, scope)) : o.price;
        if (amt > 0) lines.push({
          label: o.name,
          amount: amt,
          group: g.name,
          formula: !!o.priceExpr
        });
      }
    }
    const total = lines.reduce((s, l) => s + l.amount, 0);
    return {
      lines,
      total
    };
  }

  // Generate the routing (ordered, de-duped operations) — ties to Process Manager.
  function generateRouting(sel) {
    const ops = [...CAR.baseOps];
    for (const g of applicableGroups(sel)) {
      const chosen = g.multi ? sel[g.id] : sel[g.id] ? [sel[g.id]] : [];
      for (const id of chosen) {
        const o = opt(id);
        if (o && o.ops) ops.push(...o.ops);
      }
    }
    const map = {};
    for (const o of ops) map[o.code] = o; // de-dupe by code
    return Object.values(map).sort((a, b) => a.seq - b.seq);
  }

  // Completion: every required & applicable group has a selection, no violations.
  function status(sel, violations) {
    const need = applicableGroups(sel).filter(g => g.required);
    const done = need.filter(g => sel[g.id]).length;
    return {
      done,
      total: need.length,
      complete: done === need.length && violations.length === 0
    };
  }
  function clone(o) {
    return JSON.parse(JSON.stringify(o));
  }
  // ── Resolved export: the full, serialisable spec behind a released work order ──
  // Surfaces everything the configurator computed: attribute values & their bounds,
  // calculated attributes with formulas, formula-driven BoM quantities, per-unit
  // price formulas, the order-quantity rollup, the active rule set, and routing.
  function exportSpec(sel, enabled) {
    const ctx = context(sel);
    const oq = Number(sel.attrs && sel.attrs.orderQty || 1);
    const bom = resolveBom(sel);
    const price = priceRollup(sel);
    const attributes = ATTRS.map(a => {
      const b = a.kind === "range" ? attrBounds(a.id, enabled) : {
        clamped: false
      };
      return {
        id: a.id,
        name: a.name,
        value: Number(sel.attrs && sel.attrs[a.id] != null ? sel.attrs[a.id] : a.default),
        unit: a.unit || "",
        kind: a.kind,
        bounds: a.kind === "range" ? {
          min: isFinite(b.min) ? b.min : a.min,
          max: isFinite(b.max) ? b.max : a.max,
          clampedBy: b.fromRules || []
        } : null
      };
    });
    const calculated = CALC.map(c => ({
      id: c.id,
      name: c.name,
      expr: c.expr,
      unit: c.unit || "",
      value: evalExpr(c.expr, ctx.scope)
    }));
    const selections = [];
    for (const g of applicableGroups(sel)) {
      const ids = g.multi ? sel[g.id] || [] : sel[g.id] ? [sel[g.id]] : [];
      ids.forEach(id => {
        const o = opt(id);
        if (o) selections.push({
          group: g.name,
          groupId: g.id,
          optionId: id,
          name: o.name,
          unitPrice: o.priceExpr ? Math.round(evalExpr(o.priceExpr, ctx.scope)) : o.price || 0,
          priceFormula: o.priceExpr || null
        });
      });
    }
    const rows = [];
    bom.systems.forEach(s => s.assemblies.forEach(a => {
      rows.push({
        level: 1,
        system: s.name,
        source: a.source || "",
        code: a.code,
        name: a.name,
        qty: a.qty,
        qtyFormula: a.qtyExpr || null,
        unitPrice: a.price,
        ext: a.price * a.qty
      });
      (a.children || []).forEach(c => rows.push({
        level: 2,
        system: s.name,
        source: a.source || "",
        code: c.code,
        name: c.name,
        qty: c.qty,
        qtyFormula: c.qtyExpr || null,
        unitPrice: c.price,
        ext: c.price * c.qty
      }));
    }));
    const priceLines = price.lines.map(l => {
      const o = l.label && opt0ByName(l.label);
      return {
        label: l.label,
        amount: l.amount,
        base: !!l.base,
        group: l.group || null,
        priceFormula: l.formula && o ? o.priceExpr : null
      };
    });
    const rules = CAR.rules.filter(r => !enabled || enabled.includes(r.id)).map(r => ({
      id: r.id,
      type: r.type,
      msg: r.msg,
      when: r.when || null,
      then: r.then || null,
      expr: r.expr || null,
      refs: r.refs || null
    }));

    // Configuration fingerprint (ISO 10007 traceability): deterministic short hash
    // over the resolved selections + attributes, so an identical config yields an
    // identical document-control id.
    const fpSrc = JSON.stringify({
      p: CAR.platform.code,
      s: selections.map(s => s.optionId),
      a: attributes.map(a => a.id + ":" + a.value)
    });
    let h = 5381;
    for (let i = 0; i < fpSrc.length; i++) {
      h = (h << 5) + h + fpSrc.charCodeAt(i) >>> 0;
    }
    const configHash = ("00000000" + h.toString(16).toUpperCase()).slice(-8);
    const comp = CAR.compliance || {};
    const docNo = (comp.formNo || "QF-BOM-001") + "/" + CAR.platform.code + "-" + configHash;
    return {
      meta: {
        platform: {
          name: CAR.platform.name,
          code: CAR.platform.code
        },
        basePrice: CAR.platform.basePrice,
        generatedAt: new Date().toISOString(),
        orderQty: oq,
        compliance: comp,
        configHash,
        docNo
      },
      attributes,
      calculated,
      selections,
      bom: {
        rows,
        partCount: bom.partCount,
        cost: bom.cost
      },
      price: {
        lines: priceLines,
        perUnit: price.total,
        orderQty: oq,
        orderTotal: price.total * oq
      },
      routing: generateRouting(sel).map((o, i) => ({
        step: i + 1,
        code: o.code,
        name: o.name
      })),
      rules
    };
  }
  function opt0ByName(name) {
    for (const g of CAR.groups) {
      const o = g.options.find(o => o.name === name);
      if (o) return o;
    }
    return null;
  }

  // CSV of the resolved multi-level BoM (one row per part, formula provenance kept).
  function bomToCSV(spec) {
    const head = ["Level", "System", "Source", "Code", "Part", "Qty", "QtyFormula", "UnitPrice", "ExtPrice"];
    const esc = v => {
      v = String(v == null ? "" : v);
      return /[",\n]/.test(v) ? '"' + v.replace(/"/g, '""') + '"' : v;
    };
    const lines = [head.join(",")];
    spec.bom.rows.forEach(r => lines.push([r.level, r.system, r.source, r.code, r.name, r.qty, r.qtyFormula || "", r.unitPrice, r.ext].map(esc).join(",")));
    return lines.join("\n");
  }
  function specToJSON(spec) {
    return JSON.stringify(spec, null, 2);
  }
  function dependencyEdges(enabledRuleIds) {
    const rules = CAR.rules.filter(r => !enabledRuleIds || enabledRuleIds.includes(r.id));
    const edges = [];
    for (const r of rules) {
      const froms = r.when || (r.refs ? [r.refs[0]] : []);
      const tos = r.then || (r.refs ? r.refs.slice(1) : []);
      for (const w of froms) for (const t of tos) if (opt(w) && opt(t))
        // only draw between real option nodes; skip attribute/calc refs
        edges.push({
          rule: r.id,
          type: r.type,
          from: w,
          to: t,
          msg: r.msg
        });
    }
    return edges;
  }
  window.CFG = {
    opt,
    groupOf,
    groupById,
    isSel,
    applicableGroups,
    defaultSelections,
    reconcile,
    applyChoice,
    resolveBom,
    priceRollup,
    generateRouting,
    status,
    dependencyEdges,
    context,
    evalExpr,
    computeCalc,
    validateExpr,
    setAttr,
    attrBounds,
    attrName,
    attrDef,
    exportSpec,
    bomToCSV,
    specToJSON,
    attributes: ATTRS,
    calc: CALC,
    fmt: fmtMoney
  };
  function fmtMoney(n) {
    return "$" + Math.round(n).toLocaleString("en-US");
  }
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/engine.js", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/imagestore.js
try { (() => {
/* ============================================================
   BoM Configurator — author-defined preview image store
   window.PreviewStore

   The product preview is NOT hard-coded to the car. An author can
   upload a BASE image (the product in its default state) plus per-
   option OVERLAY images (ideally transparent PNGs) that composite
   on top when that option is selected — the standard layered-render
   approach used by real product configurators (robots, 3D printers,
   machinery, anything). Stored in localStorage as downscaled data
   URLs so it survives reloads. If no images are set, the preview
   falls back to the built-in schematic (the car example).
   ============================================================ */
(function () {
  const KEY = "sqpro.bomcfg.preview.v2"; // v2: option ids changed (car → robot platform)
  let cache = null;
  const subs = new Set();
  function load() {
    if (cache) return cache;
    try {
      cache = JSON.parse(localStorage.getItem(KEY)) || {};
    } catch (e) {
      cache = {};
    }
    if (!cache.overlays) cache.overlays = {};
    if (!cache.meta) cache.meta = {};
    return cache;
  }
  function save() {
    try {
      localStorage.setItem(KEY, JSON.stringify(cache));
    } catch (e) {
      console.warn("PreviewStore: localStorage full — image not saved", e);
    }
    subs.forEach(f => {
      try {
        f();
      } catch (_) {}
    });
  }

  // Read a File, downscale to maxDim, return a PNG data URL (keeps transparency).
  function fileToDataUrl(file, maxDim) {
    return new Promise((resolve, reject) => {
      if (!file || !/^image\//.test(file.type)) return reject(new Error("Not an image"));
      const fr = new FileReader();
      fr.onerror = () => reject(fr.error);
      fr.onload = () => {
        const img = new Image();
        img.onerror = () => reject(new Error("Bad image"));
        img.onload = () => {
          let w = img.naturalWidth,
            h = img.naturalHeight;
          const m = maxDim || 1000;
          if (w > m || h > m) {
            const s = Math.min(m / w, m / h);
            w = Math.round(w * s);
            h = Math.round(h * s);
          }
          const cv = document.createElement("canvas");
          cv.width = w;
          cv.height = h;
          cv.getContext("2d").drawImage(img, 0, 0, w, h);
          resolve({
            src: cv.toDataURL("image/png"),
            w,
            h
          });
        };
        img.src = fr.result;
      };
      fr.readAsDataURL(file);
    });
  }
  const api = {
    get() {
      return load();
    },
    base() {
      return load().base || null;
    },
    overlay(id) {
      return load().overlays[id] || null;
    },
    setBase(rec) {
      load();
      cache.base = rec.src;
      cache.meta.baseW = rec.w;
      cache.meta.baseH = rec.h;
      save();
    },
    setOverlay(id, rec) {
      load();
      cache.overlays[id] = rec.src;
      save();
    },
    remove(id) {
      load();
      if (id === "__base__") {
        delete cache.base;
        delete cache.meta.baseW;
        delete cache.meta.baseH;
      } else delete cache.overlays[id];
      save();
    },
    clearAll() {
      cache = {
        overlays: {},
        meta: {}
      };
      save();
    },
    hasImages() {
      load();
      return !!cache.base || Object.keys(cache.overlays).length > 0;
    },
    aspect() {
      load();
      return cache.meta.baseW && cache.meta.baseH ? cache.meta.baseW / cache.meta.baseH : 16 / 9;
    },
    overlayCount() {
      return Object.keys(load().overlays).length;
    },
    subscribe(fn) {
      subs.add(fn);
      return () => subs.delete(fn);
    },
    fileToDataUrl
  };
  window.PreviewStore = api;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/imagestore.js", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/preview.js
try { (() => {
/* ============================================================
   BoM Configurator — product preview + image authoring
   window.ProductPreview  — composites author images, else schematic
   window.PreviewAuthor    — the authoring panel (mode: "preview")
   ============================================================ */
(function () {
  const {
    useState,
    useRef,
    useEffect
  } = React;
  const DS = window.SequencerProDesignSystem_5eb90b;
  const {
    Button
  } = DS;
  const C = window.CFG,
    CAR = window.CAR;
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
      const ids = g.multi ? sel[g.id] || [] : sel[g.id] ? [sel[g.id]] : [];
      ids.forEach(id => {
        const src = PS.overlay(id);
        if (src) out.push({
          id,
          src
        });
      });
    });
    return out;
  }

  // ── Live composited product preview (falls back to the schematic car) ──
  function ProductPreview({
    sel,
    schematic
  }) {
    usePreviewStore();
    const base = PS.base();
    if (!base) {
      // No author images yet → built-in schematic example.
      return React.createElement(window.SchematicRobot, schematic);
    }
    const overlays = overlaysFor(sel);
    return React.createElement("div", {
      style: {
        position: "relative",
        width: "100%",
        aspectRatio: String(PS.aspect()),
        maxWidth: 520,
        margin: "0 auto"
      }
    }, React.createElement("img", {
      src: base,
      alt: "Product",
      style: {
        position: "absolute",
        inset: 0,
        width: "100%",
        height: "100%",
        objectFit: "contain"
      }
    }), overlays.map(o => React.createElement("img", {
      key: o.id,
      src: o.src,
      alt: "",
      style: {
        position: "absolute",
        inset: 0,
        width: "100%",
        height: "100%",
        objectFit: "contain",
        pointerEvents: "none"
      }
    })));
  }

  // ── Reusable image drop / upload slot ──
  function ImageSlot({
    src,
    onPick,
    onClear,
    label,
    height = 92,
    hint
  }) {
    const inputRef = useRef(null);
    const [over, setOver] = useState(false);
    const [busy, setBusy] = useState(false);
    const take = async (file, maxDim) => {
      if (!file) return;
      setBusy(true);
      try {
        const rec = await PS.fileToDataUrl(file, maxDim);
        onPick(rec);
      } catch (e) {
        console.warn(e);
        alert("Could not read that image.");
      }
      setBusy(false);
    };
    return React.createElement("div", {
      onDragOver: e => {
        e.preventDefault();
        setOver(true);
      },
      onDragLeave: () => setOver(false),
      onDrop: e => {
        e.preventDefault();
        setOver(false);
        take(e.dataTransfer.files[0], label === "base" ? 1000 : 1000);
      },
      onClick: () => inputRef.current && inputRef.current.click(),
      style: {
        position: "relative",
        height,
        borderRadius: "var(--radius)",
        cursor: "pointer",
        overflow: "hidden",
        border: over ? "2px dashed var(--gold)" : src ? "1px solid var(--border-default)" : "1.5px dashed var(--border-strong)",
        background: src ? "var(--bg-sunken)" : over ? "var(--gold-50)" : "var(--bg-surface)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        transition: "border-color .12s, background .12s"
      }
    }, React.createElement("input", {
      ref: inputRef,
      type: "file",
      accept: "image/*",
      style: {
        display: "none"
      },
      onChange: e => {
        take(e.target.files[0], 1000);
        e.target.value = "";
      }
    }), src ? React.createElement(React.Fragment, null, React.createElement("img", {
      src,
      alt: "",
      style: {
        maxWidth: "100%",
        maxHeight: "100%",
        objectFit: "contain"
      }
    }), React.createElement("button", {
      onClick: e => {
        e.stopPropagation();
        onClear();
      },
      title: "Remove image",
      style: {
        position: "absolute",
        top: 4,
        right: 4,
        width: 20,
        height: 20,
        borderRadius: "50%",
        border: "none",
        background: "rgba(33,33,33,0.7)",
        color: "#fff",
        cursor: "pointer",
        fontSize: 12,
        lineHeight: 1,
        display: "grid",
        placeItems: "center"
      }
    }, "×")) : React.createElement("div", {
      style: {
        textAlign: "center",
        color: "var(--text-muted)",
        pointerEvents: "none",
        padding: 6
      }
    }, React.createElement("i", {
      className: "bi " + (busy ? "bi-hourglass-split" : "bi-image"),
      style: {
        fontSize: 18,
        display: "block",
        marginBottom: 3
      }
    }), React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        lineHeight: 1.3
      }
    }, busy ? "Loading…" : hint || "Drop image or click")));
  }

  // ── One option's overlay slot (label + swatch under a drop slot) ──
  function OverlayCard({
    o
  }) {
    return React.createElement("div", null, React.createElement(ImageSlot, {
      src: PS.overlay(o.id),
      label: o.id,
      height: 84,
      hint: "Overlay image",
      onPick: rec => PS.setOverlay(o.id, rec),
      onClear: () => PS.remove(o.id)
    }), React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 5,
        marginTop: 4
      }
    }, o.swatch ? React.createElement("span", {
      style: {
        width: 9,
        height: 9,
        borderRadius: "50%",
        background: o.swatch,
        border: "1px solid rgba(0,0,0,0.1)",
        flex: "none"
      }
    }) : null, React.createElement("span", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--text-secondary)",
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis"
      }
    }, o.name)));
  }

  // ── One option group: header + grid of overlay slots ──
  function GroupOverlays({
    g
  }) {
    return React.createElement("div", {
      style: {
        marginBottom: 22
      }
    }, React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 8,
        marginBottom: 9,
        paddingBottom: 6,
        borderBottom: "var(--border)"
      }
    }, React.createElement("i", {
      className: "bi " + (g.icon || "bi-dot"),
      style: {
        color: "var(--gold-600)"
      }
    }), React.createElement("span", {
      style: {
        fontWeight: 700,
        fontSize: "var(--text-md)"
      }
    }, g.name), React.createElement("span", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)",
        marginLeft: "auto",
        textTransform: "uppercase",
        letterSpacing: "0.05em"
      }
    }, "Overlay when selected")), React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "repeat(auto-fill, minmax(150px, 1fr))",
        gap: 10
      }
    }, g.options.map(o => React.createElement(OverlayCard, {
      key: o.id,
      o
    }))));
  }

  // ── Authoring panel: base image + per-option overlays ──
  function PreviewAuthor() {
    usePreviewStore();
    const sample = React.useMemo(() => C.defaultSelections(), []);
    const base = PS.base();
    return React.createElement("div", {
      style: {
        height: "100%",
        overflow: "auto",
        background: "var(--bg-app)"
      }
    }, React.createElement("div", {
      style: {
        maxWidth: 960,
        margin: "0 auto",
        padding: "22px 26px 60px",
        display: "grid",
        gridTemplateColumns: "320px 1fr",
        gap: 26,
        alignItems: "start"
      }
    },
    // Left: live preview + base + intro
    React.createElement("div", {
      style: {
        position: "sticky",
        top: 22
      }
    }, React.createElement("h2", {
      style: {
        fontSize: "var(--text-xl)",
        fontWeight: 700,
        margin: "0 0 4px",
        display: "flex",
        alignItems: "center",
        gap: 9
      }
    }, React.createElement("i", {
      className: "bi bi-images",
      style: {
        color: "var(--gold-600)"
      }
    }), "Preview images"), React.createElement("p", {
      style: {
        fontSize: "var(--text-sm)",
        color: "var(--text-secondary)",
        margin: "0 0 14px",
        lineHeight: 1.5
      }
    }, "The schematic robot is only a placeholder. Upload images of ", React.createElement("em", null, "your"), " product — photos or renders — and the preview composites them live as options are picked."), React.createElement("div", {
      style: {
        background: "var(--bg-surface)",
        border: "var(--border)",
        borderRadius: "var(--radius-md)",
        padding: 14,
        boxShadow: "var(--shadow-xs)"
      }
    }, React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        textTransform: "uppercase",
        letterSpacing: "var(--ls-caps)",
        color: "var(--text-muted)",
        marginBottom: 8
      }
    }, "Live composite"), React.createElement("div", {
      style: {
        background: "var(--bg-sunken)",
        borderRadius: "var(--radius)",
        padding: 10,
        marginBottom: 14
      }
    }, React.createElement(ProductPreview, {
      sel: sample,
      schematic: {
        color: "#e95b15",
        badge: "10 KG"
      }
    })), React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        textTransform: "uppercase",
        letterSpacing: "var(--ls-caps)",
        color: "var(--text-muted)",
        marginBottom: 6
      }
    }, "Base image"), React.createElement(ImageSlot, {
      src: base,
      label: "base",
      height: 120,
      hint: "Drop base product image",
      onPick: rec => PS.setBase(rec),
      onClear: () => PS.remove("__base__")
    }), React.createElement("p", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)",
        margin: "8px 0 0",
        lineHeight: 1.4
      }
    }, "Always-visible bottom layer. Sets the preview's aspect ratio. Overlays should share its dimensions to line up."), PS.hasImages() ? React.createElement("button", {
      onClick: () => {
        if (confirm("Remove all preview images and revert to the schematic?")) PS.clearAll();
      },
      style: {
        marginTop: 12,
        width: "100%",
        padding: "7px 0",
        border: "1px solid var(--border-strong)",
        borderRadius: "var(--radius-sm)",
        background: "var(--bg-surface)",
        color: "var(--status-fail)",
        cursor: "pointer",
        fontFamily: "var(--font-sans)",
        fontSize: "var(--text-xs)",
        fontWeight: 600
      }
    }, React.createElement("i", {
      className: "bi bi-trash",
      style: {
        marginRight: 5
      }
    }), "Clear all images") : null)),
    // Right: per-option overlay slots
    React.createElement("div", null, !base ? React.createElement("div", {
      style: {
        display: "flex",
        gap: 9,
        alignItems: "flex-start",
        background: "var(--gold-50)",
        border: "1px solid var(--gold-200)",
        borderRadius: "var(--radius)",
        padding: "10px 13px",
        marginBottom: 16,
        fontSize: "var(--text-xs)",
        color: "var(--gold-800)"
      }
    }, React.createElement("i", {
      className: "bi bi-info-circle-fill",
      style: {
        marginTop: 1
      }
    }), React.createElement("span", null, "Add a base image first. Until then, the configurator shows the built-in schematic example.")) : null, CAR.groups.map(g => React.createElement(GroupOverlays, {
      key: g.id,
      g
    })))));
  }
  Object.assign(window, {
    ProductPreview,
    PreviewAuthor,
    PreviewImageSlot: ImageSlot
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/preview.js", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/robot.js
try { (() => {
/* ============================================================
   BoM Configurator — schematic robot preview (SVG, reacts to config)
   window.SchematicRobot
   Side view of a 6-axis arm: link lengths scale with reach, the
   end effector swaps by type, a rail appears for the 7th axis,
   and the payload-class badge mirrors the arm selection.
   ============================================================ */
(function () {
  function Joint({
    cx,
    cy,
    r
  }) {
    return React.createElement("g", null, React.createElement("circle", {
      cx,
      cy,
      r,
      fill: "#2b2f36",
      stroke: "#15171b",
      strokeWidth: 1.5
    }), React.createElement("circle", {
      cx,
      cy,
      r: r * 0.45,
      fill: "#9aa0a8"
    }));
  }

  // End-effector glyphs, drawn at the wrist (wx, wy), pointing right.
  function Effector({
    type,
    wx,
    wy
  }) {
    const g = "#3d434c";
    if (type === "grip") return React.createElement("g", null, React.createElement("rect", {
      x: wx + 8,
      y: wy - 8,
      width: 12,
      height: 16,
      rx: 2,
      fill: g
    }), React.createElement("path", {
      d: `M${wx + 20},${wy - 8} h16 l4,5 h-20 Z`,
      fill: "#5e6570"
    }), React.createElement("path", {
      d: `M${wx + 20},${wy + 8} h16 l4,-5 h-20 Z`,
      fill: "#5e6570"
    }));
    if (type === "vac") return React.createElement("g", null, React.createElement("rect", {
      x: wx + 8,
      y: wy - 14,
      width: 8,
      height: 28,
      rx: 2,
      fill: g
    }), [-10, -3, 4, 11].map((dy, i) => React.createElement("g", {
      key: i
    }, React.createElement("line", {
      x1: wx + 16,
      y1: wy + dy + 1.5,
      x2: wx + 24,
      y2: wy + dy + 1.5,
      stroke: g,
      strokeWidth: 2
    }), React.createElement("path", {
      d: `M${wx + 24},${wy + dy - 2} q7,3.5 0,7 Z`,
      fill: "#5e6570"
    }))));
    if (type === "weld") return React.createElement("g", null, React.createElement("rect", {
      x: wx + 8,
      y: wy - 5,
      width: 14,
      height: 10,
      rx: 2,
      fill: g
    }), React.createElement("line", {
      x1: wx + 22,
      y1: wy,
      x2: wx + 38,
      y2: wy + 10,
      stroke: "#5e6570",
      strokeWidth: 5,
      strokeLinecap: "round"
    }), React.createElement("circle", {
      cx: wx + 41,
      cy: wy + 12,
      r: 3.5,
      fill: "#f1c40f"
    }), [[8, 2], [10, -4], [4, -8]].map(([dx, dy], i) => React.createElement("line", {
      key: i,
      x1: wx + 41,
      y1: wy + 12,
      x2: wx + 41 + dx,
      y2: wy + 12 + dy,
      stroke: "#e95b15",
      strokeWidth: 1.6,
      strokeLinecap: "round"
    })));
    // bare ISO flange
    return React.createElement("g", null, React.createElement("rect", {
      x: wx + 8,
      y: wy - 9,
      width: 6,
      height: 18,
      rx: 1.5,
      fill: g
    }), [-5, 0, 5].map((dy, i) => React.createElement("circle", {
      key: i,
      cx: wx + 11,
      cy: wy + dy,
      r: 1.3,
      fill: "#9aa0a8"
    })));
  }
  function SchematicRobot({
    color = "#e95b15",
    reach = 1300,
    effector = "none",
    badge = "10 KG",
    hasRail = false
  }) {
    // scale link lengths with reach (1300 mm = 1.0)
    const s = Math.max(0.8, Math.min(1.3, reach / 1300));
    const floorY = 210;
    const bx = hasRail ? 210 : 240; // base center x
    const j1 = {
      x: bx,
      y: 168
    }; // shoulder (axis 2/3 cluster)
    const L1 = 92 * s,
      L2 = 128 * s;
    const a1 = 64 * Math.PI / 180; // lower arm angle up-right
    const j2 = {
      x: j1.x + Math.cos(a1) * L1,
      y: j1.y - Math.sin(a1) * L1
    };
    const a2 = -14 * Math.PI / 180; // upper arm slightly downward
    const j3 = {
      x: j2.x + Math.cos(a2) * L2,
      y: j2.y - Math.sin(a2) * L2
    };
    const stroke = "rgba(0,0,0,0.25)";
    const badgeFill = badge === "20 KG" ? "#e95b15" : badge === "5 KG" ? "#3d3d3d" : "#529bde";
    const pedTop = j1.y + 14;
    return React.createElement("svg", {
      viewBox: "0 0 560 250",
      width: "100%",
      style: {
        display: "block"
      }
    },
    // ground shadow + floor
    React.createElement("ellipse", {
      cx: 280,
      cy: floorY + 16,
      rx: 210,
      ry: 11,
      fill: "rgba(33,33,33,0.10)"
    }), React.createElement("line", {
      x1: 60,
      y1: floorY + 14,
      x2: 500,
      y2: floorY + 14,
      stroke: "#d4d4d4",
      strokeWidth: 2
    }),
    // 7th-axis rail + carriage
    hasRail && React.createElement("g", null, React.createElement("rect", {
      x: 100,
      y: floorY + 2,
      width: 360,
      height: 9,
      rx: 2,
      fill: "#3d434c"
    }), [0, 1, 2, 3, 4, 5].map(i => React.createElement("line", {
      key: i,
      x1: 118 + i * 66,
      y1: floorY + 2,
      x2: 118 + i * 66,
      y2: floorY + 11,
      stroke: "#5e6570",
      strokeWidth: 2
    })), React.createElement("rect", {
      x: bx - 44,
      y: floorY - 8,
      width: 88,
      height: 12,
      rx: 3,
      fill: "#2b2f36"
    })),
    // pedestal
    React.createElement("path", {
      d: `M${bx - 42},${hasRail ? floorY - 8 : floorY + 2} L${bx + 42},${hasRail ? floorY - 8 : floorY + 2} L${bx + 26},${pedTop} L${bx - 26},${pedTop} Z`,
      fill: color,
      stroke,
      strokeWidth: 1.5,
      strokeLinejoin: "round"
    }), React.createElement("rect", {
      x: bx - 30,
      y: pedTop - 10,
      width: 60,
      height: 14,
      rx: 4,
      fill: color,
      stroke,
      strokeWidth: 1.2
    }),
    // arm links (drawn as thick rounded strokes in the body color)
    React.createElement("line", {
      x1: j1.x,
      y1: j1.y,
      x2: j2.x,
      y2: j2.y,
      stroke: color,
      strokeWidth: 26,
      strokeLinecap: "round"
    }), React.createElement("line", {
      x1: j1.x,
      y1: j1.y,
      x2: j2.x,
      y2: j2.y,
      stroke,
      strokeWidth: 26,
      strokeLinecap: "round",
      fill: "none",
      opacity: 0.18
    }), React.createElement("line", {
      x1: j2.x,
      y1: j2.y,
      x2: j3.x,
      y2: j3.y,
      stroke: color,
      strokeWidth: 19,
      strokeLinecap: "round"
    }),
    // joints
    React.createElement(Joint, {
      cx: j1.x,
      cy: j1.y,
      r: 17
    }), React.createElement(Joint, {
      cx: j2.x,
      cy: j2.y,
      r: 13
    }), React.createElement(Joint, {
      cx: j3.x,
      cy: j3.y,
      r: 9
    }),
    // end effector at the wrist
    React.createElement(Effector, {
      type: effector,
      wx: j3.x,
      wy: j3.y
    }),
    // payload badge
    React.createElement("g", null, React.createElement("rect", {
      x: 250,
      y: 228,
      width: 62,
      height: 20,
      rx: 10,
      fill: badgeFill
    }), React.createElement("text", {
      x: 281,
      y: 242,
      textAnchor: "middle",
      fontSize: 11,
      fontWeight: 700,
      fontFamily: "var(--font-mono)",
      fill: "#fff"
    }, badge)));
  }
  window.SchematicRobot = SchematicRobot;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/robot.js", error: String((e && e.message) || e) }); }

// prototypes/bom-configurator/tree.js
try { (() => {
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
  const {
    useState
  } = React;
  const DS = window.SequencerProDesignSystem_5eb90b;
  const C = window.CFG,
    CAR = window.CAR,
    fmt = C.fmt;
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
  function PartsList({
    bom
  }) {
    if (!(bom || []).length) return /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)",
        fontStyle: "italic",
        padding: "4px 2px 0"
      }
    }, "No parts added");
    return /*#__PURE__*/React.createElement("div", {
      style: {
        borderTop: "1px dashed var(--border-default)",
        marginTop: 6,
        paddingTop: 5,
        display: "flex",
        flexDirection: "column",
        gap: 2
      }
    }, bom.map(a => /*#__PURE__*/React.createElement(React.Fragment, {
      key: a.code
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 5
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 9.5,
        color: "var(--text-link)",
        width: 58,
        flex: "none",
        textAlign: "left"
      }
    }, a.code), /*#__PURE__*/React.createElement("span", {
      style: {
        fontSize: "var(--text-2xs)",
        flex: 1,
        textAlign: "left",
        minWidth: 0,
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis"
      }
    }, a.name), /*#__PURE__*/React.createElement("span", {
      title: a.qtyExpr || undefined,
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 9.5,
        fontWeight: 600,
        color: a.qtyExpr ? "var(--teal-700)" : "var(--text-muted)",
        flex: "none"
      }
    }, a.qtyExpr ? "ƒ" : "×" + a.qty)), (a.children || []).map(p => /*#__PURE__*/React.createElement("div", {
      key: p.code,
      style: {
        display: "flex",
        alignItems: "center",
        gap: 5,
        paddingLeft: 12
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: 8,
        borderTop: "1px solid var(--border-default)",
        flex: "none"
      }
    }), /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 9,
        color: "var(--text-muted)",
        width: 52,
        flex: "none",
        textAlign: "left"
      }
    }, p.code), /*#__PURE__*/React.createElement("span", {
      style: {
        fontSize: 9.5,
        color: "var(--text-secondary)",
        flex: 1,
        textAlign: "left",
        minWidth: 0,
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis"
      }
    }, p.name), /*#__PURE__*/React.createElement("span", {
      title: p.qtyExpr || undefined,
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 9,
        fontWeight: 600,
        color: p.qtyExpr ? "var(--teal-700)" : "var(--text-muted)",
        flex: "none"
      }
    }, p.qtyExpr ? "ƒ" : "×" + p.qty))))));
  }

  // A leaf option card on the diagram.
  function OptionNode({
    o,
    sel
  }) {
    const [open, setOpen] = useState(false);
    const isSel = C.isSel(sel, o.id);
    const rules = ruleBadges(o.id);
    const hasF = !!o.priceExpr || (o.bom || []).some(a => a.qtyExpr || (a.children || []).some(p => p.qtyExpr));
    return /*#__PURE__*/React.createElement("button", {
      onClick: () => setOpen(v => !v),
      style: {
        width: 168,
        textAlign: "center",
        cursor: "pointer",
        fontFamily: "var(--font-sans)",
        background: isSel ? "var(--gold-50)" : "var(--bg-surface)",
        border: isSel ? "2px solid var(--gold)" : "1px solid var(--border-strong)",
        borderRadius: "var(--radius)",
        padding: "7px 9px",
        boxShadow: "var(--shadow-xs)",
        position: "relative"
      }
    }, isSel && /*#__PURE__*/React.createElement("i", {
      className: "bi bi-check-circle-fill",
      title: "In current configuration",
      style: {
        position: "absolute",
        top: -7,
        right: -7,
        color: "var(--gold-600)",
        fontSize: 14,
        background: "var(--bg-surface)",
        borderRadius: "50%"
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        gap: 6
      }
    }, o.swatch && /*#__PURE__*/React.createElement("span", {
      style: {
        width: 10,
        height: 10,
        borderRadius: "50%",
        background: o.swatch,
        border: "1px solid rgba(0,0,0,0.12)",
        flex: "none"
      }
    }), /*#__PURE__*/React.createElement("span", {
      style: {
        fontSize: "var(--text-xs)",
        fontWeight: 700,
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis"
      }
    }, o.name)), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        gap: 6,
        marginTop: 3
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 9.5,
        fontWeight: 600,
        color: "var(--text-secondary)"
      }
    }, o.priceExpr ? "ƒ price" : o.price ? "+" + fmt(o.price) : "Incl."), hasF && /*#__PURE__*/React.createElement("span", {
      title: "Contains formula-driven quantities/prices",
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 9.5,
        fontWeight: 700,
        color: "var(--teal-700)"
      }
    }, "\u0192"), rules.length > 0 && /*#__PURE__*/React.createElement("span", {
      title: rules.map(r => r.id + ": " + r.msg).join("\n"),
      style: {
        fontSize: 9.5,
        color: "var(--blue-700)"
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-diagram-3"
    }), " ", rules.length), (o.ops || []).length > 0 && /*#__PURE__*/React.createElement("span", {
      title: o.ops.map(op => op.code + " " + op.name).join("\n"),
      style: {
        fontSize: 9.5,
        color: "var(--text-muted)"
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-signpost-split"
    }), " ", o.ops.length)), open && /*#__PURE__*/React.createElement(PartsList, {
      bom: o.bom
    }));
  }

  // Group node card.
  function GroupCard({
    g
  }) {
    return /*#__PURE__*/React.createElement("div", {
      style: {
        width: 168,
        textAlign: "center",
        background: "var(--slate)",
        color: "#fff",
        borderRadius: "var(--radius)",
        padding: "7px 9px",
        boxShadow: "var(--shadow-sm)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        gap: 6
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi " + (g.icon || "bi-dot"),
      style: {
        color: "var(--gold)",
        fontSize: 12
      }
    }), /*#__PURE__*/React.createElement("span", {
      style: {
        fontSize: "var(--text-xs)",
        fontWeight: 700
      }
    }, g.name)), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: 9,
        textTransform: "uppercase",
        letterSpacing: "0.06em",
        color: "var(--grey)",
        marginTop: 2
      }
    }, g.multi ? "multi-select" : g.required ? "pick one" : "optional", " \xB7 ", g.options.length));
  }

  // Base-platform branch: assemblies as one stacked card set.
  function BaseCard() {
    const [open, setOpen] = useState(false);
    return /*#__PURE__*/React.createElement("button", {
      onClick: () => setOpen(v => !v),
      style: {
        width: 168,
        textAlign: "center",
        cursor: "pointer",
        fontFamily: "var(--font-sans)",
        background: "var(--gold-50)",
        border: "2px solid var(--gold)",
        borderRadius: "var(--radius)",
        padding: "7px 9px",
        boxShadow: "var(--shadow-xs)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        fontWeight: 700
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-box",
      style: {
        color: "var(--gold-700)",
        marginRight: 5
      }
    }), "Every build"), /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 9.5,
        fontWeight: 600,
        color: "var(--text-secondary)",
        marginTop: 3
      }
    }, CAR.baseBom.length, " assemblies \xB7 ", fmt(CAR.platform.basePrice)), open && /*#__PURE__*/React.createElement(PartsList, {
      bom: CAR.baseBom
    }));
  }
  function ProductTree({
    sel
  }) {
    const totalOpts = CAR.groups.reduce((n, g) => n + g.options.length, 0);
    return /*#__PURE__*/React.createElement("div", {
      style: {
        height: "100%",
        overflow: "auto",
        background: "var(--bg-app)"
      }
    }, /*#__PURE__*/React.createElement("style", null, TREE_CSS), /*#__PURE__*/React.createElement("div", {
      style: {
        padding: "22px 26px 60px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        maxWidth: 860,
        margin: "0 auto 4px"
      }
    }, /*#__PURE__*/React.createElement("h2", {
      style: {
        fontSize: "var(--text-xl)",
        fontWeight: 700,
        margin: "0 0 4px",
        display: "flex",
        alignItems: "center",
        gap: 9,
        justifyContent: "center"
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-diagram-2",
      style: {
        color: "var(--gold-600)"
      }
    }), "Product tree"), /*#__PURE__*/React.createElement("p", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)",
        margin: "0 0 18px",
        lineHeight: 1.5,
        textAlign: "center"
      }
    }, "The full \"150% BoM\" \u2014 everything the platform ", /*#__PURE__*/React.createElement("em", null, "can"), " be. Click any card to see the parts it adds. Gold ring = in the current configuration \xB7 ", /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)",
        color: "var(--teal-700)"
      }
    }, "\u0192"), " = formula-driven \xB7", " ", /*#__PURE__*/React.createElement("i", {
      className: "bi bi-diagram-3",
      style: {
        color: "var(--blue-700)"
      }
    }), " = rules (hover) \xB7 ", /*#__PURE__*/React.createElement("i", {
      className: "bi bi-signpost-split"
    }), " = routing ops.")), /*#__PURE__*/React.createElement("div", {
      className: "pt-chart"
    }, /*#__PURE__*/React.createElement("ul", null, /*#__PURE__*/React.createElement("li", null, /*#__PURE__*/React.createElement("div", {
      style: {
        width: 200,
        textAlign: "center",
        background: "var(--bg-surface)",
        border: "2px solid var(--slate)",
        borderRadius: "var(--radius-md)",
        padding: "10px 12px",
        boxShadow: "var(--shadow-sm)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "var(--font-display)",
        fontWeight: 800,
        fontSize: "var(--text-md)"
      }
    }, CAR.platform.name), /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: 9.5,
        color: "var(--text-muted)",
        marginTop: 1
      }
    }, CAR.platform.code), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: 9.5,
        color: "var(--text-secondary)",
        marginTop: 3
      }
    }, CAR.groups.length, " groups \xB7 ", totalOpts, " options \xB7 ", CAR.rules.length, " rules")), /*#__PURE__*/React.createElement("ul", null, /*#__PURE__*/React.createElement("li", null, /*#__PURE__*/React.createElement(BaseCard, null)), CAR.groups.map(g => /*#__PURE__*/React.createElement("li", {
      key: g.id
    }, /*#__PURE__*/React.createElement(GroupCard, {
      g: g
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 0,
        marginTop: 0
      }
    }, g.options.map(o => /*#__PURE__*/React.createElement("div", {
      key: o.id,
      style: {
        display: "flex",
        flexDirection: "column",
        alignItems: "center"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: 0,
        height: 14,
        borderLeft: "1.5px solid var(--border-strong)"
      }
    }), /*#__PURE__*/React.createElement(OptionNode, {
      o: o,
      sel: sel
    }))))))))))));
  }
  window.ProductTree = ProductTree;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "prototypes/bom-configurator/tree.js", error: String((e && e.message) || e) }); }

// ui_kits/process-manager/data.js
try { (() => {
// SequencerPro Process Manager — seed data lifted from the API DataSeeder.
// Domain-neutral internals mapped to "General Manufacturing" vocabulary.
window.PM_DATA = {
  vocab: {
    kind: "Part",
    grade: "Disposition",
    item: "Unit",
    batch: "Lot",
    job: "Work Order",
    workflow: "Value Stream",
    process: "Process",
    step: "Operation"
  },
  kinds: [{
    code: "WDG-100",
    name: "Widget",
    desc: "Standard machined widget component.",
    serialized: true,
    batchable: true,
    grades: [{
      code: "NEW",
      name: "New",
      def: true
    }, {
      code: "A",
      name: "Grade A"
    }, {
      code: "B",
      name: "Grade B"
    }, {
      code: "SCRAP",
      name: "Scrap"
    }]
  }, {
    code: "PCB-200",
    name: "PCB Assembly",
    desc: "Populated printed circuit board assembly.",
    serialized: true,
    batchable: false,
    grades: [{
      code: "UNGRD",
      name: "Ungraded",
      def: true
    }, {
      code: "PASS",
      name: "Pass"
    }, {
      code: "FAIL",
      name: "Fail"
    }, {
      code: "RWRK",
      name: "Rework"
    }]
  }, {
    code: "CMP-300",
    name: "Compound",
    desc: "Bulk chemical compound used in coating operations.",
    serialized: false,
    batchable: true,
    grades: [{
      code: "STD",
      name: "Standard",
      def: true
    }, {
      code: "PREM",
      name: "Premium"
    }, {
      code: "REJ",
      name: "Reject"
    }]
  }],
  steps: [{
    code: "INSP-01",
    name: "Incoming Inspection",
    pattern: "Transform",
    version: 1,
    inP: [{
      t: "Material",
      l: "Received widget"
    }, {
      t: "Condition",
      l: "Docs present"
    }],
    outP: [{
      t: "Material",
      l: "Verified widget"
    }, {
      t: "Characteristic",
      l: "Surface finish Ra"
    }]
  }, {
    code: "MACH-01",
    name: "CNC Machining",
    pattern: "Transform",
    version: 2,
    inP: [{
      t: "Material",
      l: "Raw widget"
    }, {
      t: "Parameter",
      l: "Spindle RPM"
    }, {
      t: "Condition",
      l: "Tool offset OK"
    }],
    outP: [{
      t: "Material",
      l: "Machined widget"
    }, {
      t: "Characteristic",
      l: "Hole Ø"
    }]
  }, {
    code: "ASSY-01",
    name: "Sub-Assembly",
    pattern: "Assembly",
    version: 1,
    inP: [{
      t: "Material",
      l: "Board"
    }, {
      t: "Material",
      l: "Housing"
    }],
    outP: [{
      t: "Material",
      l: "Sub-assembly"
    }]
  }, {
    code: "TEST-01",
    name: "Functional Test",
    pattern: "Transform",
    version: 3,
    inP: [{
      t: "Material",
      l: "Assembly"
    }, {
      t: "Parameter",
      l: "Test voltage"
    }],
    outP: [{
      t: "Material",
      l: "Tested unit"
    }, {
      t: "Condition",
      l: "Pass / Fail"
    }]
  }, {
    code: "INSP-02",
    name: "Visual Inspection",
    pattern: "Transform",
    version: 1,
    inP: [{
      t: "Material",
      l: "Machined widget"
    }],
    outP: [{
      t: "Material",
      l: "Inspected widget"
    }, {
      t: "Condition",
      l: "Cosmetic OK"
    }]
  }, {
    code: "PACK-01",
    name: "Packaging",
    pattern: "General",
    version: 1,
    inP: [{
      t: "Material",
      l: "Inspected widget"
    }, {
      t: "Material",
      l: "Carton"
    }],
    outP: [{
      t: "Material",
      l: "Packed unit"
    }]
  }],
  processes: [{
    code: "WDG-MFG-01",
    name: "Widget Manufacturing",
    version: 1,
    active: true,
    desc: "Full manufacturing flow for standard widgets from incoming goods to packaged output.",
    stepCodes: ["INSP-01", "MACH-01", "INSP-02", "PACK-01"],
    jobs: 3
  }, {
    code: "PCB-ASSY-01",
    name: "PCB Assembly & Test",
    version: 1,
    active: true,
    desc: "SMT population, sub-assembly, and functional test for PCB assemblies.",
    stepCodes: ["INSP-01", "ASSY-01", "TEST-01"],
    jobs: 2
  }],
  workflows: [{
    code: "WDG-VS-01",
    name: "Widget Value Stream",
    version: 2,
    active: true,
    processes: 2,
    links: 3
  }, {
    code: "PCB-BUILD-01",
    name: "PCB Build Plan",
    version: 1,
    active: true,
    processes: 1,
    links: 0
  }],
  jobs: [{
    code: "WO-2026-001",
    name: "Widget Batch Run — Sprint 1",
    process: "WDG-MFG-01",
    status: "done",
    priority: 3,
    units: 5,
    started: "20d ago",
    done: "20d ago"
  }, {
    code: "WO-2026-002",
    name: "Widget Batch Run — Sprint 2",
    process: "WDG-MFG-01",
    status: "active",
    priority: 5,
    units: 5,
    started: "5d ago",
    done: null
  }, {
    code: "PO-2026-003",
    name: "PCB Assembly — Rev B Boards",
    process: "PCB-ASSY-01",
    status: "pending",
    priority: 2,
    units: 4,
    started: null,
    done: null
  }, {
    code: "WO-2026-004",
    name: "Widget Run — Customer Hold",
    process: "WDG-MFG-01",
    status: "hold",
    priority: 1,
    units: 3,
    started: "14d ago",
    done: null
  }, {
    code: "PO-2026-005",
    name: "PCB Assembly — Rev A Boards",
    process: "PCB-ASSY-01",
    status: "done",
    priority: 3,
    units: 4,
    started: "58d ago",
    done: "50d ago"
  }, {
    code: "WO-2026-006",
    name: "Widget Run — Cancelled",
    process: "WDG-MFG-01",
    status: "fail",
    priority: 2,
    units: 0,
    started: null,
    done: null
  }],
  // Step-execution timeline for WO-2026-002 (the active job)
  execution: {
    job: "WO-2026-002",
    steps: [{
      seq: 1,
      step: "Incoming Inspection",
      code: "INSP-01",
      status: "done",
      at: "5d ago",
      op: "M. Reyes"
    }, {
      seq: 2,
      step: "CNC Machining",
      code: "MACH-01",
      status: "active",
      at: "3d ago",
      op: "J. Okafor"
    }, {
      seq: 3,
      step: "Visual Inspection",
      code: "INSP-02",
      status: "pending",
      at: null,
      op: null
    }, {
      seq: 4,
      step: "Packaging",
      code: "PACK-01",
      status: "pending",
      at: null,
      op: null
    }],
    units: [{
      sn: "WDG-0006",
      grade: "NEW",
      state: "active"
    }, {
      sn: "WDG-0007",
      grade: "NEW",
      state: "active"
    }, {
      sn: "WDG-0008",
      grade: "NEW",
      state: "active"
    }, {
      sn: "WDG-0009",
      grade: "NEW",
      state: "pending"
    }, {
      sn: "WDG-0010",
      grade: "NEW",
      state: "pending"
    }]
  }
};
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/process-manager/data.js", error: String((e && e.message) || e) }); }

// ui_kits/process-manager/screens.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
// Process Manager — screens. Composes DS primitives + PM_DATA.
(() => {
  const {
    useState: useS
  } = React;
  const DS = window.SequencerProDesignSystem_5eb90b;
  const {
    Button,
    IconButton,
    StatusBadge,
    Tag,
    Card,
    StatCard,
    PortBadge
  } = DS;
  const D = window.PM_DATA;
  const JOB_TONE = {
    done: "done",
    active: "active",
    pending: "pending",
    hold: "hold",
    fail: "fail"
  };
  const JOB_LABEL = {
    done: "Completed",
    active: "In Progress",
    pending: "Created",
    hold: "On Hold",
    fail: "Cancelled"
  };

  // ---- shared table shell -------------------------------------------------
  function Table({
    cols,
    children
  }) {
    return /*#__PURE__*/React.createElement("table", {
      style: {
        width: "100%",
        borderCollapse: "collapse",
        fontSize: "var(--text-sm)"
      }
    }, /*#__PURE__*/React.createElement("thead", null, /*#__PURE__*/React.createElement("tr", null, cols.map((c, i) => /*#__PURE__*/React.createElement("th", {
      key: i,
      style: {
        textAlign: c.right ? "right" : "left",
        padding: "9px 14px",
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        letterSpacing: "var(--ls-caps)",
        textTransform: "uppercase",
        color: "var(--text-muted)",
        borderBottom: "1px solid var(--border-strong)",
        whiteSpace: "nowrap"
      }
    }, c.label)))), /*#__PURE__*/React.createElement("tbody", null, children));
  }
  const Td = ({
    children,
    right,
    mono,
    ...p
  }) => /*#__PURE__*/React.createElement("td", _extends({}, p, {
    style: {
      padding: "10px 14px",
      borderBottom: "1px solid var(--border-default)",
      textAlign: right ? "right" : "left",
      fontFamily: mono ? "var(--font-mono)" : "inherit",
      color: "var(--text-primary)",
      verticalAlign: "middle",
      ...(p.style || {})
    }
  }), children);
  const Row = ({
    children,
    onClick
  }) => /*#__PURE__*/React.createElement("tr", {
    onClick: onClick,
    style: {
      cursor: onClick ? "pointer" : "default",
      transition: "background var(--dur)"
    },
    onMouseEnter: e => onClick && (e.currentTarget.style.background = "var(--grey-100)"),
    onMouseLeave: e => e.currentTarget.style.background = "transparent"
  }, children);
  const PageWrap = ({
    children
  }) => /*#__PURE__*/React.createElement("div", {
    style: {
      padding: 22,
      display: "flex",
      flexDirection: "column",
      gap: 18
    }
  }, children);

  // ---- Dashboard ----------------------------------------------------------
  function Dashboard({
    onOpenJob
  }) {
    return /*#__PURE__*/React.createElement(PageWrap, null, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "repeat(4,1fr)",
        gap: 14
      }
    }, /*#__PURE__*/React.createElement(StatCard, {
      label: "Active work orders",
      value: "2",
      delta: "+1 this week",
      deltaTone: "up",
      icon: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-briefcase"
      })
    }), /*#__PURE__*/React.createElement(StatCard, {
      label: "Units in process",
      value: "8",
      accent: "var(--blue)",
      icon: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-box-seam"
      })
    }), /*#__PURE__*/React.createElement(StatCard, {
      label: "First-pass yield",
      value: "96.4",
      unit: "%",
      delta: "-0.8 pt",
      deltaTone: "down",
      accent: "var(--teal)",
      icon: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-check2-circle"
      })
    }), /*#__PURE__*/React.createElement(StatCard, {
      label: "On hold",
      value: "1",
      accent: "var(--orange)",
      icon: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-pause-circle"
      })
    })), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "1.6fr 1fr",
        gap: 16,
        alignItems: "start"
      }
    }, /*#__PURE__*/React.createElement(Card, {
      title: "Work orders in flight",
      subtitle: "Live job status across all value streams",
      actions: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "secondary",
        iconLeft: /*#__PURE__*/React.createElement("i", {
          className: "bi bi-funnel"
        })
      }, "Filter"),
      padding: "0"
    }, /*#__PURE__*/React.createElement(Table, {
      cols: [{
        label: "Work order"
      }, {
        label: "Process"
      }, {
        label: "Status"
      }, {
        label: "Units",
        right: true
      }]
    }, D.jobs.filter(j => j.status !== "fail").slice(0, 5).map(j => /*#__PURE__*/React.createElement(Row, {
      key: j.code,
      onClick: () => onOpenJob(j)
    }, /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "var(--font-mono)",
        fontWeight: 600,
        fontSize: "var(--text-xs)",
        color: "var(--text-link)"
      }
    }, j.code), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)",
        marginTop: 1
      }
    }, j.name)), /*#__PURE__*/React.createElement(Td, {
      mono: true,
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)"
      }
    }, j.process), /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement(StatusBadge, {
      tone: JOB_TONE[j.status]
    }, JOB_LABEL[j.status])), /*#__PURE__*/React.createElement(Td, {
      right: true,
      mono: true
    }, j.units))))), /*#__PURE__*/React.createElement(Card, {
      title: "Widget Manufacturing",
      subtitle: "WDG-MFG-01 \xB7 process flow",
      accent: "var(--gold)"
    }, /*#__PURE__*/React.createElement(ProcessFlow, {
      compact: true,
      stepCodes: D.processes[0].stepCodes
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: 14,
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)",
        lineHeight: 1.5
      }
    }, "4 operations \xB7 type-checked ports ensure the wrong part can't flow to the wrong station."))));
  }

  // ---- Process flow strip -------------------------------------------------
  function ProcessFlow({
    stepCodes,
    compact
  }) {
    const steps = stepCodes.map(c => D.steps.find(s => s.code === c));
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: compact ? "column" : "row",
        gap: compact ? 8 : 0,
        alignItems: "stretch"
      }
    }, steps.map((s, i) => /*#__PURE__*/React.createElement(React.Fragment, {
      key: s.code
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1,
        background: "var(--bg-surface)",
        border: "var(--border)",
        borderRadius: "var(--radius)",
        padding: "10px 12px",
        minWidth: 0
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 8,
        marginBottom: compact ? 0 : 8
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: 20,
        height: 20,
        borderRadius: "50%",
        background: "var(--slate)",
        color: "#fff",
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        flex: "none"
      }
    }, i + 1), /*#__PURE__*/React.createElement("div", {
      style: {
        minWidth: 0
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-sm)",
        fontWeight: 600,
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis"
      }
    }, s.name), /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "var(--font-mono)",
        fontSize: "10px",
        color: "var(--text-muted)"
      }
    }, s.code, " \xB7 ", s.pattern))), !compact && /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexWrap: "wrap",
        gap: 5
      }
    }, s.inP.map((p, k) => /*#__PURE__*/React.createElement(PortBadge, {
      key: "i" + k,
      type: p.t,
      direction: "in",
      label: p.l
    })), s.outP.map((p, k) => /*#__PURE__*/React.createElement(PortBadge, {
      key: "o" + k,
      type: p.t,
      direction: "out",
      label: p.l
    })))), i < steps.length - 1 && /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        color: "var(--grey)",
        padding: compact ? "0" : "0 6px",
        transform: compact ? "rotate(90deg)" : "none"
      }
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-arrow-right",
      style: {
        fontSize: 16
      }
    })))));
  }

  // ---- Processes list + detail -------------------------------------------
  function Processes({
    onOpen
  }) {
    return /*#__PURE__*/React.createElement(PageWrap, null, /*#__PURE__*/React.createElement(Card, {
      padding: "0",
      title: "Processes",
      subtitle: "Linear operation sequences \u2014 routings",
      actions: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        iconLeft: /*#__PURE__*/React.createElement("i", {
          className: "bi bi-plus-lg"
        })
      }, "New process")
    }, /*#__PURE__*/React.createElement(Table, {
      cols: [{
        label: "Code"
      }, {
        label: "Name"
      }, {
        label: "Operations"
      }, {
        label: "Version"
      }, {
        label: "Status"
      }, {
        label: ""
      }]
    }, D.processes.map(p => /*#__PURE__*/React.createElement(Row, {
      key: p.code,
      onClick: () => onOpen(p)
    }, /*#__PURE__*/React.createElement(Td, {
      mono: true,
      style: {
        fontWeight: 600,
        color: "var(--text-link)",
        fontSize: "var(--text-xs)"
      }
    }, p.code), /*#__PURE__*/React.createElement(Td, null, p.name), /*#__PURE__*/React.createElement(Td, null, p.stepCodes.length), /*#__PURE__*/React.createElement(Td, {
      mono: true,
      style: {
        color: "var(--text-secondary)"
      }
    }, "v", p.version), /*#__PURE__*/React.createElement(Td, null, p.active ? /*#__PURE__*/React.createElement(StatusBadge, {
      tone: "pass",
      dot: false
    }, "Active") : /*#__PURE__*/React.createElement(StatusBadge, {
      tone: "pending",
      dot: false
    }, "Draft")), /*#__PURE__*/React.createElement(Td, {
      right: true
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-chevron-right",
      style: {
        color: "var(--text-muted)"
      }
    })))))));
  }
  function ProcessDetail({
    proc
  }) {
    return /*#__PURE__*/React.createElement(PageWrap, null, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 12,
        flexWrap: "wrap"
      }
    }, /*#__PURE__*/React.createElement(Tag, {
      color: "blue"
    }, proc.code), /*#__PURE__*/React.createElement(Tag, {
      color: "neutral"
    }, "v", proc.version), /*#__PURE__*/React.createElement(StatusBadge, {
      tone: "pass",
      dot: false
    }, "Active"), /*#__PURE__*/React.createElement("span", {
      style: {
        color: "var(--text-secondary)",
        fontSize: "var(--text-sm)",
        alignSelf: "center"
      }
    }, proc.desc)), /*#__PURE__*/React.createElement(Card, {
      title: "Operation sequence",
      subtitle: `${proc.stepCodes.length} operations · ports validated step-to-step`,
      actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "secondary",
        iconLeft: /*#__PURE__*/React.createElement("i", {
          className: "bi bi-check2-circle"
        })
      }, "Validate"), /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        iconLeft: /*#__PURE__*/React.createElement("i", {
          className: "bi bi-pencil"
        })
      }, "Edit"))
    }, /*#__PURE__*/React.createElement(ProcessFlow, {
      stepCodes: proc.stepCodes
    })));
  }

  // ---- Jobs list + detail -------------------------------------------------
  function Jobs({
    onOpen
  }) {
    const [filter, setFilter] = useS("all");
    const tabs = [["all", "All"], ["active", "In Progress"], ["hold", "On Hold"], ["done", "Completed"]];
    const rows = D.jobs.filter(j => filter === "all" ? true : j.status === filter);
    return /*#__PURE__*/React.createElement(PageWrap, null, /*#__PURE__*/React.createElement(Card, {
      padding: "0",
      title: "Work Orders",
      subtitle: "Jobs driving units through value streams",
      actions: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "accent",
        iconLeft: /*#__PURE__*/React.createElement("i", {
          className: "bi bi-plus-lg"
        })
      }, "New work order")
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 4,
        padding: "10px 14px",
        borderBottom: "1px solid var(--border-default)"
      }
    }, tabs.map(([id, l]) => /*#__PURE__*/React.createElement("button", {
      key: id,
      onClick: () => setFilter(id),
      style: {
        padding: "5px 12px",
        borderRadius: "var(--radius)",
        border: "none",
        cursor: "pointer",
        fontSize: "var(--text-xs)",
        fontWeight: 600,
        fontFamily: "var(--font-sans)",
        background: filter === id ? "var(--slate)" : "transparent",
        color: filter === id ? "var(--white)" : "var(--text-secondary)"
      }
    }, l))), /*#__PURE__*/React.createElement(Table, {
      cols: [{
        label: "Work order"
      }, {
        label: "Process"
      }, {
        label: "Priority"
      }, {
        label: "Status"
      }, {
        label: "Units",
        right: true
      }, {
        label: "Started"
      }]
    }, rows.map(j => /*#__PURE__*/React.createElement(Row, {
      key: j.code,
      onClick: () => onOpen(j)
    }, /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "var(--font-mono)",
        fontWeight: 600,
        fontSize: "var(--text-xs)",
        color: "var(--text-link)"
      }
    }, j.code), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)",
        marginTop: 1
      }
    }, j.name)), /*#__PURE__*/React.createElement(Td, {
      mono: true,
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)"
      }
    }, j.process), /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement(PriorityDots, {
      p: j.priority
    })), /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement(StatusBadge, {
      tone: JOB_TONE[j.status]
    }, JOB_LABEL[j.status])), /*#__PURE__*/React.createElement(Td, {
      right: true,
      mono: true
    }, j.units), /*#__PURE__*/React.createElement(Td, {
      style: {
        color: "var(--text-secondary)",
        fontSize: "var(--text-xs)"
      }
    }, j.started || "—"))))));
  }
  function PriorityDots({
    p
  }) {
    return /*#__PURE__*/React.createElement("span", {
      style: {
        display: "inline-flex",
        gap: 3
      }
    }, [1, 2, 3, 4, 5].map(i => /*#__PURE__*/React.createElement("span", {
      key: i,
      style: {
        width: 6,
        height: 6,
        borderRadius: "50%",
        background: i <= p ? "var(--orange)" : "var(--grey-200)"
      }
    })));
  }
  function JobDetail({
    job
  }) {
    const ex = D.execution;
    const isThis = job.code === ex.job;
    return /*#__PURE__*/React.createElement(PageWrap, null, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 12,
        flexWrap: "wrap",
        alignItems: "center"
      }
    }, /*#__PURE__*/React.createElement(Tag, {
      color: "gold"
    }, job.code), /*#__PURE__*/React.createElement(StatusBadge, {
      tone: JOB_TONE[job.status]
    }, JOB_LABEL[job.status]), /*#__PURE__*/React.createElement(Tag, {
      color: "blue"
    }, job.process), /*#__PURE__*/React.createElement("span", {
      style: {
        color: "var(--text-secondary)",
        fontSize: "var(--text-sm)"
      }
    }, job.name), /*#__PURE__*/React.createElement("div", {
      style: {
        marginLeft: "auto",
        display: "flex",
        gap: 6
      }
    }, /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary"
    }, "On hold"), /*#__PURE__*/React.createElement(Button, {
      size: "sm"
    }, "Advance step"))), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "1.4fr 1fr",
        gap: 16,
        alignItems: "start"
      }
    }, /*#__PURE__*/React.createElement(Card, {
      title: "Step execution",
      subtitle: "Operation timeline for this work order",
      padding: "0"
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        padding: "6px 0"
      }
    }, (isThis ? ex.steps : [{
      seq: 1,
      step: "Created",
      code: job.process,
      status: "pending",
      op: null,
      at: null
    }]).map((s, i, arr) => /*#__PURE__*/React.createElement("div", {
      key: s.seq,
      style: {
        display: "flex",
        gap: 12,
        padding: "11px 18px",
        position: "relative"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        flex: "none"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: 24,
        height: 24,
        borderRadius: "50%",
        flex: "none",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        background: s.status === "done" ? "var(--teal)" : s.status === "active" ? "var(--blue)" : "var(--grey-200)",
        color: s.status === "pending" ? "var(--ink-500)" : "#fff",
        fontSize: "11px",
        fontWeight: 700
      }
    }, s.status === "done" ? /*#__PURE__*/React.createElement("i", {
      className: "bi bi-check-lg"
    }) : s.seq), i < arr.length - 1 && /*#__PURE__*/React.createElement("span", {
      style: {
        width: 2,
        flex: 1,
        minHeight: 14,
        background: "var(--border-strong)",
        marginTop: 4
      }
    })), /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1,
        paddingBottom: 4
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 8
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontWeight: 600,
        fontSize: "var(--text-sm)"
      }
    }, s.step), /*#__PURE__*/React.createElement(StatusBadge, {
      tone: s.status === "done" ? "pass" : s.status === "active" ? "active" : "pending",
      dot: false
    }, s.status === "done" ? "Done" : s.status === "active" ? "Running" : "Pending")), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)",
        marginTop: 2
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "var(--font-mono)"
      }
    }, s.code), s.op ? ` · ${s.op}` : "", s.at ? ` · ${s.at}` : "")))))), /*#__PURE__*/React.createElement(Card, {
      title: "Units",
      subtitle: `${(isThis ? ex.units : []).length || job.units} units in this work order`,
      padding: "0"
    }, isThis ? /*#__PURE__*/React.createElement(Table, {
      cols: [{
        label: "Serial"
      }, {
        label: "Disposition"
      }, {
        label: "State"
      }]
    }, ex.units.map(u => /*#__PURE__*/React.createElement(Row, {
      key: u.sn
    }, /*#__PURE__*/React.createElement(Td, {
      mono: true,
      style: {
        fontSize: "var(--text-xs)"
      }
    }, u.sn), /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement(Tag, {
      color: "neutral"
    }, u.grade)), /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement(StatusBadge, {
      tone: u.state === "active" ? "active" : "pending",
      dot: false
    }, u.state === "active" ? "In process" : "Available"))))) : /*#__PURE__*/React.createElement("div", {
      style: {
        padding: 22,
        color: "var(--text-muted)",
        fontSize: "var(--text-sm)",
        textAlign: "center"
      }
    }, "No units recorded yet."))));
  }

  // ---- Catalog (Kinds) ----------------------------------------------------
  function Catalog() {
    return /*#__PURE__*/React.createElement(PageWrap, null, /*#__PURE__*/React.createElement(Card, {
      padding: "0",
      title: "Part Catalog",
      subtitle: "Kinds & dispositions \u2014 the type system",
      actions: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        iconLeft: /*#__PURE__*/React.createElement("i", {
          className: "bi bi-plus-lg"
        })
      }, "New part")
    }, /*#__PURE__*/React.createElement(Table, {
      cols: [{
        label: "Code"
      }, {
        label: "Name"
      }, {
        label: "Dispositions"
      }, {
        label: "Tracking"
      }]
    }, D.kinds.map(k => /*#__PURE__*/React.createElement(Row, {
      key: k.code
    }, /*#__PURE__*/React.createElement(Td, {
      mono: true,
      style: {
        fontWeight: 600,
        color: "var(--text-link)",
        fontSize: "var(--text-xs)"
      }
    }, k.code), /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement("div", {
      style: {
        fontWeight: 600
      }
    }, k.name), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--text-secondary)"
      }
    }, k.desc)), /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 5,
        flexWrap: "wrap"
      }
    }, k.grades.map(g => /*#__PURE__*/React.createElement(Tag, {
      key: g.code,
      color: g.def ? "teal" : "neutral"
    }, g.name)))), /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 5
      }
    }, k.serialized && /*#__PURE__*/React.createElement(Tag, {
      color: "blue",
      mono: false
    }, "Serialized"), k.batchable && /*#__PURE__*/React.createElement(Tag, {
      color: "gold",
      mono: false
    }, "Batchable"))))))));
  }

  // ---- Operations (Step templates) ---------------------------------------
  function Steps() {
    return /*#__PURE__*/React.createElement(PageWrap, null, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "repeat(2,1fr)",
        gap: 14
      }
    }, D.steps.map(s => /*#__PURE__*/React.createElement(Card, {
      key: s.code,
      title: s.name,
      subtitle: `${s.code} · ${s.pattern} · v${s.version}`,
      accent: s.pattern === "Assembly" ? "var(--teal)" : s.pattern === "General" ? "var(--orange)" : "var(--blue)"
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        letterSpacing: "var(--ls-caps)",
        textTransform: "uppercase",
        color: "var(--text-muted)",
        marginBottom: 6
      }
    }, "Inputs"), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexWrap: "wrap",
        gap: 5,
        marginBottom: 12
      }
    }, s.inP.map((p, k) => /*#__PURE__*/React.createElement(PortBadge, {
      key: k,
      type: p.t,
      direction: "in",
      label: p.l
    }))), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        letterSpacing: "var(--ls-caps)",
        textTransform: "uppercase",
        color: "var(--text-muted)",
        marginBottom: 6
      }
    }, "Outputs"), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexWrap: "wrap",
        gap: 5
      }
    }, s.outP.map((p, k) => /*#__PURE__*/React.createElement(PortBadge, {
      key: k,
      type: p.t,
      direction: "out",
      label: p.l
    })))))));
  }

  // ---- Workflows ----------------------------------------------------------
  function Workflows() {
    return /*#__PURE__*/React.createElement(PageWrap, null, /*#__PURE__*/React.createElement(Card, {
      padding: "0",
      title: "Workflows",
      subtitle: "Directed graphs of processes with routing decisions",
      actions: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        iconLeft: /*#__PURE__*/React.createElement("i", {
          className: "bi bi-plus-lg"
        })
      }, "New workflow")
    }, /*#__PURE__*/React.createElement(Table, {
      cols: [{
        label: "Code"
      }, {
        label: "Name"
      }, {
        label: "Processes"
      }, {
        label: "Routing links"
      }, {
        label: "Version"
      }, {
        label: "Status"
      }]
    }, D.workflows.map(w => /*#__PURE__*/React.createElement(Row, {
      key: w.code
    }, /*#__PURE__*/React.createElement(Td, {
      mono: true,
      style: {
        fontWeight: 600,
        color: "var(--text-link)",
        fontSize: "var(--text-xs)"
      }
    }, w.code), /*#__PURE__*/React.createElement(Td, null, w.name), /*#__PURE__*/React.createElement(Td, null, w.processes), /*#__PURE__*/React.createElement(Td, null, w.links), /*#__PURE__*/React.createElement(Td, {
      mono: true,
      style: {
        color: "var(--text-secondary)"
      }
    }, "v", w.version), /*#__PURE__*/React.createElement(Td, null, /*#__PURE__*/React.createElement(StatusBadge, {
      tone: "pass",
      dot: false
    }, "Active")))))));
  }
  Object.assign(window, {
    Dashboard,
    Processes,
    ProcessDetail,
    Jobs,
    JobDetail,
    Catalog,
    Steps,
    Workflows
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/process-manager/screens.jsx", error: String((e && e.message) || e) }); }

// ui_kits/process-manager/shell.jsx
try { (() => {
// Process Manager — app chrome (sidebar + topbar). Exports to window.
(() => {
  const {
    useState
  } = React;
  const {
    Wordmark,
    NodeMark,
    Button,
    IconButton,
    Input
  } = window.SequencerProDesignSystem_5eb90b;
  const NAV = [{
    id: "dashboard",
    label: "Dashboard",
    icon: "bi-speedometer2"
  }, {
    id: "processes",
    label: "Processes",
    icon: "bi-diagram-3"
  }, {
    id: "workflows",
    label: "Workflows",
    icon: "bi-bezier2"
  }, {
    id: "jobs",
    label: "Work Orders",
    icon: "bi-briefcase"
  }, {
    id: "catalog",
    label: "Part Catalog",
    icon: "bi-box-seam"
  }];
  const NAV2 = [{
    id: "steps",
    label: "Operations",
    icon: "bi-sliders"
  }, {
    id: "vocab",
    label: "Vocabulary",
    icon: "bi-translate"
  }];
  function Sidebar({
    active,
    onNav
  }) {
    const item = n => {
      const on = active === n.id;
      return /*#__PURE__*/React.createElement("button", {
        key: n.id,
        onClick: () => onNav(n.id),
        style: {
          display: "flex",
          alignItems: "center",
          gap: 11,
          width: "100%",
          padding: "9px 12px",
          borderRadius: "var(--radius)",
          border: "none",
          cursor: "pointer",
          background: on ? "rgba(241,196,15,0.14)" : "transparent",
          color: on ? "var(--white)" : "var(--grey)",
          fontFamily: "var(--font-sans)",
          fontSize: "var(--text-sm)",
          fontWeight: on ? "var(--fw-semibold)" : "var(--fw-medium)",
          textAlign: "left",
          position: "relative",
          transition: "background var(--dur), color var(--dur)"
        },
        onMouseEnter: e => {
          if (!on) e.currentTarget.style.color = "var(--white)";
        },
        onMouseLeave: e => {
          if (!on) e.currentTarget.style.color = "var(--grey)";
        }
      }, on && /*#__PURE__*/React.createElement("span", {
        style: {
          position: "absolute",
          left: 0,
          top: 8,
          bottom: 8,
          width: 3,
          borderRadius: 3,
          background: "var(--gold)"
        }
      }), /*#__PURE__*/React.createElement("i", {
        className: `bi ${n.icon}`,
        style: {
          fontSize: 16,
          color: on ? "var(--gold)" : "inherit"
        }
      }), n.label);
    };
    return /*#__PURE__*/React.createElement("aside", {
      style: {
        width: "var(--sidebar-w)",
        flex: "none",
        background: "var(--slate)",
        display: "flex",
        flexDirection: "column",
        height: "100%",
        padding: "14px 12px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: 9,
        padding: "4px 8px 16px"
      }
    }, /*#__PURE__*/React.createElement(NodeMark, {
      size: 34
    }), /*#__PURE__*/React.createElement(Wordmark, {
      size: 19,
      color: "var(--gold)"
    })), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: 2
      }
    }, NAV.map(item)), /*#__PURE__*/React.createElement("div", {
      style: {
        height: 1,
        background: "rgba(255,255,255,0.08)",
        margin: "12px 8px"
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        fontWeight: 700,
        letterSpacing: "var(--ls-caps)",
        textTransform: "uppercase",
        color: "var(--ink-500)",
        padding: "0 12px 8px"
      }
    }, "Design"), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: 2
      }
    }, NAV2.map(item)), /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: "auto",
        display: "flex",
        alignItems: "center",
        gap: 9,
        padding: "10px 8px",
        borderTop: "1px solid rgba(255,255,255,0.08)"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: 30,
        height: 30,
        borderRadius: "50%",
        background: "var(--teal)",
        color: "#fff",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: "var(--text-xs)",
        fontWeight: 700,
        flex: "none"
      }
    }, "EM"), /*#__PURE__*/React.createElement("div", {
      style: {
        minWidth: 0
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-xs)",
        color: "var(--white)",
        fontWeight: 600,
        whiteSpace: "nowrap"
      }
    }, "Elena Marsh"), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--ink-500)"
      }
    }, "Mfg. Engineer"))));
  }
  function Topbar({
    title,
    crumb,
    actions
  }) {
    return /*#__PURE__*/React.createElement("header", {
      style: {
        height: "var(--topbar-h)",
        flex: "none",
        display: "flex",
        alignItems: "center",
        gap: 14,
        padding: "0 22px",
        background: "var(--bg-surface)",
        borderBottom: "var(--border)"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        minWidth: 0
      }
    }, crumb && /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-2xs)",
        color: "var(--text-muted)",
        marginBottom: 1
      }
    }, crumb), /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: "var(--text-lg)",
        fontWeight: 700,
        color: "var(--text-primary)",
        lineHeight: 1.1
      }
    }, title)), /*#__PURE__*/React.createElement("div", {
      style: {
        marginLeft: "auto",
        width: 240,
        maxWidth: "32vw"
      }
    }, /*#__PURE__*/React.createElement(Input, {
      size: "sm",
      prefix: /*#__PURE__*/React.createElement("i", {
        className: "bi bi-search"
      }),
      placeholder: "Search work orders, parts\u2026"
    })), /*#__PURE__*/React.createElement(IconButton, {
      title: "Notifications",
      variant: "ghost"
    }, /*#__PURE__*/React.createElement("i", {
      className: "bi bi-bell"
    })), actions);
  }
  Object.assign(window, {
    Sidebar,
    Topbar
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/process-manager/shell.jsx", error: String((e && e.message) || e) }); }

__ds_ns.NodeMark = __ds_scope.NodeMark;

__ds_ns.Wordmark = __ds_scope.Wordmark;

__ds_ns.Button = __ds_scope.Button;

__ds_ns.IconButton = __ds_scope.IconButton;

__ds_ns.Card = __ds_scope.Card;

__ds_ns.PortBadge = __ds_scope.PortBadge;

__ds_ns.StatCard = __ds_scope.StatCard;

__ds_ns.StatusBadge = __ds_scope.StatusBadge;

__ds_ns.Tag = __ds_scope.Tag;

__ds_ns.Field = __ds_scope.Field;

__ds_ns.Input = __ds_scope.Input;

__ds_ns.Select = __ds_scope.Select;

})();
