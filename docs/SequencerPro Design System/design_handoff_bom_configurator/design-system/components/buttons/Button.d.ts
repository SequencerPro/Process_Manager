import * as React from "react";

export type ButtonVariant = "primary" | "accent" | "secondary" | "ghost" | "danger" | "dark";
export type ButtonSize = "sm" | "md" | "lg";

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  /** Visual style. `primary` = gold/slate brand lockup. Default "primary". */
  variant?: ButtonVariant;
  /** Control height. Default "md". */
  size?: ButtonSize;
  /** Icon node placed before the label. */
  iconLeft?: React.ReactNode;
  /** Icon node placed after the label. */
  iconRight?: React.ReactNode;
  /** Full-width block button. */
  block?: boolean;
  children?: React.ReactNode;
}

/**
 * Primary action control for SequencerPro.
 * @startingPoint section="Core" subtitle="Buttons — all variants & sizes" viewport="700x180"
 */
export function Button(props: ButtonProps): JSX.Element;
