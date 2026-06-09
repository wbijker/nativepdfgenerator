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

        var page = doc.AddPage(PageSizes.A5);

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

        // A MultiColumn (2 columns, newspaper-style flow) with five
        // paragraphs interleaved with rectangles of various sizes.
        // Sits inside the page body alongside the row + the standalone
        // paragraphs / rectangles.
        var multi = new MultiColumn
        {
            ColumnCount = 2,
            ColumnGap = 20,
            Background = PdfColors.Green(100),
        };
        multi.Add(new Paragraph(
            "First paragraph in the multi-column flow. The MultiColumn " +
            "container divides the available width into N columns and " +
            "drops each item into the next available slot top-to-bottom " +
            "before wrapping into the next column.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(30, PdfColors.Indigo(400)));
        multi.Add(new Paragraph(
            "Second paragraph. Items keep their natural height so the " +
            "wrap point depends on the column height and the cumulative " +
            "height of everything that came before it in the same column.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(60, PdfColors.Amber(400)));
        multi.Add(new Paragraph(
            "Third paragraph. A wider gap between paragraphs is just a " +
            "matter of inserting an empty Rectangle (or any spacer) — " +
            "the flow doesn't care what shape an item has.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(45, PdfColors.Teal(400)));
        multi.Add(new Paragraph(
            "Fourth paragraph. By the time the cumulative content in " +
            "column 1 exceeds the page height, subsequent items spill " +
            "into column 2 automatically.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(80, PdfColors.Rose(400)));
        multi.Add(new Paragraph(
            "Fifth paragraph — the last of the five. Anything beyond " +
            "what fits in the configured columns becomes a Partial " +
            "continuation.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(25, PdfColors.Emerald(400)));

        multi.Add(new Paragraph(
            "First paragraph in the multi-column flow. The MultiColumn " +
            "container divides the available width into N columns and " +
            "drops each item into the next available slot top-to-bottom " +
            "before wrapping into the next column.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(30, PdfColors.Indigo(400)));
        multi.Add(new Paragraph(
            "Second paragraph. Items keep their natural height so the " +
            "wrap point depends on the column height and the cumulative " +
            "height of everything that came before it in the same column.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(60, PdfColors.Amber(400)));
        multi.Add(new Paragraph(
            "Third paragraph. A wider gap between paragraphs is just a " +
            "matter of inserting an empty Rectangle (or any spacer) — " +
            "the flow doesn't care what shape an item has.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(45, PdfColors.Teal(400)));
        multi.Add(new Paragraph(
            "Fourth paragraph. By the time the cumulative content in " +
            "column 1 exceeds the page height, subsequent items spill " +
            "into column 2 automatically.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(80, PdfColors.Rose(400)));
        multi.Add(new Paragraph(
            "Fifth paragraph — the last of the five. Anything beyond " +
            "what fits in the configured columns becomes a Partial " +
            "continuation.",
            StandardFont.Helvetica, 11));
        multi.Add(new Rectangle(25, PdfColors.Emerald(400)));

        // A page owns one Element. Wrap everything — the HStack row, the
        // standalone paragraphs / rectangles, and the MultiColumn — in a
        // single outer VStack and hand that to page.Body.
        var body = new VStack { Background = PdfColors.Slate(200) };
        body.AddAuto(row);
        body.AddFixed(50, new Rectangle(40, PdfColors.Emerald(400)));
        body.AddAuto(new Paragraph(
            "Second paragraph — sitting under the first row. The VStack " +
            "stacks items top to bottom; Fixed slots claim their value " +
            "and Auto slots take whatever height the content reports.",
            StandardFont.Helvetica, 11), Alignment.Center);
        body.AddFixed(60, new Rectangle(50, PdfColors.Rose(400)), Alignment.Center);
        body.AddAuto(new Paragraph(
            "Third paragraph — last text-only item. When the column " +
            "doesn't fit the page, items beyond the cut return a Partial " +
            "continuation for the next page.",
            StandardFont.Helvetica, 11), Alignment.End);
        body.AddAuto(multi);

        page.Body(body);


        // ===== Save =====
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples"));
        Directory.CreateDirectory(samplesDir);
        var output = Path.Combine(samplesDir, "spec-text-operators.pdf");
        doc.Save(output);

        Console.WriteLine($"Wrote {output}");
    }
}