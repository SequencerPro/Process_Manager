import * as React from "react";

export interface InputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, "size" | "prefix"> {
  size?: "sm" | "md" | "lg";
  invalid?: boolean;
  /** Node rendered inside, before the field (icon / unit). */
  prefix?: React.ReactNode;
  /** Node rendered inside, after the field. */
  suffix?: React.ReactNode;
  /** Monospace value (codes, serials, tolerances). */
  mono?: boolean;
}

/**
 * Text input with focus ring and invalid state.
 * @startingPoint section="Forms" subtitle="Inputs, selects & fields" viewport="700x300"
 */
export function Input(props: InputProps): JSX.Element;
