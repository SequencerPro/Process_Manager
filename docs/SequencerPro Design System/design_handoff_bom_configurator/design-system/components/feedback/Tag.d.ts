import * as React from "react";

export interface TagProps extends React.HTMLAttributes<HTMLSpanElement> {
  color?: "neutral" | "gold" | "teal" | "blue" | "orange";
  /** Monospace text (for codes/IDs). Default true. */
  mono?: boolean;
  removable?: boolean;
  onRemove?: () => void;
  children?: React.ReactNode;
}

/** Chip for codes, kinds, grades and metadata. */
export function Tag(props: TagProps): JSX.Element;
