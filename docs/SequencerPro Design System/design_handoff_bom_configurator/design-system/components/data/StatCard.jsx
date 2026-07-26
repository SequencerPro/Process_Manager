import React from "react";

/**
 * KPI / metric tile for dashboards. Big display number (Coda),
 * a label, and an optional delta with up/down tone.
 */
export function StatCard({
  label,
  value,
  unit,
  delta,
  deltaTone = "up",
  icon,
  accent = "var(--gold)",
  className = "",
  style = {},
}) {
  const deltaColor = deltaTone === "up" ? "var(--teal-700)" : deltaTone === "down" ? "#9a2b2b" : "var(--text-secondary)";
  return (
    <div
      className={className}
      style={{
        background: "var(--bg-surface)",
        border: "var(--border)",
        borderRadius: "var(--radius-md)",
        boxShadow: "var(--shadow-sm)",
        padding: "var(--space-4) var(--space-5)",
        display: "flex", flexDirection: "column", gap: "8px",
        ...style,
      }}
    >
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <span style={{
          fontSize: "var(--text-2xs)", fontWeight: "var(--fw-bold)", letterSpacing: "var(--ls-caps)",
          textTransform: "uppercase", color: "var(--text-secondary)",
        }}>{label}</span>
        {icon && (
          <span style={{
            width: 26, height: 26, borderRadius: "var(--radius-sm)",
            display: "inline-flex", alignItems: "center", justifyContent: "center",
            background: `color-mix(in srgb, ${accent} 16%, white)`, color: accent, fontSize: "14px",
          }}>{icon}</span>
        )}
      </div>
      <div style={{ display: "flex", alignItems: "baseline", gap: "6px" }}>
        <span style={{ fontFamily: "var(--font-display)", fontWeight: "var(--fw-display)", fontSize: "var(--text-3xl)", color: "var(--text-primary)", lineHeight: 1 }}>{value}</span>
        {unit && <span style={{ fontSize: "var(--text-sm)", color: "var(--text-secondary)" }}>{unit}</span>}
      </div>
      {delta && (
        <span style={{ fontSize: "var(--text-xs)", fontWeight: "var(--fw-semibold)", color: deltaColor }}>
          {deltaTone === "up" ? "▲" : deltaTone === "down" ? "▼" : "•"} {delta}
        </span>
      )}
    </div>
  );
}
