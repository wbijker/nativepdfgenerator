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

        // Split the previous single VStack into three parts so we can
        // place an imperative page break before and after the second
        // paragraph.
        //   Page 1: HStack row + emerald rectangle
        //   Page 2: second paragraph
        //   Page 3: rose rectangle + third paragraph

        var part1 = new VStack { Background = PdfColors.Slate(50) };
        part1.AddAuto(row);
        part1.AddFixed(50, new Rectangle(40, PdfColors.Emerald(400)));
        page.Body(part1);

        // Page break before the second paragraph.
        page = page.PageBreak();

        var part2 = new VStack { Background = PdfColors.Slate(50) };
        part2.AddAuto(new Paragraph(
            "Second paragraph — sitting under the first row. The Column " +
            "stacks items top to bottom; Fixed slots claim their value " +
            "and Auto slots take whatever height the content reports.",
            StandardFont.Helvetica, 11), Alignment.Center);
        page.Body(part2);

        // Page break after the second paragraph.
        page = page.PageBreak();

        var part3 = new VStack { Background = PdfColors.Slate(50) };
        part3.AddFixed(60, new Rectangle(50, PdfColors.Rose(400)), Alignment.Center);
        part3.AddAuto(new Paragraph(
            "Third paragraph — last item in the column. When the column " +
            "doesn't fit the page, items beyond the cut return a Partial " +
            "continuation Column for the next page.",
            StandardFont.Helvetica, 11), Alignment.End);
        page.Body(part3);


        // ===== Save =====
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples"));
        Directory.CreateDirectory(samplesDir);
        var output = Path.Combine(samplesDir, "spec-text-operators.pdf");
        doc.Save(output);

        Console.WriteLine($"Wrote {output}");
    }
}