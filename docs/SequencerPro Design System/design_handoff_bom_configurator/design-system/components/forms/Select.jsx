import React from "react";

/** Native select styled to match Input. */
export function Select({ size = "md", invalid = false, className = "", style = {}, children, ...rest }) {
  const heights = { sm: "var(--control-h-sm)", md: "var(--control-h)", lg: "var(--control-h-lg)" };
  return (
    <div className={className} style={{ position: "relative", display: "inline-flex", width: "100%", ...style }}>
      <select
        {...rest}
        style={{
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
          outline: "none",
        }}
      >
        {children}
      </select>
      <span style={{
        position: "absolute", right: 10, top: "50%", transform: "translateY(-50%)",
        pointerEvents: "none", color: "var(--text-muted)", fontSize: "10px",
      }}>▼</span>
    </div>
  );
}
