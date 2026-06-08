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
        doc.SetDefaultFont(Standard14Font.Helvetica, 10);

        var page = doc.AddPage(PageSizes.A4);
        var cs = page.Content;


        // Rows demo — mix Fixed / Auto / Relative columns
        var row = new Rows();
        row.Add(AxisSize.Fixed(60), new Rectangle(40, PdfColors.Blue(900)));

        var container = new Container();
        container.Add(new Paragraph("Some paragrpah", Standard14Font.Helvetica, 12));
        row.Add(AxisSize.Auto(), container);

        row.Add(AxisSize.Relative(1), new Rectangle(30, PdfColors.Blue(500)));
        row.Add(AxisSize.Relative(2), new Rectangle(70, PdfColors.Blue(300)));
        row.Add(AxisSize.Fixed(80), new Rectangle(60, PdfColors.Blue(100)));

        row.Render(cs, cs.Size);


        // ===== Save =====
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples"));
        Directory.CreateDirectory(samplesDir);
        var output = Path.Combine(samplesDir, "spec-text-operators.pdf");
        doc.Save(output);

        Console.WriteLine($"Wrote {output}");
    }
}
