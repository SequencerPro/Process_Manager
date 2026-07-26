import * as React from "react";

export type PortType = "Material" | "Parameter" | "Characteristic" | "Condition";

export interface PortBadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  /** PortType from the process model. Default "Material". */
  type?: PortType;
  /** Flow direction. Default "in". */
  direction?: "in" | "out";
  /** Override the label (defaults to the type abbreviation). */
  label?: React.ReactNode;
}

/** Typed Step connection point (Material/Parameter/Characteristic/Condition). */
export function PortBadge(props: PortBadgeProps): JSX.Element;
