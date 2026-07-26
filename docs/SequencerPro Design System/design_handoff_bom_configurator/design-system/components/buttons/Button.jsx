import React from "react";

const VARIANTS = {
  primary:   { background: "var(--gold)", color: "var(--slate)", border: "1px solid var(--gold)" },
  accent:    { background: "var(--orange)", color: "#fff", border: "1px solid var(--orange)" },
  secondary: { background: "var(--pure-white)", color: "var(--text-primary)", border: "1px solid var(--border-strong)" },
  ghost:     { background: "transparent", color: "var(--text-primary)", border: "1px solid transparent" },
  danger:    { background: "var(--status-fail)", color: "#fff", border: "1px solid var(--status-fail)" },
  dark:      { background: "var(--slate)", color: "var(--white)", border: "1px solid var(--slate)" },
};

const SIZES = {
  sm: { height: "var(--control-h-sm)", padding: "0 10px", fontSize: "var(--text-xs)", gap: "5px" },
  md: { height: "var(--control-h)",    padding: "0 14px", fontSize: "var(--text-sm)", gap: "6px" },
  lg: { height: "var(--control-h-lg)", padding: "0 20px", fontSize: "var(--text-md)", gap: "8px" },
};

/**
 * Primary action control. Variants map to the brand node colors;
 * `primary` is the gold-on-slate lockup used across the product.
 */
export function Button({
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
  return (
    <button
      className={`sq-btn ${className}`}
      disabled={disabled}
      style={{
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
        ...style,
      }}
      onMouseEnter={(e) => { if (!disabled) e.currentTarget.style.filter = "brightness(0.94)"; }}
      onMouseLeave={(e) => { e.currentTarget.style.filter = "none"; }}
      {...rest}
    >
      {iconLeft}
      {children}
      {iconRight}
    </button>
  );
}
