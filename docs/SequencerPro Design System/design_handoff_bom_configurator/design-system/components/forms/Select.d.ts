import * as React from "react";

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  size?: "sm" | "md" | "lg";
  invalid?: boolean;
  children?: React.ReactNode;
}

/** Native select styled to match Input. */
export function Select(props: SelectProps): JSX.Element;
