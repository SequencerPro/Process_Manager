import * as React from "react";

export interface CardProps extends React.HTMLAttributes<HTMLElement> {
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
  /** Action nodes shown top-right of the header. */
  actions?: React.ReactNode;
  footer?: React.ReactNode;
  /** Optional top accent stripe color, e.g. "var(--orange)". */
  accent?: string;
  /** Body padding. Default var(--space-5). */
  padding?: string;
  children?: React.ReactNode;
}

/**
 * Surface container with optional header/footer.
 * @startingPoint section="Core" subtitle="Cards, KPI tiles & port badges" viewport="700x320"
 */
export function Card(props: CardProps): JSX.Element;
