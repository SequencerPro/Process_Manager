import React from "react";

/** Form field wrapper: label, optional hint/required marker, and error text. */
export function Field({ label, hint, error, required = false, htmlFor, className = "", style = {}, children }) {
  return (
    <div className={className} style={{ display: "flex", flexDirection: "column", gap: "5px", ...style }}>
      {label && (
        <label htmlFor={htmlFor} style={{
          fontSize: "var(--text-xs)", fontWeight: "var(--fw-semibold)", color: "var(--text-primary)",
        }}>
          {label}
          {required && <span style={{ color: "var(--status-fail)", marginLeft: 3 }}>*</span>}
        </label>
      )}
      {children}
      {error
        ? <span style={{ fontSize: "var(--text-2xs)", color: "var(--status-fail)" }}>{error}</span>
        : hint && <span style={{ fontSize: "var(--text-2xs)", color: "var(--text-muted)" }}>{hint}</span>}
    </div>
  );
}
