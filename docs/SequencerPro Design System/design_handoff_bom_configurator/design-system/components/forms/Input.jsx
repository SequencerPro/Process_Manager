import React from "react";

/**
 * Text input. Compact, 1px hairline, blue focus ring — Bootstrap-5
 * lineage tuned to the brand. Supports invalid state and prefix/suffix.
 */
export function Input({
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
  const heights = { sm: "var(--control-h-sm)", md: "var(--control-h)", lg: "var(--control-h-lg)" };
  const [focus, setFocus] = React.useState(false);
  return (
    <div
      className={className}
      style={{
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
        ...style,
      }}
    >
      {prefix && <span style={{ color: "var(--text-muted)", display: "inline-flex" }}>{prefix}</span>}
      <input
        disabled={disabled}
        onFocus={(e) => { setFocus(true); rest.onFocus?.(e); }}
        onBlur={(e) => { setFocus(false); rest.onBlur?.(e); }}
        {...rest}
        style={{
          flex: 1,
          minWidth: 0,
          border: "none",
          outline: "none",
          background: "transparent",
          color: "var(--text-primary)",
          fontFamily: mono ? "var(--font-mono)" : "var(--font-sans)",
          fontSize: "var(--text-sm)",
        }}
      />
      {suffix && <span style={{ color: "var(--text-muted)", display: "inline-flex" }}>{suffix}</span>}
    </div>
  );
}
