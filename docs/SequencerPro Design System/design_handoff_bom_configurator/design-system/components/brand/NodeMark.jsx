import React from "react";

const NODES = [
  { c: "var(--gold)", x: 14, y: 76 },
  { c: "var(--teal)", x: 52, y: 50 },
  { c: "var(--blue)", x: 92, y: 62 },
  { c: "var(--orange)", x: 134, y: 24 },
];

/**
 * NodeMark — the SequencerPro logo motif: four process nodes
 * (gold → teal → blue → orange) connected in an ascending sequence.
 * Pure CSS/SVG recreation so it tints and scales cleanly.
 */
export function NodeMark({ size = 40, className = "", style = {}, ...rest }) {
  const r = [11, 15, 16, 21];
  return (
    <svg
      className={className}
      width={size}
      height={(size * 100) / 168}
      viewBox="0 0 168 100"
      fill="none"
      style={style}
      role="img"
      aria-label="SequencerPro"
      {...rest}
    >
      {NODES.slice(0, -1).map((n, i) => {
        const m = NODES[i + 1];
        return (
          <line
            key={i}
            x1={n.x} y1={n.y} x2={m.x} y2={m.y}
            stroke="var(--grey-300)" strokeWidth="7" strokeLinecap="round"
          />
        );
      })}
      {NODES.map((n, i) => (
        <circle key={i} cx={n.x} cy={n.y} r={r[i]} fill={n.c}
          stroke="var(--pure-white)" strokeWidth="3" />
      ))}
    </svg>
  );
}
