import * as React from "react";

export interface WordmarkProps extends React.HTMLAttributes<HTMLSpanElement> {
  /** Font size in px (number) or any CSS length. Default 28. */
  size?: number | string;
  /** Override text color. Defaults to --text-primary. Use "var(--gold)" for the brand lockup. */
  color?: string;
  className?: string;
  style?: React.CSSProperties;
}

/**
 * SequencerPro wordmark — Coda with Kelly-Slab "e" flourishes.
 * @startingPoint section="Brand" subtitle="Brand wordmark lockup" viewport="360x120"
 */
export function Wordmark(props: WordmarkProps): JSX.Element;
