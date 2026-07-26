import React from "react";

const SIZES = { sm: 28, md: 34, lg: 42 };

/**
 * Square icon-only button. Use for toolbar actions, table row actions,
 * and anywhere a label would be redundant. Always set `title` for a11y.
 */
export function IconButton({
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
    ghost:     { background: "transparent", color: "var(--text-secondary)", border: "1px solid transparent" },
    secondary: { background: "var(--pure-white)", color: "var(--text-primary)", border: "1px solid var(--border-strong)" },
    primary:   { background: "var(--gold)", color: "var(--slate)", border: "1px solid var(--gold)" },
  };
  const v = variants[variant] || variants.ghost;
  return (
    <button
      title={title}
      aria-label={title}
      disabled={disabled}
      className={`sq-iconbtn ${className}`}
      style={{
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        width: d, height: d,
        borderRadius: "var(--radius)",
        cursor: disabled ? "not-allowed" : "pointer",
        opacity: disabled ? 0.5 : 1,
        transition: "background var(--dur) var(--ease), color var(--dur) var(--ease)",
        ...v,
        ...style,
      }}
      onMouseEnter={(e) => { if (!disabled && variant === "ghost") e.currentTarget.style.background = "var(--grey-100)"; }}
      onMouseLeave={(e) => { if (variant === "ghost") e.currentTarget.style.background = "transparent"; }}
      {...rest}
    >
      {children}
    </button>
  );
}
