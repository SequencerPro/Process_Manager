import * as React from "react";

export interface NodeMarkProps extends React.SVGAttributes<SVGElement> {
  /** Width in px (height is derived). Default 40. */
  size?: number;
  className?: string;
  style?: React.CSSProperties;
}

/** The SequencerPro four-node logo motif (gold→teal→blue→orange ascending). */
export function NodeMark(props: NodeMarkProps): JSX.Element;
