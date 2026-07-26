import * as React from "react";

export type StatusTone = "pass" | "active" | "hold" | "rework" | "fail" | "pending" | "done";

export interface StatusBadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  /** Semantic state. Drives color + default label. Default "pending". */
  tone?: StatusTone;
  /** Show the leading status dot. Default true. */
  dot?: boolean;
  /** Override the label; falls back to the tone's default word. */
  children?: React.ReactNode;
}

/**
 * Status pill for job/item/step lifecycle and QA verdicts.
 * @startingPoint section="Core" subtitle="Status pills for lifecycle & QA" viewport="700x140"
 */
export function StatusBadge(props: StatusBadgeProps): JSX.Element;
