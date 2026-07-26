import React from "react";

/**
 * Surface container. Soft single-layer shadow, hairline border,
 * 6px radius. Optional header (title + actions) and footer.
 */
export function Card({
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
  return (
    <section
      className={className}
      style={{
        background: "var(--bg-surface)",
        border: "var(--border)",
        borderTop: accent ? `3px solid ${accent}` : "var(--border)",
        borderRadius: "var(--radius-md)",
        boxShadow: "var(--shadow-sm)",
        overflow: "hidden",
        ...style,
      }}
      {...rest}
    >
      {(title || actions) && (
        <header style={{
          display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: "12px",
          padding: "var(--space-4) var(--space-5)",
          borderBottom: "var(--border)",
        }}>
          <div>
            {title && <div style={{ fontSize: "var(--text-md)", fontWeight: "var(--fw-bold)", color: "var(--text-primary)" }}>{title}</div>}
            {subtitle && <div style={{ fontSize: "var(--text-xs)", color: "var(--text-secondary)", marginTop: 2 }}>{subtitle}</div>}
          </div>
          {actions && <div style={{ display: "flex", gap: "6px", flex: "none" }}>{actions}</div>}
        </header>
      )}
      <div style={{ padding }}>{children}</div>
      {footer && (
        <footer style={{
          padding: "var(--space-3) var(--space-5)",
          borderTop: "var(--border)", background: "var(--bg-sunken)",
          fontSize: "var(--text-xs)", color: "var(--text-secondary)",
        }}>{footer}</footer>
      )}
    </section>
  );
}
