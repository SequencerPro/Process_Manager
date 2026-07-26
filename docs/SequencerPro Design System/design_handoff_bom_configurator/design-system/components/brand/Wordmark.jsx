import React from "react";

/**
 * SequencerPro wordmark. Coda body with Kelly-Slab "e" flourishes,
 * exactly as the product locks it up.
 */
export function Wordmark({ size = 28, color, className = "", style = {}, ...rest }) {
  const E = (k) => (
    <span key={k} style={{ fontFamily: "var(--font-accent)", fontStyle: "normal", fontSize: "1.08em" }}>e</span>
  );
  return (
    <span
      className={className}
      style={{
        fontFamily: "var(--font-display)",
        fontWeight: "var(--fw-display)",
        fontSize: typeof size === "number" ? `${size}px` : size,
        lineHeight: 1,
        whiteSpace: "nowrap",
        color: color || "var(--text-primary)",
        ...style,
      }}
      {...rest}
    >
      S{E("e1")}qu{E("e2")}nc{E("e3")}rPro
    </span>
  );
}
