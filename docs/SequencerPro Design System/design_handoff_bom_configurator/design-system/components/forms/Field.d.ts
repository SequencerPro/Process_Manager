import * as React from "react";

export interface FieldProps {
  label?: React.ReactNode;
  hint?: React.ReactNode;
  error?: React.ReactNode;
  required?: boolean;
  htmlFor?: string;
  className?: string;
  style?: React.CSSProperties;
  children?: React.ReactNode;
}

/** Label + hint/error wrapper around any control. */
export function Field(props: FieldProps): JSX.Element;
