/* ============================================================
   BoM Configurator — Robot platform data
   "Sequencer RX-6" — a widely-configurable 6-axis industrial
   robot (generic; not modeled on any vendor's product).
   Option groups → options; each option carries the BoM assemblies
   it adds, the routing operations it triggers, and a price delta.
   Rules are declarative so the same list drives the live engine,
   the inline disabling, the validation panel AND the author canvas.

   window.CAR — legacy global name kept so the engine/views don't
   need renaming; the content is the robot platform.
   ============================================================ */
window.CAR = {
  platform: { code: "SEQ-RX6", name: "Sequencer RX-6", tagline: "Configurable 6-Axis Industrial Robot", basePrice: 28000 },

  // ISO 9001:2015 / ISO 10007 controlled-document metadata for the spec sheet.
  compliance: {
    formNo: "QF-BOM-001",
    revision: "A",
    standard: "ISO 9001:2015",
    clauses: "§8.5.1 Production control · §8.5.2 Identification & traceability · ISO 10007 Configuration management",
    classification: "Controlled — Confidential",
    owner: "Manufacturing Engineering",
    retention: "Retain 7 years",
    approvals: [
      { role: "Prepared by", name: "", title: "Configuration Engineer" },
      { role: "Reviewed by", name: "", title: "Manufacturing Engineer" },
      { role: "Approved by", name: "", title: "Engineering Manager" },
      { role: "Quality release", name: "", title: "QA Inspector" },
    ],
    revisionHistory: [
      { rev: "A", date: "2026-07-01", description: "Initial release from configurator", author: "Sequencer Process Manager" },
    ],
  },

  // Base assemblies every build includes (multi-level BoM root).
  baseBom: [
    { system: "Structure", code: "BASE-001", name: "Base & swing assembly", qty: 1, price: 4600, children: [
      { code: "PED-01", name: "Pedestal casting", qty: 1, price: 1700 },
      { code: "SWG-01", name: "Swing bearing (axis 1)", qty: 1, price: 1400 },
    ]},
    { system: "Structure", code: "LINK-001", name: "Arm linkage set", qty: 1, price: 3900, children: [
      { code: "ARM-LWR", name: "Lower arm casting", qty: 1, price: 1500 },
      { code: "ARM-UPR", name: "Upper arm casting", qty: 1, price: 1300 },
      { code: "WRST-HSG", name: "Wrist housing", qty: 1, price: 1100 },
    ]},
    { system: "Motion", code: "DRV-001", name: "Axis drive package", qty: 1, price: 0, children: [
      { code: "SRV-AX", name: "AC servo motor", qtyExpr: "axisMotors", price: 850 },
      { code: "RED-AX", name: "Harmonic reducer", qtyExpr: "axisMotors", price: 1150 },
    ]},
    { system: "Electrical", code: "ELEC-001", name: "Electrical base kit", qty: 1, price: 1900, children: [
      { code: "PND-01", name: "Teach pendant", qty: 1, price: 900 },
      { code: "SFC-01", name: "Safety I/O module", qty: 1, price: 600 },
      { code: "HARN-01", name: "Internal harness set", qty: 1, price: 400 },
    ]},
  ],

  // Base routing operations (always present). seq = order weight.
  baseOps: [
    { code: "MACH-01", name: "Casting Machining", seq: 10 },
    { code: "PAINT-01", name: "Prime & Paint", seq: 20 },
    { code: "ARM-01", name: "Arm & Joint Assembly", seq: 30 },
    { code: "WIRE-01", name: "Harness Routing", seq: 70 },
    { code: "CAL-01", name: "Axis Calibration", seq: 90 },
    { code: "QA-01", name: "End-of-Line Test", seq: 95 },
  ],

  // Numeric ATTRIBUTES that feed formulas (D365-style calculated config).
  attributes: [
    { id: "reach",    name: "Working reach",     unit: "mm",     kind: "range", min: 600, max: 2200, step: 100, default: 1300,
      hint: "Arm reach at full extension. Drives dress-pack segments; beyond 1400 mm requires the 20 kg arm." },
    { id: "railLen",  name: "7th-axis rail length", unit: "mm",  kind: "range", min: 0, max: 6000, step: 500, default: 0,
      hint: "0 = no track. Rail segments are computed as ceil(length ÷ 1000); a fitted track adds a 7th servo axis." },
    { id: "orderQty", name: "Order quantity",    unit: "robots", kind: "range", min: 1, max: 25, step: 1, default: 1,
      hint: "Robots built by this work order. Multiplies the order-level rollup." },
  ],

  // CALCULATED attributes — read-only, value derived from a formula over attributes
  // and selections (has('id') tests a selected option). Resolved top-to-bottom.
  calc: [
    { id: "axisMotors", name: "Servo axes",        expr: "6 + (railLen > 0 ? 1 : 0)", unit: "", hint: "Six arm axes, plus the rail axis when a track is fitted." },
    { id: "railSegs",   name: "Rail segments",     expr: "railLen > 0 ? ceil(railLen / 1000) : 0", unit: "", hint: "ceil(rail length ÷ 1000 mm)." },
    { id: "dressSegs",  name: "Dress-pack segments", expr: "ceil(reach / 700)", unit: "", hint: "Cable dress segments scale with reach: ceil(reach ÷ 700 mm)." },
  ],

  groups: [
    { id: "arm", name: "Arm Class", icon: "bi-robot", multi: false, required: true,
      hint: "Payload capacity class — castings, wrist and counterbalance.", options: [
      { id: "arm-5",  name: "RX-6/5 · 5 kg",  price: 0, sub: "Light handling & assembly", badge: "5 KG",
        bom: [{ system:"Structure", code:"ARM-C5", name:"5 kg arm kit", qty:1, price:3200, children:[
          {code:"WRST-5",name:"Wrist unit (5 kg)",qty:1,price:1100}]}], ops: [] },
      { id: "arm-10", name: "RX-6/10 · 10 kg", price: 5800, sub: "General purpose", badge: "10 KG",
        bom: [{ system:"Structure", code:"ARM-C10", name:"10 kg arm kit", qty:1, price:5100, children:[
          {code:"WRST-10",name:"Wrist unit (10 kg)",qty:1,price:1600},{code:"ELB-R",name:"Reinforced elbow",qty:1,price:700}]}], ops: [] },
      { id: "arm-20", name: "RX-6/20 · 20 kg", price: 12500, sub: "Heavy payload / long reach", badge: "20 KG",
        bom: [{ system:"Structure", code:"ARM-C20", name:"20 kg arm kit", qty:1, price:8400, children:[
          {code:"WRST-20",name:"Wrist unit (20 kg)",qty:1,price:2300},{code:"CBAL-1",name:"Counterbalance cylinder",qty:2,price:650}]}],
        ops: [{code:"CBAL-01",name:"Counterbalance Fit",seq:35}] },
    ]},

    { id: "controller", name: "Controller", icon: "bi-cpu", multi: false, required: true,
      hint: "Cabinet, drives and path computer.", options: [
      { id: "ctl-compact", name: "SC-1 Compact", price: 0, sub: "Cell-mount cabinet",
        bom: [{ system:"Electrical", code:"CTL-SC1", name:"SC-1 compact cabinet", qty:1, price:2600, children:[
          {code:"PSU-1",name:"Drive PSU",qty:1,price:600},{code:"IO-16",name:"I/O board (16ch)",qty:1,price:300}]}],
        ops: [{code:"CTRL-01",name:"Controller Marriage",seq:60}] },
      { id: "ctl-std", name: "SC-3 Standard", price: 3400, sub: "Floor cabinet, expandable I/O",
        bom: [{ system:"Electrical", code:"CTL-SC3", name:"SC-3 standard cabinet", qty:1, price:4200, children:[
          {code:"PSU-2",name:"Drive PSU (heavy)",qty:1,price:900},{code:"IO-64",name:"I/O rack (64ch)",qty:1,price:700}]}],
        ops: [{code:"CTRL-01",name:"Controller Marriage",seq:60}] },
      { id: "ctl-perf", name: "SC-5 High-Path", price: 8200, sub: "Path co-processor, 7-axis ready",
        bom: [{ system:"Electrical", code:"CTL-SC5", name:"SC-5 high-path cabinet", qty:1, price:7400, children:[
          {code:"PSU-3",name:"Drive PSU (perf)",qty:1,price:1100},{code:"CPU-P",name:"Path co-processor",qty:1,price:1800},{code:"IO-64",name:"I/O rack (64ch)",qty:1,price:700}]}],
        ops: [{code:"CTRL-01",name:"Controller Marriage",seq:60}] },
    ]},

    { id: "mount", name: "Mounting", icon: "bi-arrows-move", multi: false, required: true,
      hint: "How the robot is installed in the cell.", options: [
      { id: "mnt-floor", name: "Floor Mount", price: 0, sub: "Standard baseplate",
        bom: [{ system:"Structure", code:"MNT-FL", name:"Floor baseplate kit", qty:1, price:450, children:[] }], ops: [] },
      { id: "mnt-wall", name: "Wall Mount", price: 600, sub: "Side bracket + shimming",
        bom: [{ system:"Structure", code:"MNT-WL", name:"Wall bracket kit", qty:1, price:900, children:[] }],
        ops: [{code:"MNT-02",name:"Bracket Prep & Shim",seq:15}] },
      { id: "mnt-ceiling", name: "Ceiling Mount", price: 900, sub: "Inverted kit, drip protection",
        bom: [{ system:"Structure", code:"MNT-CL", name:"Inverted-mount kit", qty:1, price:1200, children:[
          {code:"DRIP-1",name:"Drip shield",qty:1,price:250}]}],
        ops: [{code:"MNT-02",name:"Inverted-Mount Prep",seq:15}] },
    ]},

    { id: "effector", name: "End Effector", icon: "bi-magic", multi: false, required: true,
      hint: "Tooling on the wrist flange.", options: [
      { id: "ee-none", name: "Customer-Supplied", price: 0, sub: "Bare ISO 9409 flange", ee: "none",
        bom: [], ops: [] },
      { id: "ee-grip", name: "2-Finger Servo Gripper", price: 2800, sub: "Parallel, force-controlled", ee: "grip",
        bom: [{ system:"Tooling", code:"EE-GRIP", name:"Servo gripper", qty:1, price:2400, children:[
          {code:"FING-2",name:"Finger set",qty:2,price:180},{code:"SRV-G",name:"Gripper servo",qty:1,price:600}]}], ops: [] },
      { id: "ee-vac", name: "Vacuum Plate (4-cup)", price: 2200, sub: "Venturi vacuum, foam seal", ee: "vac",
        bom: [{ system:"Tooling", code:"EE-VAC", name:"Vacuum plate", qty:1, price:1500, children:[
          {code:"CUP-4",name:"Suction cup",qty:4,price:60},{code:"VENT-1",name:"Venturi generator",qty:1,price:420}]}], ops: [] },
      { id: "ee-weld", name: "MIG Weld Torch", price: 4800, sub: "Water-cooled, wire feeder", ee: "weld",
        bom: [{ system:"Tooling", code:"EE-WELD", name:"MIG torch package", qty:1, price:3900, children:[
          {code:"TRCH-1",name:"Water-cooled torch",qty:1,price:1600},{code:"FEED-1",name:"Wire feeder",qty:1,price:1300}]}],
        ops: [{code:"WELD-01",name:"Torch Fit & Purge Test",seq:65}] },
    ]},

    { id: "color", name: "Finish", icon: "bi-palette", multi: false, required: true,
      hint: "Paint finish.", options: [
      { id: "col-orange", name: "Safety Orange", price: 0, swatch: "#e95b15",
        bom: [{ system:"Structure", code:"PNT-ORG", name:"Paint — Safety Orange", qty:1, price:250, children:[] }], ops: [] },
      { id: "col-graphite", name: "Graphite", price: 400, swatch: "#3d3d3d",
        bom: [{ system:"Structure", code:"PNT-GPH", name:"Paint — Graphite", qty:1, price:400, children:[] }], ops: [] },
      { id: "col-white", name: "Cleanroom White", price: 900, swatch: "#eef0f2",
        bom: [{ system:"Structure", code:"PNT-WHT", name:"Paint — Cleanroom White (low-particle)", qty:1, price:900, children:[] }], ops: [] },
      { id: "col-gold", name: "Heritage Gold", price: 1200, swatch: "#f1c40f",
        bom: [{ system:"Structure", code:"PNT-GLD", name:"Paint — Heritage Gold (signature)", qty:1, price:1200, children:[] }],
        ops: [{code:"PAINT-02",name:"Signature Paint Cell",seq:22}] },
    ]},

    { id: "packages", name: "Packages", icon: "bi-box-seam", multi: true, required: false,
      hint: "Bundled hardware options.", options: [
      { id: "pkg-vision", name: "Vision Package", price: 5500, sub: "2D cameras, lighting, GPU",
        bom: [{ system:"Electrical", code:"PKG-VIS", name:"Vision kit", qty:1, price:4800, children:[
          {code:"CAM-2D",name:"2D camera",qty:2,price:1200},{code:"LGT-BAR",name:"LED light bar",qty:2,price:300},{code:"GPU-1",name:"Vision GPU module",qty:1,price:1800}]}],
        ops: [{code:"VIS-01",name:"Vision Calibration",seq:92}] },
      { id: "pkg-track", name: "7th-Axis Track", priceExpr: "2400 + 1.5 * railLen", price: 2400, sub: "Carriage + rail, priced per mm",
        bom: [{ system:"Motion", code:"PKG-TRK", name:"Linear track system", qty:1, price:1800, children:[
          {code:"TRK-CAR",name:"Track carriage",qty:1,price:1800},{code:"RAIL-SEG",name:"Rail segment (1 m)",qtyExpr:"railSegs",price:650}]}],
        ops: [{code:"TRK-01",name:"Track Install & Align",seq:12}] },
      { id: "pkg-dress", name: "Dress Pack", price: 900, sub: "Cable chain, segments scale with reach",
        bom: [{ system:"Electrical", code:"PKG-DRS", name:"Dress-pack kit", qty:1, price:400, children:[
          {code:"DRS-SEG",name:"Dress segment",qtyExpr:"dressSegs",price:180}]}], ops: [] },
      { id: "pkg-clean", name: "Cleanroom Package", price: 3200, sub: "ISO 5 — sealed bellows, LP grease",
        bom: [{ system:"Structure", code:"PKG-CLN", name:"Cleanroom kit", qty:1, price:3000, children:[
          {code:"BELW-1",name:"Sealed bellows set",qty:1,price:1400},{code:"GRS-LP",name:"Low-particle grease service",qty:1,price:600}]}], ops: [] },
      { id: "pkg-foundry", name: "Foundry Package", price: 2600, sub: "IP67 — heat jacket, sealed connectors",
        bom: [{ system:"Structure", code:"PKG-FDY", name:"Foundry protection kit", qty:1, price:2400, children:[
          {code:"JKT-HT",name:"Heat-resist jacket",qty:1,price:1200},{code:"CONN-67",name:"IP67 connector set",qty:1,price:700}]}], ops: [] },
    ]},

    { id: "software", name: "Software", icon: "bi-code-square", multi: true, required: false,
      hint: "Licensed application suites.", options: [
      { id: "sw-path", name: "Path Optimization Suite", price: 1900, sub: "Cycle-time & jerk tuning",
        bom: [{ system:"Software", code:"LIC-PATH", name:"Path optimization license", qty:1, price:1900, children:[] }], ops: [] },
      { id: "sw-pallet", name: "Palletizing Suite", price: 2400, sub: "Pattern builder + mixed-SKU",
        bom: [{ system:"Software", code:"LIC-PAL", name:"Palletizing license", qty:1, price:2400, children:[] }], ops: [] },
      { id: "sw-weld", name: "Welding Suite", price: 2100, sub: "Seam tracking, weave control",
        bom: [{ system:"Software", code:"LIC-WELD", name:"Welding license", qty:1, price:2100, children:[] }],
        ops: [{code:"WELD-02",name:"Weld Package Commissioning",seq:93}] },
    ]},
  ],

  // Declarative constraint rules. Types: requires | requiresOneOf | excludes | formula
  rules: [
    { id: "R1", type: "requires",      when: ["ee-weld"],   then: ["sw-weld"],   msg: "MIG torch requires the Welding software suite." },
    { id: "R2", type: "requires",      when: ["ee-weld"],   then: ["pkg-dress"], msg: "MIG torch requires the Dress Pack for torch cabling." },
    { id: "R3", type: "requiresOneOf", when: ["pkg-vision"],then: ["ctl-std","ctl-perf"], msg: "Vision Package requires the SC-3 or SC-5 controller." },
    { id: "R4", type: "requires",      when: ["col-white"], then: ["pkg-clean"], msg: "Cleanroom White requires the Cleanroom Package." },
    { id: "R5", type: "requiresOneOf", when: ["sw-pallet"], then: ["ee-grip","ee-vac"], msg: "Palletizing Suite requires a gripper or vacuum effector." },
    { id: "R6", type: "requires",      when: ["arm-20"],    then: ["ctl-perf"],  msg: "The 20 kg arm requires the SC-5 High-Path controller." },
    { id: "R7", type: "requires",      when: ["pkg-track"], then: ["pkg-dress"], msg: "The 7th-axis track requires the Dress Pack cable chain." },
    { id: "R8", type: "requires",      when: ["pkg-track"], then: ["ctl-perf"],  msg: "The 7th-axis track requires the SC-5 controller (7-axis drives)." },
    { id: "X1", type: "excludes",      when: ["pkg-clean"], then: ["pkg-foundry"], msg: "Cleanroom and Foundry packages are mutually exclusive." },
    { id: "X2", type: "excludes",      when: ["mnt-ceiling"], then: ["pkg-track"], msg: "Ceiling mount excludes the 7th-axis track." },
    { id: "F1", type: "formula", expr: "has('pkg-track') ? railLen > 0 : true", msg: "7th-Axis Track needs a rail length above 0 mm.", fixAttr: { id: "railLen", value: 2000 }, refs: ["pkg-track", "railLen"] },
    { id: "F2", type: "formula", expr: "reach <= 1400 || has('arm-20')", msg: "Reach beyond 1400 mm requires the 20 kg arm class.", fixChoose: "arm-20", refs: ["reach", "arm-20"] },
  ],
};
