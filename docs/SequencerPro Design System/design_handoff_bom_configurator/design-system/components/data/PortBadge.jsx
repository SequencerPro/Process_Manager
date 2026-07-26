import React from "react";

/**
 * PortBadge — domain primitive. Represents a Step's typed connection
 * point. PortType (Material / Parameter / Characteristic / Condition)
 * sets the color & glyph; direction sets the arrow. Mirrors the
 * Process Manager port model used in PFMEA / C&E / Control Plan tools.
 */
const TYPES = {
  Material:       { color: "var(--blue)",   glyph: "▣", abbr: "MAT" },
  Parameter:      { color: "var(--gold)",   glyph: "𝑥", abbr: "PARAM" },
  Characteristic: { color: "var(--teal)",   glyph: "𝑦", abbr: "CHAR" },
  Condition:      { color: "var(--orange)", glyph: "✓", abbr: "COND" },
};

export function PortBadge({
  type = "Material",
  direction = "in",
  label,
  className = "",
  style = {},
  ...rest
}) {
  const t = TYPES[type] || TYPES.Material;
  const isIn = direction === "in";
  return (
    <span
      className={className}
      style={{
        display: "inline-flex", alignItems: "center", gap: "6px",
        height: "22px", padding: "0 8px 0 6px",
        background: `color-mix(in srgb, ${t.color} 12%, white)`,
        border: `1px solid color-mix(in srgb, ${t.color} 45%, white)`,
        borderRadius: "var(--radius-pill)",
        fontSize: "var(--text-2xs)", fontWeight: "var(--fw-semibold)",
        color: "var(--text-primary)", whiteSpace: "nowrap",
        ...style,
      }}
      {...rest}
    >
      <span style={{
        color: t.color, fontWeight: "var(--fw-bold)",
        transform: isIn ? "none" : "scaleX(-1)", display: "inline-block",
      }} aria-hidden>{isIn ? "→" : "→"}</span>
      <span style={{
        width: 15, height: 15, borderRadius: "3px", flex: "none",
        display: "inline-flex", alignItems: "center", justifyContent: "center",
        background: t.color, color: "#fff", fontSize: "10px", lineHeight: 1,
      }}>{t.glyph}</span>
      <span style={{ fontFamily: "var(--font-mono)" }}>{label || t.abbr}</span>
    </span>
  );
}
