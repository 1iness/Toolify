using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HouseholdStore.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfColor = QuestPDF.Infrastructure.Color;

namespace HouseholdStore.Services;

public class AdminReportExportService
{
    private static class PdfTheme
    {
        public static readonly PdfColor Accent = PdfColor.FromHex("#28a745");
        public static readonly PdfColor AccentDark = PdfColor.FromHex("#1f3c2b");
        public static readonly PdfColor Text = PdfColor.FromHex("#223322");
        public static readonly PdfColor Muted = PdfColor.FromHex("#5f7567");
        public static readonly PdfColor HeaderBg = PdfColor.FromHex("#e8f8ee");
        public static readonly PdfColor HeaderText = PdfColor.FromHex("#2f4a38");
        public static readonly PdfColor Border = PdfColor.FromHex("#b9d4c1");
        public static readonly PdfColor BorderLight = PdfColor.FromHex("#e8efe9");
        public static readonly PdfColor CardBg = PdfColor.FromHex("#ffffff");
        public static readonly PdfColor RowAlt = PdfColor.FromHex("#f9fcf9");
        public static readonly PdfColor PageBgTop = PdfColor.FromHex("#f8fbf8");
        public static readonly PdfColor PageBgBottom = PdfColor.FromHex("#f3f7f4");
    }

    public byte[] ExportExcel(IReadOnlyList<AdminReportTable> tables)
    {
        using var workbook = new XLWorkbook();

        foreach (var table in tables)
        {
            var worksheet = workbook.Worksheets.Add(SanitizeWorksheetName(table.Title));
            worksheet.Cell(1, 1).Value = table.Title;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;

            for (var columnIndex = 0; columnIndex < table.Headers.Count; columnIndex++)
            {
                var cell = worksheet.Cell(3, columnIndex + 1);
                cell.Value = table.Headers[columnIndex];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                for (var columnIndex = 0; columnIndex < table.Headers.Count; columnIndex++)
                {
                    var cell = worksheet.Cell(rowIndex + 4, columnIndex + 1);
                    cell.Value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            worksheet.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportWord(IReadOnlyList<AdminReportTable> tables)
    {
        using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());
            var body = mainPart.Document.Body!;

            body.Append(CreateParagraph("Отчеты Toolify", true, "32"));

            foreach (var reportTable in tables)
            {
                body.Append(CreateParagraph(reportTable.Title, true, "24"));
                body.Append(CreateWordTable(reportTable));
                body.Append(new Paragraph(new Run(new Break())));
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    public byte[] ExportPdf(IReadOnlyList<AdminReportTable> tables)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var generatedAt = DateTime.Now;

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(36);
                page.MarginVertical(28);
                page.DefaultTextStyle(style => style
                    .FontSize(9)
                    .FontColor(PdfTheme.Text)
                    .LineHeight(1.25f));

                page.Background()
                    .BackgroundLinearGradient(180, new[] { PdfTheme.PageBgTop, PdfTheme.PageBgBottom });

                page.Header()
                    .Height(56)
                    .Background(PdfTheme.CardBg)
                    .BorderBottom(1)
                    .BorderColor(PdfTheme.BorderLight)
                    .Row(row =>
                    {
                        row.ConstantItem(5)
                            .ExtendVertical()
                            .Background(PdfTheme.Accent);

                        row.RelativeItem()
                            .PaddingVertical(10)
                            .PaddingLeft(14)
                            .PaddingRight(8)
                            .Column(col =>
                            {
                                col.Item().Row(titleRow =>
                                {
                                    titleRow.AutoItem().Text("Toolify")
                                        .FontSize(17)
                                        .Bold()
                                        .FontColor(PdfTheme.AccentDark);
                                    titleRow.AutoItem().PaddingLeft(8).PaddingTop(4).Text("—")
                                        .FontColor(PdfTheme.Border);
                                    titleRow.AutoItem().PaddingLeft(8).PaddingTop(2).Text("аналитика")
                                        .FontSize(12)
                                        .SemiBold()
                                        .FontColor(PdfTheme.Muted);
                                });
                                col.Item().PaddingTop(2).Text("Экспорт отчётов администратора")
                                    .FontSize(8.5f)
                                    .FontColor(PdfTheme.Muted);
                            });
                    });

                page.Content().Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor(PdfTheme.Muted));
                        text.Span("Сформировано: ");
                        text.Span($"{generatedAt:dd.MM.yyyy HH:mm}").SemiBold().FontColor(PdfTheme.AccentDark);
                    });

                    foreach (var table in tables)
                    {
                        column.Item()
                            .Background(PdfTheme.CardBg)
                            .Border(1)
                            .BorderColor(PdfTheme.BorderLight)
                            .CornerRadius(10)
                            .Padding(14)
                            .Column(section =>
                            {
                                section.Spacing(10);

                                section.Item().Row(r =>
                                {
                                    r.ConstantItem(4)
                                        .Height(14)
                                        .Background(PdfTheme.Accent)
                                        .CornerRadius(2);
                                    r.RelativeItem()
                                        .PaddingLeft(10)
                                        .AlignMiddle()
                                        .Text(table.Title)
                                        .FontSize(11.5f)
                                        .SemiBold()
                                        .FontColor(PdfTheme.AccentDark);
                                });

                                section.Item().Table(pdfTable =>
                                {
                                    pdfTable.ColumnsDefinition(columns =>
                                    {
                                        foreach (var _ in table.Headers)
                                        {
                                            columns.RelativeColumn();
                                        }
                                    });

                                    pdfTable.Header(header =>
                                    {
                                        foreach (var title in table.Headers)
                                        {
                                            header.Cell().Element(PdfHeaderCell).Text(title).SemiBold();
                                        }
                                    });

                                    for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
                                    {
                                        var row = table.Rows[rowIndex];
                                        var zebra = rowIndex % 2 == 1;
                                        for (var columnIndex = 0; columnIndex < table.Headers.Count; columnIndex++)
                                        {
                                            var value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                                            pdfTable.Cell().Element(c => PdfBodyCell(c, zebra)).Text(value);
                                        }
                                    }
                                });
                            });
                    }
                });

                page.Footer()
                    .Height(36)
                    .AlignMiddle()
                    .BorderTop(1)
                    .BorderColor(PdfTheme.BorderLight)
                    .PaddingTop(8)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .AlignMiddle()
                            .Text("Toolify · отчёты")
                            .FontSize(8)
                            .FontColor(PdfTheme.Muted);

                        row.AutoItem()
                            .AlignMiddle()
                            .Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(8).FontColor(PdfTheme.Muted));
                                text.Span("стр. ");
                                text.CurrentPageNumber().SemiBold().FontColor(PdfTheme.AccentDark);
                                text.Span(" / ");
                                text.TotalPages().SemiBold().FontColor(PdfTheme.AccentDark);
                            });
                    });
            });
        }).GeneratePdf();
    }

    private static string SanitizeWorksheetName(string title)
    {
        var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var sanitized = invalidChars.Aggregate(title, (current, invalidChar) => current.Replace(invalidChar, ' '));
        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
    }

    private static IContainer PdfHeaderCell(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(PdfTheme.Border)
            .Background(PdfTheme.HeaderBg)
            .PaddingVertical(7)
            .PaddingHorizontal(8)
            .DefaultTextStyle(x => x.FontSize(8.5f).FontColor(PdfTheme.HeaderText).SemiBold());
    }

    private static IContainer PdfBodyCell(IContainer container, bool alternateRow)
    {
        return container
            .BorderBottom(1)
            .BorderColor(PdfTheme.BorderLight)
            .Background(alternateRow ? PdfTheme.RowAlt : PdfTheme.CardBg)
            .PaddingVertical(6)
            .PaddingHorizontal(8)
            .DefaultTextStyle(x => x.FontSize(8.5f).FontColor(PdfTheme.Text));
    }

    private static Paragraph CreateParagraph(string text, bool bold, string fontSize)
    {
        return new Paragraph(
            new Run(
                CreateRunProperties(bold, fontSize),
                new Text(text)))
        {
            ParagraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { After = "160" })
        };
    }

    private static Table CreateWordTable(AdminReportTable reportTable)
    {
        var table = new Table(
            new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        table.Append(new TableRow(reportTable.Headers.Select(header => CreateWordCell(header, true))));

        foreach (var row in reportTable.Rows)
        {
            table.Append(new TableRow(
                Enumerable.Range(0, reportTable.Headers.Count)
                    .Select(columnIndex => CreateWordCell(columnIndex < row.Count ? row[columnIndex] : string.Empty, false))));
        }

        return table;
    }

    private static TableCell CreateWordCell(string text, bool isHeader)
    {
        var properties = new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Auto },
            new TableCellMargin(
                new TopMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                new BottomMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa }));

        if (isHeader)
        {
            properties.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = "D9EAF7" });
        }

        return new TableCell(
            properties,
            new Paragraph(
                new Run(
                    CreateRunProperties(isHeader),
                    new Text(text ?? string.Empty))));
    }

    private static RunProperties CreateRunProperties(bool bold, string? fontSize = null)
    {
        var properties = new RunProperties();

        if (bold)
        {
            properties.Append(new Bold());
        }

        if (!string.IsNullOrWhiteSpace(fontSize))
        {
            properties.Append(new FontSize { Val = fontSize });
        }

        return properties;
    }
}
