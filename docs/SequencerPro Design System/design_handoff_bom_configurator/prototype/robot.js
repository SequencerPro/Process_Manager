/* ============================================================
   BoM Configurator — schematic robot preview (SVG, reacts to config)
   window.SchematicRobot
   Side view of a 6-axis arm: link lengths scale with reach, the
   end effector swaps by type, a rail appears for the 7th axis,
   and the payload-class badge mirrors the arm selection.
   ============================================================ */
(function () {
  function Joint({ cx, cy, r }) {
    return React.createElement("g", null,
      React.createElement("circle", { cx, cy, r, fill: "#2b2f36", stroke: "#15171b", strokeWidth: 1.5 }),
      React.createElement("circle", { cx, cy, r: r * 0.45, fill: "#9aa0a8" }));
  }

  // End-effector glyphs, drawn at the wrist (wx, wy), pointing right.
  function Effector({ type, wx, wy }) {
    const g = "#3d434c";
    if (type === "grip") return React.createElement("g", null,
      React.createElement("rect", { x: wx + 8, y: wy - 8, width: 12, height: 16, rx: 2, fill: g }),
      React.createElement("path", { d: `M${wx + 20},${wy - 8} h16 l4,5 h-20 Z`, fill: "#5e6570" }),
      React.createElement("path", { d: `M${wx + 20},${wy + 8} h16 l4,-5 h-20 Z`, fill: "#5e6570" }));
    if (type === "vac") return React.createElement("g", null,
      React.createElement("rect", { x: wx + 8, y: wy - 14, width: 8, height: 28, rx: 2, fill: g }),
      [-10, -3, 4, 11].map((dy, i) => React.createElement("g", { key: i },
        React.createElement("line", { x1: wx + 16, y1: wy + dy + 1.5, x2: wx + 24, y2: wy + dy + 1.5, stroke: g, strokeWidth: 2 }),
        React.createElement("path", { d: `M${wx + 24},${wy + dy - 2} q7,3.5 0,7 Z`, fill: "#5e6570" }))));
    if (type === "weld") return React.createElement("g", null,
      React.createElement("rect", { x: wx + 8, y: wy - 5, width: 14, height: 10, rx: 2, fill: g }),
      React.createElement("line", { x1: wx + 22, y1: wy, x2: wx + 38, y2: wy + 10, stroke: "#5e6570", strokeWidth: 5, strokeLinecap: "round" }),
      React.createElement("circle", { cx: wx + 41, cy: wy + 12, r: 3.5, fill: "#f1c40f" }),
      [[8, 2], [10, -4], [4, -8]].map(([dx, dy], i) =>
        React.createElement("line", { key: i, x1: wx + 41, y1: wy + 12, x2: wx + 41 + dx, y2: wy + 12 + dy, stroke: "#e95b15", strokeWidth: 1.6, strokeLinecap: "round" })));
    // bare ISO flange
    return React.createElement("g", null,
      React.createElement("rect", { x: wx + 8, y: wy - 9, width: 6, height: 18, rx: 1.5, fill: g }),
      [-5, 0, 5].map((dy, i) => React.createElement("circle", { key: i, cx: wx + 11, cy: wy + dy, r: 1.3, fill: "#9aa0a8" })));
  }

  function SchematicRobot({ color = "#e95b15", reach = 1300, effector = "none", badge = "10 KG", hasRail = false }) {
    // scale link lengths with reach (1300 mm = 1.0)
    const s = Math.max(0.8, Math.min(1.3, reach / 1300));
    const floorY = 210;
    const bx = hasRail ? 210 : 240;           // base center x
    const j1 = { x: bx, y: 168 };             // shoulder (axis 2/3 cluster)
    const L1 = 92 * s, L2 = 128 * s;
    const a1 = (64 * Math.PI) / 180;          // lower arm angle up-right
    const j2 = { x: j1.x + Math.cos(a1) * L1, y: j1.y - Math.sin(a1) * L1 };
    const a2 = (-14 * Math.PI) / 180;         // upper arm slightly downward
    const j3 = { x: j2.x + Math.cos(a2) * L2, y: j2.y - Math.sin(a2) * L2 };
    const stroke = "rgba(0,0,0,0.25)";
    const badgeFill = badge === "20 KG" ? "#e95b15" : badge === "5 KG" ? "#3d3d3d" : "#529bde";
    const pedTop = j1.y + 14;

    return React.createElement("svg", { viewBox: "0 0 560 250", width: "100%", style: { display: "block" } },
      // ground shadow + floor
      React.createElement("ellipse", { cx: 280, cy: floorY + 16, rx: 210, ry: 11, fill: "rgba(33,33,33,0.10)" }),
      React.createElement("line", { x1: 60, y1: floorY + 14, x2: 500, y2: floorY + 14, stroke: "#d4d4d4", strokeWidth: 2 }),

      // 7th-axis rail + carriage
      hasRail && React.createElement("g", null,
        React.createElement("rect", { x: 100, y: floorY + 2, width: 360, height: 9, rx: 2, fill: "#3d434c" }),
        [0, 1, 2, 3, 4, 5].map(i => React.createElement("line", { key: i, x1: 118 + i * 66, y1: floorY + 2, x2: 118 + i * 66, y2: floorY + 11, stroke: "#5e6570", strokeWidth: 2 })),
        React.createElement("rect", { x: bx - 44, y: floorY - 8, width: 88, height: 12, rx: 3, fill: "#2b2f36" })),

      // pedestal
      React.createElement("path", { d: `M${bx - 42},${hasRail ? floorY - 8 : floorY + 2} L${bx + 42},${hasRail ? floorY - 8 : floorY + 2} L${bx + 26},${pedTop} L${bx - 26},${pedTop} Z`,
        fill: color, stroke, strokeWidth: 1.5, strokeLinejoin: "round" }),
      React.createElement("rect", { x: bx - 30, y: pedTop - 10, width: 60, height: 14, rx: 4, fill: color, stroke, strokeWidth: 1.2 }),

      // arm links (drawn as thick rounded strokes in the body color)
      React.createElement("line", { x1: j1.x, y1: j1.y, x2: j2.x, y2: j2.y, stroke: color, strokeWidth: 26, strokeLinecap: "round" }),
      React.createElement("line", { x1: j1.x, y1: j1.y, x2: j2.x, y2: j2.y, stroke, strokeWidth: 26, strokeLinecap: "round", fill: "none", opacity: 0.18 }),
      React.createElement("line", { x1: j2.x, y1: j2.y, x2: j3.x, y2: j3.y, stroke: color, strokeWidth: 19, strokeLinecap: "round" }),

      // joints
      React.createElement(Joint, { cx: j1.x, cy: j1.y, r: 17 }),
      React.createElement(Joint, { cx: j2.x, cy: j2.y, r: 13 }),
      React.createElement(Joint, { cx: j3.x, cy: j3.y, r: 9 }),

      // end effector at the wrist
      React.createElement(Effector, { type: effector, wx: j3.x, wy: j3.y }),

      // payload badge
      React.createElement("g", null,
        React.createElement("rect", { x: 250, y: 228, width: 62, height: 20, rx: 10, fill: badgeFill }),
        React.createElement("text", { x: 281, y: 242, textAnchor: "middle", fontSize: 11, fontWeight: 700,
          fontFamily: "var(--font-mono)", fill: "#fff" }, badge)));
  }

  window.SchematicRobot = SchematicRobot;
})();
