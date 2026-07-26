import React from "react";

/**
 * Status pill for the data-oriented domain: job lifecycle, item grade,
 * step execution state, QA verdicts. Tone sets the color from the
 * brand semantic palette; a leading dot reinforces it.
 */
const TONES = {
  pass:     { label: "Pass",        fg: "var(--teal-700)",   bg: "var(--teal-100)",   dot: "var(--teal)" },
  active:   { label: "In Progress", fg: "var(--blue-700)",   bg: "var(--blue-100)",   dot: "var(--blue)" },
  hold:     { label: "On Hold",     fg: "var(--gold-700)",   bg: "var(--gold-100)",   dot: "var(--gold)" },
  rework:   { label: "Rework",      fg: "var(--orange-700)", bg: "var(--orange-100)", dot: "var(--orange)" },
  fail:     { label: "Fail",        fg: "#9a2b2b",           bg: "#f8dada",           dot: "var(--status-fail)" },
  pending:  { label: "Pending",     fg: "var(--ink-600)",    bg: "var(--grey-100)",   dot: "var(--grey)" },
  done:     { label: "Completed",   fg: "var(--ink-600)",    bg: "var(--grey-100)",   dot: "var(--charcoal)" },
};

export function StatusBadge({ tone = "pending", children, dot = true, className = "", style = {}, ...rest }) {
  const t = TONES[tone] || TONES.pending;
  return (
    <span
      className={className}
      style={{
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
        ...style,
      }}
      {...rest}
    >
      {dot && <span style={{ width: 6, height: 6, borderRadius: "50%", background: t.dot, flex: "none" }} />}
      {children || t.label}
    </span>
  );
}
