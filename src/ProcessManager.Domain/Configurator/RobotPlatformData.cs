namespace ProcessManager.Domain.Configurator;

/// <summary>
/// The "Sequencer RX-6" — a widely-configurable 6-axis industrial robot
/// (generic; not modeled on any vendor's product). Ported from the design
/// handoff <c>data.js</c>. Entirely data-driven: any product tree (robots, 3D
/// printers, machinery) can be expressed in the same schema. In production this
/// would be sourced from the item-master / product-model tables.
/// </summary>
public static class RobotPlatformData
{
    public static ProductPlatform Build() => new()
    {
        Platform = new PlatformMeta
        {
            Code = "SEQ-RX6",
            Name = "Sequencer RX-6",
            Tagline = "Configurable 6-Axis Industrial Robot",
            BasePrice = 28000,
        },

        Compliance = new ComplianceMeta
        {
            FormNo = "QF-BOM-001",
            Revision = "A",
            Standard = "ISO 9001:2015",
            Clauses = "§8.5.1 Production control · §8.5.2 Identification & traceability · ISO 10007 Configuration management",
            Classification = "Controlled — Confidential",
            Owner = "Manufacturing Engineering",
            Retention = "Retain 7 years",
            Approvals = new()
            {
                new() { Role = "Prepared by", Name = "", Title = "Configuration Engineer" },
                new() { Role = "Reviewed by", Name = "", Title = "Manufacturing Engineer" },
                new() { Role = "Approved by", Name = "", Title = "Engineering Manager" },
                new() { Role = "Quality release", Name = "", Title = "QA Inspector" },
            },
            RevisionHistory = new()
            {
                new() { Rev = "A", Date = "2026-07-01", Description = "Initial release from configurator", Author = "Sequencer Process Manager" },
            },
        },

        BaseBom = new()
        {
            new() { System = "Structure", Code = "BASE-001", Name = "Base & swing assembly", Qty = 1, Price = 4600, Children = new()
            {
                new() { Code = "PED-01", Name = "Pedestal casting", Qty = 1, Price = 1700 },
                new() { Code = "SWG-01", Name = "Swing bearing (axis 1)", Qty = 1, Price = 1400 },
            }},
            new() { System = "Structure", Code = "LINK-001", Name = "Arm linkage set", Qty = 1, Price = 3900, Children = new()
            {
                new() { Code = "ARM-LWR", Name = "Lower arm casting", Qty = 1, Price = 1500 },
                new() { Code = "ARM-UPR", Name = "Upper arm casting", Qty = 1, Price = 1300 },
                new() { Code = "WRST-HSG", Name = "Wrist housing", Qty = 1, Price = 1100 },
            }},
            new() { System = "Motion", Code = "DRV-001", Name = "Axis drive package", Qty = 1, Price = 0, Children = new()
            {
                new() { Code = "SRV-AX", Name = "AC servo motor", QtyExpr = "axisMotors", Price = 850 },
                new() { Code = "RED-AX", Name = "Harmonic reducer", QtyExpr = "axisMotors", Price = 1150 },
            }},
            new() { System = "Electrical", Code = "ELEC-001", Name = "Electrical base kit", Qty = 1, Price = 1900, Children = new()
            {
                new() { Code = "PND-01", Name = "Teach pendant", Qty = 1, Price = 900 },
                new() { Code = "SFC-01", Name = "Safety I/O module", Qty = 1, Price = 600 },
                new() { Code = "HARN-01", Name = "Internal harness set", Qty = 1, Price = 400 },
            }},
        },

        BaseOps = new()
        {
            new() { Code = "MACH-01", Name = "Casting Machining", Seq = 10 },
            new() { Code = "PAINT-01", Name = "Prime & Paint", Seq = 20 },
            new() { Code = "ARM-01", Name = "Arm & Joint Assembly", Seq = 30 },
            new() { Code = "WIRE-01", Name = "Harness Routing", Seq = 70 },
            new() { Code = "CAL-01", Name = "Axis Calibration", Seq = 90 },
            new() { Code = "QA-01", Name = "End-of-Line Test", Seq = 95 },
        },

        Attributes = new()
        {
            new() { Id = "reach", Name = "Working reach", Unit = "mm", Kind = "range", Min = 600, Max = 2200, Step = 100, Default = 1300,
                Hint = "Arm reach at full extension. Drives dress-pack segments; beyond 1400 mm requires the 20 kg arm." },
            new() { Id = "railLen", Name = "7th-axis rail length", Unit = "mm", Kind = "range", Min = 0, Max = 6000, Step = 500, Default = 0,
                Hint = "0 = no track. Rail segments are computed as ceil(length ÷ 1000); a fitted track adds a 7th servo axis." },
            new() { Id = "orderQty", Name = "Order quantity", Unit = "robots", Kind = "range", Min = 1, Max = 25, Step = 1, Default = 1,
                Hint = "Robots built by this work order. Multiplies the order-level rollup." },
        },

        Calc = new()
        {
            new() { Id = "axisMotors", Name = "Servo axes", Expr = "6 + (railLen > 0 ? 1 : 0)", Unit = "", Hint = "Six arm axes, plus the rail axis when a track is fitted." },
            new() { Id = "railSegs", Name = "Rail segments", Expr = "railLen > 0 ? ceil(railLen / 1000) : 0", Unit = "", Hint = "ceil(rail length ÷ 1000 mm)." },
            new() { Id = "dressSegs", Name = "Dress-pack segments", Expr = "ceil(reach / 700)", Unit = "", Hint = "Cable dress segments scale with reach: ceil(reach ÷ 700 mm)." },
        },

        Groups = new()
        {
            new() { Id = "arm", Name = "Arm Class", Icon = "bi-robot", Multi = false, Required = true,
                Hint = "Payload capacity class — castings, wrist and counterbalance.", Options = new()
            {
                new() { Id = "arm-5", Name = "RX-6/5 · 5 kg", Price = 0, Sub = "Light handling & assembly", Badge = "5 KG",
                    Bom = new() { new() { System = "Structure", Code = "ARM-C5", Name = "5 kg arm kit", Qty = 1, Price = 3200, Children = new()
                        { new() { Code = "WRST-5", Name = "Wrist unit (5 kg)", Qty = 1, Price = 1100 } } } } },
                new() { Id = "arm-10", Name = "RX-6/10 · 10 kg", Price = 5800, Sub = "General purpose", Badge = "10 KG",
                    Bom = new() { new() { System = "Structure", Code = "ARM-C10", Name = "10 kg arm kit", Qty = 1, Price = 5100, Children = new()
                        { new() { Code = "WRST-10", Name = "Wrist unit (10 kg)", Qty = 1, Price = 1600 },
                          new() { Code = "ELB-R", Name = "Reinforced elbow", Qty = 1, Price = 700 } } } } },
                new() { Id = "arm-20", Name = "RX-6/20 · 20 kg", Price = 12500, Sub = "Heavy payload / long reach", Badge = "20 KG",
                    Bom = new() { new() { System = "Structure", Code = "ARM-C20", Name = "20 kg arm kit", Qty = 1, Price = 8400, Children = new()
                        { new() { Code = "WRST-20", Name = "Wrist unit (20 kg)", Qty = 1, Price = 2300 },
                          new() { Code = "CBAL-1", Name = "Counterbalance cylinder", Qty = 2, Price = 650 } } } },
                    Ops = new() { new() { Code = "CBAL-01", Name = "Counterbalance Fit", Seq = 35 } } },
            }},

            new() { Id = "controller", Name = "Controller", Icon = "bi-cpu", Multi = false, Required = true,
                Hint = "Cabinet, drives and path computer.", Options = new()
            {
                new() { Id = "ctl-compact", Name = "SC-1 Compact", Price = 0, Sub = "Cell-mount cabinet",
                    Bom = new() { new() { System = "Electrical", Code = "CTL-SC1", Name = "SC-1 compact cabinet", Qty = 1, Price = 2600, Children = new()
                        { new() { Code = "PSU-1", Name = "Drive PSU", Qty = 1, Price = 600 },
                          new() { Code = "IO-16", Name = "I/O board (16ch)", Qty = 1, Price = 300 } } } },
                    Ops = new() { new() { Code = "CTRL-01", Name = "Controller Marriage", Seq = 60 } } },
                new() { Id = "ctl-std", Name = "SC-3 Standard", Price = 3400, Sub = "Floor cabinet, expandable I/O",
                    Bom = new() { new() { System = "Electrical", Code = "CTL-SC3", Name = "SC-3 standard cabinet", Qty = 1, Price = 4200, Children = new()
                        { new() { Code = "PSU-2", Name = "Drive PSU (heavy)", Qty = 1, Price = 900 },
                          new() { Code = "IO-64", Name = "I/O rack (64ch)", Qty = 1, Price = 700 } } } },
                    Ops = new() { new() { Code = "CTRL-01", Name = "Controller Marriage", Seq = 60 } } },
                new() { Id = "ctl-perf", Name = "SC-5 High-Path", Price = 8200, Sub = "Path co-processor, 7-axis ready",
                    Bom = new() { new() { System = "Electrical", Code = "CTL-SC5", Name = "SC-5 high-path cabinet", Qty = 1, Price = 7400, Children = new()
                        { new() { Code = "PSU-3", Name = "Drive PSU (perf)", Qty = 1, Price = 1100 },
                          new() { Code = "CPU-P", Name = "Path co-processor", Qty = 1, Price = 1800 },
                          new() { Code = "IO-64", Name = "I/O rack (64ch)", Qty = 1, Price = 700 } } } },
                    Ops = new() { new() { Code = "CTRL-01", Name = "Controller Marriage", Seq = 60 } } },
            }},

            new() { Id = "mount", Name = "Mounting", Icon = "bi-arrows-move", Multi = false, Required = true,
                Hint = "How the robot is installed in the cell.", Options = new()
            {
                new() { Id = "mnt-floor", Name = "Floor Mount", Price = 0, Sub = "Standard baseplate",
                    Bom = new() { new() { System = "Structure", Code = "MNT-FL", Name = "Floor baseplate kit", Qty = 1, Price = 450 } } },
                new() { Id = "mnt-wall", Name = "Wall Mount", Price = 600, Sub = "Side bracket + shimming",
                    Bom = new() { new() { System = "Structure", Code = "MNT-WL", Name = "Wall bracket kit", Qty = 1, Price = 900 } },
                    Ops = new() { new() { Code = "MNT-02", Name = "Bracket Prep & Shim", Seq = 15 } } },
                new() { Id = "mnt-ceiling", Name = "Ceiling Mount", Price = 900, Sub = "Inverted kit, drip protection",
                    Bom = new() { new() { System = "Structure", Code = "MNT-CL", Name = "Inverted-mount kit", Qty = 1, Price = 1200, Children = new()
                        { new() { Code = "DRIP-1", Name = "Drip shield", Qty = 1, Price = 250 } } } },
                    Ops = new() { new() { Code = "MNT-02", Name = "Inverted-Mount Prep", Seq = 15 } } },
            }},

            new() { Id = "effector", Name = "End Effector", Icon = "bi-magic", Multi = false, Required = true,
                Hint = "Tooling on the wrist flange.", Options = new()
            {
                new() { Id = "ee-none", Name = "Customer-Supplied", Price = 0, Sub = "Bare ISO 9409 flange", Ee = "none" },
                new() { Id = "ee-grip", Name = "2-Finger Servo Gripper", Price = 2800, Sub = "Parallel, force-controlled", Ee = "grip",
                    Bom = new() { new() { System = "Tooling", Code = "EE-GRIP", Name = "Servo gripper", Qty = 1, Price = 2400, Children = new()
                        { new() { Code = "FING-2", Name = "Finger set", Qty = 2, Price = 180 },
                          new() { Code = "SRV-G", Name = "Gripper servo", Qty = 1, Price = 600 } } } } },
                new() { Id = "ee-vac", Name = "Vacuum Plate (4-cup)", Price = 2200, Sub = "Venturi vacuum, foam seal", Ee = "vac",
                    Bom = new() { new() { System = "Tooling", Code = "EE-VAC", Name = "Vacuum plate", Qty = 1, Price = 1500, Children = new()
                        { new() { Code = "CUP-4", Name = "Suction cup", Qty = 4, Price = 60 },
                          new() { Code = "VENT-1", Name = "Venturi generator", Qty = 1, Price = 420 } } } } },
                new() { Id = "ee-weld", Name = "MIG Weld Torch", Price = 4800, Sub = "Water-cooled, wire feeder", Ee = "weld",
                    Bom = new() { new() { System = "Tooling", Code = "EE-WELD", Name = "MIG torch package", Qty = 1, Price = 3900, Children = new()
                        { new() { Code = "TRCH-1", Name = "Water-cooled torch", Qty = 1, Price = 1600 },
                          new() { Code = "FEED-1", Name = "Wire feeder", Qty = 1, Price = 1300 } } } },
                    Ops = new() { new() { Code = "WELD-01", Name = "Torch Fit & Purge Test", Seq = 65 } } },
            }},

            new() { Id = "color", Name = "Finish", Icon = "bi-palette", Multi = false, Required = true,
                Hint = "Paint finish.", Options = new()
            {
                new() { Id = "col-orange", Name = "Safety Orange", Price = 0, Swatch = "#e95b15",
                    Bom = new() { new() { System = "Structure", Code = "PNT-ORG", Name = "Paint — Safety Orange", Qty = 1, Price = 250 } } },
                new() { Id = "col-graphite", Name = "Graphite", Price = 400, Swatch = "#3d3d3d",
                    Bom = new() { new() { System = "Structure", Code = "PNT-GPH", Name = "Paint — Graphite", Qty = 1, Price = 400 } } },
                new() { Id = "col-white", Name = "Cleanroom White", Price = 900, Swatch = "#eef0f2",
                    Bom = new() { new() { System = "Structure", Code = "PNT-WHT", Name = "Paint — Cleanroom White (low-particle)", Qty = 1, Price = 900 } } },
                new() { Id = "col-gold", Name = "Heritage Gold", Price = 1200, Swatch = "#f1c40f",
                    Bom = new() { new() { System = "Structure", Code = "PNT-GLD", Name = "Paint — Heritage Gold (signature)", Qty = 1, Price = 1200 } },
                    Ops = new() { new() { Code = "PAINT-02", Name = "Signature Paint Cell", Seq = 22 } } },
            }},

            new() { Id = "packages", Name = "Packages", Icon = "bi-box-seam", Multi = true, Required = false,
                Hint = "Bundled hardware options.", Options = new()
            {
                new() { Id = "pkg-vision", Name = "Vision Package", Price = 5500, Sub = "2D cameras, lighting, GPU",
                    Bom = new() { new() { System = "Electrical", Code = "PKG-VIS", Name = "Vision kit", Qty = 1, Price = 4800, Children = new()
                        { new() { Code = "CAM-2D", Name = "2D camera", Qty = 2, Price = 1200 },
                          new() { Code = "LGT-BAR", Name = "LED light bar", Qty = 2, Price = 300 },
                          new() { Code = "GPU-1", Name = "Vision GPU module", Qty = 1, Price = 1800 } } } },
                    Ops = new() { new() { Code = "VIS-01", Name = "Vision Calibration", Seq = 92 } } },
                new() { Id = "pkg-track", Name = "7th-Axis Track", PriceExpr = "2400 + 1.5 * railLen", Price = 2400, Sub = "Carriage + rail, priced per mm",
                    Bom = new() { new() { System = "Motion", Code = "PKG-TRK", Name = "Linear track system", Qty = 1, Price = 1800, Children = new()
                        { new() { Code = "TRK-CAR", Name = "Track carriage", Qty = 1, Price = 1800 },
                          new() { Code = "RAIL-SEG", Name = "Rail segment (1 m)", QtyExpr = "railSegs", Price = 650 } } } },
                    Ops = new() { new() { Code = "TRK-01", Name = "Track Install & Align", Seq = 12 } } },
                new() { Id = "pkg-dress", Name = "Dress Pack", Price = 900, Sub = "Cable chain, segments scale with reach",
                    Bom = new() { new() { System = "Electrical", Code = "PKG-DRS", Name = "Dress-pack kit", Qty = 1, Price = 400, Children = new()
                        { new() { Code = "DRS-SEG", Name = "Dress segment", QtyExpr = "dressSegs", Price = 180 } } } } },
                new() { Id = "pkg-clean", Name = "Cleanroom Package", Price = 3200, Sub = "ISO 5 — sealed bellows, LP grease",
                    Bom = new() { new() { System = "Structure", Code = "PKG-CLN", Name = "Cleanroom kit", Qty = 1, Price = 3000, Children = new()
                        { new() { Code = "BELW-1", Name = "Sealed bellows set", Qty = 1, Price = 1400 },
                          new() { Code = "GRS-LP", Name = "Low-particle grease service", Qty = 1, Price = 600 } } } } },
                new() { Id = "pkg-foundry", Name = "Foundry Package", Price = 2600, Sub = "IP67 — heat jacket, sealed connectors",
                    Bom = new() { new() { System = "Structure", Code = "PKG-FDY", Name = "Foundry protection kit", Qty = 1, Price = 2400, Children = new()
                        { new() { Code = "JKT-HT", Name = "Heat-resist jacket", Qty = 1, Price = 1200 },
                          new() { Code = "CONN-67", Name = "IP67 connector set", Qty = 1, Price = 700 } } } } },
            }},

            new() { Id = "software", Name = "Software", Icon = "bi-code-square", Multi = true, Required = false,
                Hint = "Licensed application suites.", Options = new()
            {
                new() { Id = "sw-path", Name = "Path Optimization Suite", Price = 1900, Sub = "Cycle-time & jerk tuning",
                    Bom = new() { new() { System = "Software", Code = "LIC-PATH", Name = "Path optimization license", Qty = 1, Price = 1900 } } },
                new() { Id = "sw-pallet", Name = "Palletizing Suite", Price = 2400, Sub = "Pattern builder + mixed-SKU",
                    Bom = new() { new() { System = "Software", Code = "LIC-PAL", Name = "Palletizing license", Qty = 1, Price = 2400 } } },
                new() { Id = "sw-weld", Name = "Welding Suite", Price = 2100, Sub = "Seam tracking, weave control",
                    Bom = new() { new() { System = "Software", Code = "LIC-WELD", Name = "Welding license", Qty = 1, Price = 2100 } },
                    Ops = new() { new() { Code = "WELD-02", Name = "Weld Package Commissioning", Seq = 93 } } },
            }},
        },

        Rules = new()
        {
            new() { Id = "R1", Type = "requires", When = new() { "ee-weld" }, Then = new() { "sw-weld" }, Msg = "MIG torch requires the Welding software suite." },
            new() { Id = "R2", Type = "requires", When = new() { "ee-weld" }, Then = new() { "pkg-dress" }, Msg = "MIG torch requires the Dress Pack for torch cabling." },
            new() { Id = "R3", Type = "requiresOneOf", When = new() { "pkg-vision" }, Then = new() { "ctl-std", "ctl-perf" }, Msg = "Vision Package requires the SC-3 or SC-5 controller." },
            new() { Id = "R4", Type = "requires", When = new() { "col-white" }, Then = new() { "pkg-clean" }, Msg = "Cleanroom White requires the Cleanroom Package." },
            new() { Id = "R5", Type = "requiresOneOf", When = new() { "sw-pallet" }, Then = new() { "ee-grip", "ee-vac" }, Msg = "Palletizing Suite requires a gripper or vacuum effector." },
            new() { Id = "R6", Type = "requires", When = new() { "arm-20" }, Then = new() { "ctl-perf" }, Msg = "The 20 kg arm requires the SC-5 High-Path controller." },
            new() { Id = "R7", Type = "requires", When = new() { "pkg-track" }, Then = new() { "pkg-dress" }, Msg = "The 7th-axis track requires the Dress Pack cable chain." },
            new() { Id = "R8", Type = "requires", When = new() { "pkg-track" }, Then = new() { "ctl-perf" }, Msg = "The 7th-axis track requires the SC-5 controller (7-axis drives)." },
            new() { Id = "X1", Type = "excludes", When = new() { "pkg-clean" }, Then = new() { "pkg-foundry" }, Msg = "Cleanroom and Foundry packages are mutually exclusive." },
            new() { Id = "X2", Type = "excludes", When = new() { "mnt-ceiling" }, Then = new() { "pkg-track" }, Msg = "Ceiling mount excludes the 7th-axis track." },
            new() { Id = "F1", Type = "formula", Expr = "has('pkg-track') ? railLen > 0 : true", Msg = "7th-Axis Track needs a rail length above 0 mm.",
                FixAttr = new() { Id = "railLen", Value = 2000 }, Refs = new() { "pkg-track", "railLen" } },
            new() { Id = "F2", Type = "formula", Expr = "reach <= 1400 || has('arm-20')", Msg = "Reach beyond 1400 mm requires the 20 kg arm class.",
                FixChoose = "arm-20", Refs = new() { "reach", "arm-20" } },
        },
    };
}
