import * as React from "react";

export interface StatCardProps {
  label: React.ReactNode;
  value: React.ReactNode;
  unit?: React.ReactNode;
  delta?: React.ReactNode;
  deltaTone?: "up" | "down" | "flat";
  icon?: React.ReactNode;
  /** Icon accent color. Default var(--gold). */
  accent?: string;
  className?: string;
  style?: React.CSSProperties;
}

/** KPI metric tile with a Coda display number. */
export function StatCard(props: StatCardProps): JSX.Element;
