using System.Globalization;
using System.Net;
using System.Text;
using ProcessManager.Domain.Configurator;

namespace ProcessManager.Web.Components.Pages.Configurator;

/// <summary>
/// Builds the ISO 9001:2015 / ISO 10007 controlled-document spec sheet as a
/// standalone, printable HTML document (Letter, 14 mm margins). Rendered off the
/// live app into a print iframe so it prints cleanly regardless of app layout.
/// The document id is <c>formNo/PLATFORM-configHash</c> — an identical config
/// yields an identical id (ISO 10007 traceability).
/// </summary>
public static class SpecSheetHtml
{
    public static string Build(ExportSpec spec, string workOrder)
    {
        var m = spec.Meta;
        var comp = m.Compliance;
        var sb = new StringBuilder();
        var issueDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.Append("<title>").Append(E(m.DocNo)).Append("</title>");
        sb.Append("<style>").Append(Css).Append("</style></head><body>");
        sb.Append("<div class='sheet'>");

        // 1 ── Controlled-document header ────────────────────────────────────────
        sb.Append("<div class='doc-head'>");
        sb.Append("<div class='doc-title'><div class='dt-main'>Manufacturing Configuration &amp; Bill of Materials</div>")
          .Append("<div class='dt-sub'>").Append(E(m.Platform.Name)).Append(" &middot; ").Append(E(m.Platform.Tagline)).Append("</div></div>");
        sb.Append("<table class='cd-grid'><tr>")
          .Append(Cell("Document No.", m.DocNo))
          .Append(Cell("Form", comp.FormNo))
          .Append(Cell("Revision", comp.Revision))
          .Append(Cell("Standard", comp.Standard))
          .Append("</tr><tr>")
          .Append(Cell("Work Order", string.IsNullOrWhiteSpace(workOrder) ? "—" : workOrder))
          .Append(Cell("Config Hash", m.ConfigHash))
          .Append(Cell("Issue Date", issueDate))
          .Append(Cell("Page", "1 of 1"))
          .Append("</tr></table>");
        sb.Append("<div class='class-bar'><span class='cls'>").Append(E(comp.Classification)).Append("</span>")
          .Append("<span class='clauses'>").Append(E(comp.Clauses)).Append("</span></div>");
        sb.Append("</div>");

        // 2 ── Platform summary + configuration ──────────────────────────────────
        sb.Append("<div class='band'><span>Platform</span><b>").Append(E(m.Platform.Code)).Append("</b> — ")
          .Append(E(m.Platform.Name)).Append("<span class='band-r'>Order qty: <b>").Append(m.OrderQty)
          .Append("</b> &middot; Base price ").Append(Money(m.BasePrice)).Append("</span></div>");

        sb.Append("<h3>Configuration</h3><div class='two-col'>");
        foreach (var s in spec.Selections)
            sb.Append("<div class='cfg-row'><span>").Append(E(s.Group)).Append("</span><b>").Append(E(s.Name))
              .Append("</b><span class='cfg-price'>").Append(s.UnitPrice > 0 ? Money(s.UnitPrice) : "Incl.").Append("</span></div>");
        sb.Append("</div>");

        // Attributes + calculated
        sb.Append("<div class='split'>");
        sb.Append("<div><h3>Attributes</h3><table class='mini'>");
        foreach (var a in spec.Attributes)
        {
            var bounds = a.Bounds != null
                ? $" [{Num(a.Bounds.Min)}–{Num(a.Bounds.Max)}{(a.Bounds.ClampedBy.Count > 0 ? " · " + string.Join(",", a.Bounds.ClampedBy) : "")}]"
                : "";
            sb.Append("<tr><td>").Append(E(a.Name)).Append("</td><td class='num'>").Append(Num(a.Value)).Append(' ').Append(E(a.Unit))
              .Append("</td><td class='dim'>").Append(E(bounds)).Append("</td></tr>");
        }
        sb.Append("</table></div>");
        sb.Append("<div><h3>Calculated</h3><table class='mini'>");
        foreach (var c in spec.Calculated)
            sb.Append("<tr><td>").Append(E(c.Name)).Append("</td><td class='num'>").Append(Num(c.Value))
              .Append("</td><td class='dim mono'>").Append(E(c.Expr)).Append("</td></tr>");
        sb.Append("</table></div></div>");

        // Full BoM table
        sb.Append("<h3>Bill of Materials</h3><table class='bom'><thead><tr>")
          .Append("<th>Lvl</th><th>System</th><th>Source</th><th>Code</th><th>Part</th><th class='num'>Qty</th><th class='num'>Unit</th><th class='num'>Ext</th></tr></thead><tbody>");
        foreach (var r in spec.Bom.Rows)
            sb.Append("<tr class='lvl").Append(r.Level).Append("'><td>").Append(r.Level).Append("</td><td>").Append(E(r.System))
              .Append("</td><td class='dim'>").Append(E(r.Source)).Append("</td><td class='mono'>").Append(E(r.Code))
              .Append("</td><td>").Append(E(r.Name))
              .Append("</td><td class='num'>").Append(r.QtyFormula != null ? "ƒ " : "").Append(r.Qty)
              .Append("</td><td class='num'>").Append(Money(r.UnitPrice))
              .Append("</td><td class='num'>").Append(Money(r.Ext)).Append("</td></tr>");
        sb.Append("</tbody></table>");
        sb.Append("<div class='rollup'>Part count: <b>").Append(spec.Bom.PartCount).Append("</b> &middot; Per-unit MSRP: <b>")
          .Append(Money(spec.Price.PerUnit)).Append("</b> &middot; Order total (×").Append(spec.Price.OrderQty).Append("): <b>")
          .Append(Money(spec.Price.OrderTotal)).Append("</b></div>");

        // 3 ── Routing & Sequence Clearance Record ───────────────────────────────
        sb.Append("<h3>Routing &amp; Sequence Clearance Record</h3>");
        sb.Append("<div class='iso-note'>Per ISO 9001:2015 §8.5.1 (production control) and §8.5.2 (identification &amp; traceability): each operation is cleared in sequence. A skipped stamp is a nonconformance (§8.7).</div>");
        sb.Append("<table class='route'><thead><tr><th>Seq</th><th>Op</th><th>Operation</th><th>Operator &amp; Date</th><th>Stamp</th></tr></thead><tbody>");
        foreach (var op in spec.Routing)
            sb.Append("<tr><td><span class='seq'>").Append(op.Step).Append("</span></td><td class='mono'>").Append(E(op.Code))
              .Append("</td><td>").Append(E(op.Name))
              .Append("</td><td class='sig'><span class='line'></span><span class='dl'>Date</span></td>")
              .Append("<td><span class='stamp'>Stamp ").Append(op.Step.ToString("00")).Append("</span></td></tr>");
        sb.Append("</tbody></table>");

        // 4 ── Authorization & Approval ──────────────────────────────────────────
        sb.Append("<h3>Authorization &amp; Approval</h3><div class='appr-grid'>");
        foreach (var a in comp.Approvals)
            sb.Append("<div class='appr'><div class='appr-role'>").Append(E(a.Role)).Append("</div>")
              .Append("<div class='appr-line'></div><div class='appr-title'>").Append(E(a.Title)).Append("</div>")
              .Append("<div class='appr-stamp'>Stamp / Seal</div></div>");
        sb.Append("</div>");

        sb.Append("<div class='disp'><div class='disp-checks'>")
          .Append("<div class='dt'>Quality Disposition</div>")
          .Append("<label><span class='cb'></span> Accepted</label>")
          .Append("<label><span class='cb'></span> Accepted with deviation</label>")
          .Append("<label><span class='cb'></span> Rejected — see NCR</label></div>")
          .Append("<div class='qa-ring'><span>QUALITY<br/>APPROVED</span></div></div>");

        // 5 ── Revision history + footer ─────────────────────────────────────────
        sb.Append("<h3>Revision History</h3><table class='mini rev'><thead><tr><th>Rev</th><th>Date</th><th>Description</th><th>Author</th></tr></thead><tbody>");
        foreach (var rh in comp.RevisionHistory)
            sb.Append("<tr><td>").Append(E(rh.Rev)).Append("</td><td>").Append(E(rh.Date)).Append("</td><td>")
              .Append(E(rh.Description)).Append("</td><td>").Append(E(rh.Author)).Append("</td></tr>");
        sb.Append("</tbody></table>");

        sb.Append("<div class='footer'>UNCONTROLLED WHEN PRINTED &middot; ").Append(E(comp.Owner))
          .Append(" &middot; ").Append(E(comp.Retention)).Append(" &middot; ").Append(E(m.DocNo)).Append("</div>");

        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    private static string Cell(string k, string v) =>
        "<td><div class='k'>" + E(k) + "</div><div class='v'>" + E(v) + "</div></td>";

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
    private static string Num(double v) => ConfiguratorEngine.Fmt(v);
    private static string Money(double v) => "$" + Math.Round(v).ToString("#,##0", CultureInfo.InvariantCulture);

    private const string Css = @"
@page { size: Letter; margin: 14mm; }
* { box-sizing: border-box; }
body { font-family: 'Mulish', Arial, sans-serif; color: #212121; font-size: 10.5px; margin: 0; }
.sheet { max-width: 190mm; margin: 0 auto; }
h3 { font-family: 'Coda', Arial, sans-serif; font-size: 12px; text-transform: uppercase; letter-spacing: .5px; border-bottom: 1.5px solid #212121; padding-bottom: 3px; margin: 14px 0 7px; }
.doc-head { border: 1.5px solid #212121; }
.doc-title { padding: 8px 12px; border-bottom: 1px solid #212121; }
.dt-main { font-family: 'Coda', Arial, sans-serif; font-weight: 800; font-size: 15px; }
.dt-sub { color: #555; font-size: 10px; }
.cd-grid { width: 100%; border-collapse: collapse; }
.cd-grid td { border: 1px solid #212121; padding: 4px 8px; width: 25%; }
.cd-grid .k { font-size: 8px; text-transform: uppercase; letter-spacing: .4px; color: #666; }
.cd-grid .v { font-family: 'IBM Plex Mono', monospace; font-weight: 600; font-size: 11px; }
.class-bar { background: #111; color: #fff; padding: 5px 10px; display: flex; justify-content: space-between; gap: 12px; }
.class-bar .cls { font-weight: 700; text-transform: uppercase; letter-spacing: .5px; font-size: 10px; }
.class-bar .clauses { font-size: 8.5px; color: #ccc; text-align: right; }
.band { background: #f0f0f0; border: 1px solid #d4d4d4; padding: 6px 10px; margin-top: 12px; font-size: 11px; }
.band span:first-child { color: #666; text-transform: uppercase; font-size: 8px; letter-spacing: .5px; margin-right: 6px; }
.band .band-r { float: right; color: #444; }
.two-col { columns: 2; column-gap: 20px; }
.cfg-row { display: flex; justify-content: space-between; gap: 8px; padding: 2px 0; break-inside: avoid; border-bottom: 1px dotted #ddd; }
.cfg-row span:first-child { color: #777; }
.cfg-row .cfg-price { font-family: 'IBM Plex Mono', monospace; color: #333; }
.split { display: flex; gap: 20px; }
.split > div { flex: 1; }
table.mini { width: 100%; border-collapse: collapse; }
table.mini td { padding: 2px 5px; border-bottom: 1px solid #eee; }
.num { font-family: 'IBM Plex Mono', monospace; text-align: right; }
.dim { color: #888; font-size: 9px; }
.mono { font-family: 'IBM Plex Mono', monospace; }
table.bom { width: 100%; border-collapse: collapse; }
table.bom th { background: #212121; color: #fff; padding: 4px 6px; text-align: left; font-size: 8.5px; text-transform: uppercase; }
table.bom th.num { text-align: right; }
table.bom td { padding: 3px 6px; border-bottom: 1px solid #eee; }
table.bom tr.lvl2 td:nth-child(5) { padding-left: 18px; color: #555; }
.rollup { margin-top: 6px; text-align: right; font-size: 11px; }
.iso-note { font-size: 9px; color: #555; background: #f7f7f7; border-left: 3px solid #212121; padding: 5px 9px; margin-bottom: 6px; }
table.route { width: 100%; border-collapse: collapse; }
table.route th { text-align: left; font-size: 8.5px; text-transform: uppercase; color: #666; border-bottom: 1.5px solid #212121; padding: 4px 6px; }
table.route td { padding: 7px 6px; border-bottom: 1px solid #ddd; vertical-align: middle; }
.seq { display: inline-flex; width: 20px; height: 20px; border-radius: 50%; background: #212121; color: #fff; align-items: center; justify-content: center; font-family: 'IBM Plex Mono', monospace; font-size: 10px; }
.sig .line { display: block; border-bottom: 1px solid #999; width: 120px; margin-bottom: 2px; }
.sig .dl { font-size: 8px; color: #999; }
.stamp { display: inline-block; border: 1px dashed #999; border-radius: 4px; padding: 8px 14px; font-size: 8px; color: #999; }
.appr-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
.appr { border: 1px solid #d4d4d4; padding: 8px; }
.appr-role { font-weight: 700; font-size: 9px; text-transform: uppercase; letter-spacing: .3px; }
.appr-line { border-bottom: 1px solid #999; margin: 22px 0 3px; }
.appr-title { font-size: 8.5px; color: #666; }
.appr-stamp { margin-top: 8px; border: 1px dashed #bbb; border-radius: 4px; padding: 10px 0; text-align: center; font-size: 8px; color: #aaa; }
.disp { display: flex; justify-content: space-between; align-items: center; margin-top: 12px; }
.disp .dt { font-weight: 700; font-size: 9px; text-transform: uppercase; margin-bottom: 4px; }
.disp label { display: block; font-size: 10px; margin: 3px 0; }
.disp .cb { display: inline-block; width: 11px; height: 11px; border: 1.2px solid #212121; margin-right: 6px; vertical-align: -1px; }
.qa-ring { width: 78px; height: 78px; border: 2.5px solid #1da086; border-radius: 50%; display: flex; align-items: center; justify-content: center; text-align: center; color: #1da086; font-weight: 800; font-size: 9px; letter-spacing: .5px; transform: rotate(-8deg); }
table.rev th { text-align: left; font-size: 8.5px; text-transform: uppercase; color: #666; border-bottom: 1px solid #212121; padding: 3px 5px; }
.footer { margin-top: 16px; border-top: 1px solid #212121; padding-top: 5px; text-align: center; font-size: 8.5px; letter-spacing: .5px; color: #777; text-transform: uppercase; }
";
}
