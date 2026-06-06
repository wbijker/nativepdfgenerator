using PdfSpec.Content;
using PdfSpec.Geometry;
using PdfSpec.Fonts;
using PdfSpec.Layout;

namespace PdfSpec;


public class Rectangle(int size) : Element
{
    public override PdfSizeHint SizeHint(PdfSize available)
    {
        return PdfSizeHint.Fixed(size, size);
    }

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        cs.SetFillColor(PdfColors.Blue(500));
        cs.Rectangle(0, 0, size, size);
        cs.Fill();
        cs.AddText()
            .SetFillColor(PdfColors.Black())
            .SetStrokeColor(PdfColors.Black())
            .SetTextMatrix(PdfMatrix.Translate(0, 0))
            .ShowText("Die hond blaf")
            .Build();

        return RenderResult.Done(size);
    }
}

internal static class Program
{
    private const double LabelX = 40;
    private const double DemoX = 130;

    public static void Main(string[] args)
    {
        var doc = new PdfDoc();
        doc.Info.Title = "PdfSpec Text Operators";
        doc.Info.Creator = "PdfSpec";
        doc.Info.Producer = "PdfSpec";
        doc.SetDefaultFont(Standard14Font.Helvetica, 10);

        var page = doc.AddPage(PageSizes.A4);
        var cs = page.Content;


        // here
        double y = 50;
        cs.Save();
        var box = cs.CreateSubStream(LabelX, y, 40, 40);
        new Rectangle(40).Render(box, new PdfSize(40, 40));
        box.Build();
        cs.Restore();


        // ===== Save =====
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples"));
        Directory.CreateDirectory(samplesDir);
        var output = Path.Combine(samplesDir, "spec-text-operators.pdf");
        doc.Save(output);

        Console.WriteLine($"Wrote {output}");
    }
}