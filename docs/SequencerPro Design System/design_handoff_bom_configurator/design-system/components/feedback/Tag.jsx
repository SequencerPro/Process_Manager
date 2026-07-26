import React from "react";

/**
 * Tag / chip for codes, kinds, grades, and metadata. Monospace by
 * default because it most often carries an identifier (WDG-100, WO-2026-001).
 */
export function Tag({
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
    neutral: { bg: "var(--grey-100)",   fg: "var(--ink-600)", bd: "var(--border-default)" },
    gold:    { bg: "var(--gold-100)",   fg: "var(--gold-700)", bd: "var(--gold)" },
    teal:    { bg: "var(--teal-100)",   fg: "var(--teal-700)", bd: "var(--teal)" },
    blue:    { bg: "var(--blue-100)",   fg: "var(--blue-700)", bd: "var(--blue)" },
    orange:  { bg: "var(--orange-100)", fg: "var(--orange-700)", bd: "var(--orange)" },
  };
  const c = colors[color] || colors.neutral;
  return (
    <span
      className={className}
      style={{
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
        ...style,
      }}
      {...rest}
    >
      {children}
      {removable && (
        <button
          onClick={onRemove}
          aria-label="Remove"
          style={{
            border: "none", background: "none", cursor: "pointer",
            color: "inherit", opacity: 0.6, padding: 0, lineHeight: 1, fontSize: "13px",
          }}
        >×</button>
      )}
    </span>
  );
}
