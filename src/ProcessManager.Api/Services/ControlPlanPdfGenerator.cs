using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProcessManager.Api.Services;

public static class ControlPlanPdfGenerator
{
    public static byte[] Generate(ControlPlanResponseDto cp, TenantBranding? branding)
    {
        var isDraft = cp.IsStale;
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
                    ComposeHeader(header, cp, companyName, primaryColor));

                page.Content().Element(content =>
                    ComposeContent(content, cp, isDraft));

                page.Footer().Element(footer =>
                    ComposeFooter(footer, cp, companyName, footerText));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ControlPlanResponseDto cp,
        string companyName, string primaryColor)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(companyName).Bold().FontSize(12);
                    left.Item().Text("Control Plan")
                        .Bold().FontSize(10).FontColor(primaryColor);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text($"Document: {cp.Code}").Bold().FontSize(8);
                    right.Item().Text($"Version: {cp.Version}").FontSize(8);
                    right.Item().Text($"Date: {cp.UpdatedAt:yyyy-MM-dd}").FontSize(8);
                });
            });

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Process: {cp.ProcessName} ({cp.ProcessCode})")
                    .FontSize(8);
                row.RelativeItem().Text($"Control Plan: {cp.Name}").FontSize(8);
                row.RelativeItem().AlignRight()
                    .Text($"Status: {(cp.IsActive ? "Active" : "Inactive")}").FontSize(8);
            });

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            col.Item().PaddingBottom(4);
        });
    }

    private static void ComposeContent(IContainer container, ControlPlanResponseDto cp, bool isDraft)
    {
        container.Column(col =>
        {
            if (isDraft)
            {
                col.Item().PaddingBottom(4).Background("#fff3cd").Padding(4)
                    .Text("DRAFT — This Control Plan is stale and may not reflect the current process version.")
                    .FontSize(8).Bold().FontColor("#856404");
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);     // #
                    columns.RelativeColumn(1.4f);   // Operation / Step
                    columns.RelativeColumn(1.4f);   // Characteristic
                    columns.ConstantColumn(50);      // Type
                    columns.RelativeColumn(1.5f);   // Specification / Tolerance
                    columns.RelativeColumn(1.3f);   // Measurement Technique
                    columns.RelativeColumn(0.8f);   // Sample Size
                    columns.RelativeColumn(0.8f);   // Sample Frequency
                    columns.RelativeColumn(1.2f);   // Control Method
                    columns.RelativeColumn(1.5f);   // Reaction Plan
                    columns.RelativeColumn(1.0f);   // PFMEA Ref
                    columns.RelativeColumn(0.8f);   // Port Ref
                });

                table.Header(header =>
                {
                    var headerStyle = TextStyle.Default.FontSize(6.5f).Bold()
                        .FontColor(Colors.White);

                    void HeaderCell(IContainer c, string text) =>
                        c.Background("#374151").Padding(2).AlignCenter()
                            .Text(text).Style(headerStyle);

                    header.Cell().Element(c => HeaderCell(c, "#"));
                    header.Cell().Element(c => HeaderCell(c, "Operation / Step"));
                    header.Cell().Element(c => HeaderCell(c, "Characteristic"));
                    header.Cell().Element(c => HeaderCell(c, "Type"));
                    header.Cell().Element(c => HeaderCell(c, "Spec / Tolerance"));
                    header.Cell().Element(c => HeaderCell(c, "Measurement Technique"));
                    header.Cell().Element(c => HeaderCell(c, "Sample Size"));
                    header.Cell().Element(c => HeaderCell(c, "Sample Freq"));
                    header.Cell().Element(c => HeaderCell(c, "Control Method"));
                    header.Cell().Element(c => HeaderCell(c, "Reaction Plan"));
                    header.Cell().Element(c => HeaderCell(c, "PFMEA Ref"));
                    header.Cell().Element(c => HeaderCell(c, "Port"));
                });

                var rowIndex = 0;
                foreach (var entry in cp.Entries)
                {
                    var bgColor = rowIndex % 2 == 0 ? "#ffffff" : "#f9fafb";

                    void DataCell(IContainer c, string text) =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).Text(text).FontSize(6.5f);

                    void CenterCell(IContainer c, string text) =>
                        c.Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(2).AlignCenter().Text(text).FontSize(6.5f);

                    table.Cell().Element(c => CenterCell(c, entry.ProcessStepSequence.ToString()));
                    table.Cell().Element(c => DataCell(c, entry.ProcessStepName));
                    table.Cell().Element(c => DataCell(c, entry.CharacteristicName));
                    table.Cell().Element(c => CenterCell(c, entry.CharacteristicType));
                    table.Cell().Element(c => DataCell(c, entry.SpecificationOrTolerance ?? ""));
                    table.Cell().Element(c => DataCell(c, entry.MeasurementTechnique ?? ""));
                    table.Cell().Element(c => DataCell(c, entry.SampleSize ?? ""));
                    table.Cell().Element(c => DataCell(c, entry.SampleFrequency ?? ""));
                    table.Cell().Element(c => DataCell(c, entry.ControlMethod ?? ""));
                    table.Cell().Element(c => DataCell(c, entry.ReactionPlan ?? ""));
                    table.Cell().Element(c =>
                        DataCell(c, entry.LinkedPfmeaFailureModeDescription ?? ""));
                    table.Cell().Element(c => DataCell(c, entry.LinkedPortName ?? ""));

                    rowIndex++;
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container, ControlPlanResponseDto cp,
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
                    text.Span($" | {cp.Code} v{cp.Version}").FontSize(6)
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
