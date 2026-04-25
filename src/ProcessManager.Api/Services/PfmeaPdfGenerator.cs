using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProcessManager.Api.Services;

public static class PfmeaPdfGenerator
{
    public static byte[] Generate(PfmeaResponseDto pfmea, TenantBranding? branding)
    {
        var isDraft = pfmea.IsStale;
        var companyName = branding?.CompanyName ?? "Process Manager";
        var footerText = branding?.FooterText;
        var primaryColor = branding?.PrimaryColorHex ?? "#1a56db";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(25);
                page.MarginVertical(20);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Element(header =>
                    ComposeHeader(header, pfmea, companyName, primaryColor));

                page.Content().Element(content =>
                    ComposeContent(content, pfmea, isDraft));

                page.Footer().Element(footer =>
                    ComposeFooter(footer, pfmea, companyName, footerText));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, PfmeaResponseDto pfmea,
        string companyName, string primaryColor)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(companyName).Bold().FontSize(12);
                    left.Item().Text("Process Failure Mode and Effects Analysis (PFMEA)")
                        .Bold().FontSize(10).FontColor(primaryColor);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text($"Document: {pfmea.Code}").Bold().FontSize(8);
                    right.Item().Text($"Version: {pfmea.Version}").FontSize(8);
                    right.Item().Text($"Date: {pfmea.UpdatedAt:yyyy-MM-dd}").FontSize(8);
                });
            });

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Process: {pfmea.ProcessName} ({pfmea.ProcessCode})")
                    .FontSize(8);
                row.RelativeItem().Text($"PFMEA: {pfmea.Name}").FontSize(8);
                row.RelativeItem().AlignRight()
                    .Text($"Status: {(pfmea.IsActive ? "Active" : "Inactive")}").FontSize(8);
            });

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            col.Item().PaddingBottom(4);
        });
    }

    private static void ComposeContent(IContainer container, PfmeaResponseDto pfmea, bool isDraft)
    {
        container.Column(col =>
        {
            if (isDraft)
            {
                col.Item().PaddingBottom(4).Background("#fff3cd").Padding(4)
                    .Text("DRAFT — This PFMEA is stale and may not reflect the current process version.")
                    .FontSize(8).Bold().FontColor("#856404");
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);  // Step
                    columns.RelativeColumn(1.2f);  // Function
                    columns.RelativeColumn(1.4f);  // Failure Mode
                    columns.RelativeColumn(1.4f);  // Failure Effect
                    columns.ConstantColumn(20);     // S
                    columns.RelativeColumn(1.2f);  // Cause
                    columns.ConstantColumn(20);     // O
                    columns.RelativeColumn(1.2f);  // Prevention
                    columns.RelativeColumn(1.2f);  // Detection
                    columns.ConstantColumn(20);     // D
                    columns.ConstantColumn(30);     // RPN
                    columns.RelativeColumn(1.3f);  // Recommended Action
                    columns.RelativeColumn(0.8f);  // Responsible
                    columns.ConstantColumn(45);     // Target Date
                    columns.RelativeColumn(1.0f);  // Actions Taken
                    columns.ConstantColumn(20);     // New O
                    columns.ConstantColumn(20);     // New D
                    columns.ConstantColumn(30);     // New RPN
                });

                table.Header(header =>
                {
                    var headerStyle = TextStyle.Default.FontSize(6).Bold()
                        .FontColor(Colors.White);

                    void HeaderCell(IContainer c, string text) =>
                        c.Background("#374151").Padding(2).AlignCenter()
                            .Text(text).Style(headerStyle);

                    header.Cell().Element(c => HeaderCell(c, "Process Step"));
                    header.Cell().Element(c => HeaderCell(c, "Function"));
                    header.Cell().Element(c => HeaderCell(c, "Failure Mode"));
                    header.Cell().Element(c => HeaderCell(c, "Failure Effect"));
                    header.Cell().Element(c => HeaderCell(c, "S"));
                    header.Cell().Element(c => HeaderCell(c, "Cause"));
                    header.Cell().Element(c => HeaderCell(c, "O"));
                    header.Cell().Element(c => HeaderCell(c, "Prevention Controls"));
                    header.Cell().Element(c => HeaderCell(c, "Detection Controls"));
                    header.Cell().Element(c => HeaderCell(c, "D"));
                    header.Cell().Element(c => HeaderCell(c, "RPN"));
                    header.Cell().Element(c => HeaderCell(c, "Recommended Action"));
                    header.Cell().Element(c => HeaderCell(c, "Responsible"));
                    header.Cell().Element(c => HeaderCell(c, "Target Date"));
                    header.Cell().Element(c => HeaderCell(c, "Actions Taken"));
                    header.Cell().Element(c => HeaderCell(c, "New O"));
                    header.Cell().Element(c => HeaderCell(c, "New D"));
                    header.Cell().Element(c => HeaderCell(c, "New RPN"));
                });

                var rowIndex = 0;
                foreach (var fm in pfmea.FailureModes)
                {
                    var bgColor = rowIndex % 2 == 0 ? "#ffffff" : "#f9fafb";
                    var rpnColor = fm.Rpn >= 200 ? "#dc2626" : fm.Rpn >= 100 ? "#d97706" : "#16a34a";

                    var action = fm.Actions.FirstOrDefault();
                    var actionDesc = action?.Description ?? "";
                    var actionResp = action?.ResponsiblePerson ?? "";
                    var actionDate = action?.TargetDate?.ToString("yyyy-MM-dd") ?? "";
                    var actionTaken = action is { Status: "Completed" }
                        ? action.CompletionNotes ?? "Completed"
                        : action?.Status ?? "";
                    var newO = action?.RevisedOccurrence?.ToString() ?? "";
                    var newD = action?.RevisedDetection?.ToString() ?? "";
                    var newRpn = action?.RevisedRpn?.ToString() ?? "";

                    void DataCell(IContainer c, string text, string? color = null) =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).Text(text).FontSize(6)
                            .FontColor(color ?? Colors.Black);

                    table.Cell().Element(c => DataCell(c, fm.ProcessStepName));
                    table.Cell().Element(c => DataCell(c, fm.StepFunction));
                    table.Cell().Element(c => DataCell(c, fm.FailureMode));
                    table.Cell().Element(c => DataCell(c, fm.FailureEffect));
                    table.Cell().Element(c =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).AlignCenter().Text(fm.Severity.ToString()).FontSize(6).Bold());
                    table.Cell().Element(c => DataCell(c, fm.FailureCause ?? ""));
                    table.Cell().Element(c =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).AlignCenter().Text(fm.Occurrence.ToString()).FontSize(6));
                    table.Cell().Element(c => DataCell(c, fm.PreventionControls ?? ""));
                    table.Cell().Element(c => DataCell(c, fm.DetectionControls ?? ""));
                    table.Cell().Element(c =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).AlignCenter().Text(fm.Detection.ToString()).FontSize(6));
                    table.Cell().Element(c =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).AlignCenter().Text(fm.Rpn.ToString()).FontSize(7).Bold()
                            .FontColor(rpnColor));
                    table.Cell().Element(c => DataCell(c, actionDesc));
                    table.Cell().Element(c => DataCell(c, actionResp));
                    table.Cell().Element(c => DataCell(c, actionDate));
                    table.Cell().Element(c => DataCell(c, actionTaken));
                    table.Cell().Element(c =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).AlignCenter().Text(newO).FontSize(6));
                    table.Cell().Element(c =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).AlignCenter().Text(newD).FontSize(6));
                    table.Cell().Element(c =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).AlignCenter().Text(newRpn).FontSize(6).Bold());

                    // Additional action rows for failure modes with multiple actions
                    foreach (var extraAction in fm.Actions.Skip(1))
                    {
                        var extraBg = rowIndex % 2 == 0 ? "#ffffff" : "#f9fafb";
                        void EmptyCell(IContainer c) =>
                            c.Background(extraBg).BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2).Padding(2).Text("");

                        // Empty cells for step through detection columns
                        for (var i = 0; i < 11; i++)
                            table.Cell().Element(EmptyCell);

                        table.Cell().Element(c => DataCell(c, extraAction.Description));
                        table.Cell().Element(c => DataCell(c, extraAction.ResponsiblePerson ?? ""));
                        table.Cell().Element(c =>
                            DataCell(c, extraAction.TargetDate?.ToString("yyyy-MM-dd") ?? ""));
                        table.Cell().Element(c => DataCell(c,
                            extraAction is { Status: "Completed" }
                                ? extraAction.CompletionNotes ?? "Completed"
                                : extraAction.Status));
                        table.Cell().Element(c =>
                            c.Background(extraBg).BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter()
                                .Text(extraAction.RevisedOccurrence?.ToString() ?? "").FontSize(6));
                        table.Cell().Element(c =>
                            c.Background(extraBg).BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter()
                                .Text(extraAction.RevisedDetection?.ToString() ?? "").FontSize(6));
                        table.Cell().Element(c =>
                            c.Background(extraBg).BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter()
                                .Text(extraAction.RevisedRpn?.ToString() ?? "").FontSize(6).Bold());
                    }

                    rowIndex++;
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container, PfmeaResponseDto pfmea,
        string companyName, string? footerText)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span(footerText ?? companyName).FontSize(6).FontColor(Colors.Grey.Medium);
                    text.Span($" | {pfmea.Code} v{pfmea.Version}").FontSize(6)
                        .FontColor(Colors.Grey.Medium);
                });
                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(6).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(6).FontColor(Colors.Grey.Medium);
                    text.Span(" of ").FontSize(6).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(6).FontColor(Colors.Grey.Medium);
                });
                row.RelativeItem().AlignRight()
                    .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                    .FontSize(6).FontColor(Colors.Grey.Medium);
            });
        });
    }
}
