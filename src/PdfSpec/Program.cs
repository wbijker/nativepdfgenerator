using PdfSpec.Elements;
using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec;

internal static class Program
{
    public static void Main(string[] args)
    {
        var doc = new PdfDoc();
        doc.Info.Title = "PdfSpec Text Operators";
        doc.Info.Creator = "PdfSpec";
        doc.Info.Producer = "PdfSpec";
        doc.SetDefaultFont(StandardFont.Helvetica, 10);

        var page = doc.AddPage(PageSizes.A4);
        var cs = page.Content;

        // page.PageBreak();

        // HStack demo — mix Fixed / Auto / Relative columns. HStack
        // distributes column widths and applies per-item H/V alignment
        // inside the row band.
        var row = new HStack();
        row.Background = PdfColors.Purple(100);

        row.Add(AxisSize.Fixed(60), new BorderElement
        {
            Content = new Rectangle(40, PdfColors.Blue(900)),
        }, null, Alignment.End);

        var border = new BorderElement();
        border.Background = PdfColors.Pink(200);

        border.SetContent(
            new BorderElement()
            {
                Content = new Paragraph(
                    "Some paragraph - full of content. Generated: " + DateTime.Now.ToLongTimeString(),
                    StandardFont.Helvetica, 12),
                Background = PdfColors.Yellow(200),
            });
        row.Add(AxisSize.Auto(), border, null, Alignment.Center);

        row.Add(AxisSize.Relative(1), new Rectangle(30, PdfColors.Blue(500)), Alignment.End);
        row.Add(AxisSize.Relative(2), new Rectangle(70, PdfColors.Blue(300)), Alignment.Center);
        row.Add(AxisSize.Fixed(80), new Rectangle(60, PdfColors.Blue(100)), Alignment.Start);

        // Wrap everything in a 5-item VStack: the row above, then four
        // more items alternating rectangles and paragraphs. Auto slots
        // for paragraphs (the text decides its own height); Fixed slots
        // for rectangles (the size is intrinsic). The VStack inherits
        // BoxElement, so its background paints behind every item.
        var column = new VStack { Background = PdfColors.Slate(50) };
        column.AddAuto(row);
        column.AddFixed(50, new Rectangle(40, PdfColors.Emerald(400)));
        column.AddAuto(new Paragraph(
            "Second paragraph — sitting under the first row. The Column " +
            "stacks items top to bottom; Fixed slots claim their value " +
            "and Auto slots take whatever height the content reports.",
            StandardFont.Helvetica, 11), Alignment.Center);
        column.AddFixed(60, new Rectangle(50, PdfColors.Rose(400)), Alignment.Center);
        column.AddAuto(new Paragraph(
            "Third paragraph — last item in the column. When the column " +
            "doesn't fit the page, items beyond the cut return a Partial " +
            "continuation Column for the next page.",
            StandardFont.Helvetica, 11), Alignment.End);

        column.Render(cs, cs.Size);


        // ===== Save =====
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples"));
        Directory.CreateDirectory(samplesDir);
        var output = Path.Combine(samplesDir, "spec-text-operators.pdf");
        doc.Save(output);

        Console.WriteLine($"Wrote {output}");
    }
}