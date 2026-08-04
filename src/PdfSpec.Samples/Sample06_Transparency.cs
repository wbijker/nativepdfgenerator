using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 06 — basic transparency via named ExtGState resources
/// carrying constant alpha (ca / CA), plus content bracketed with the
/// marked-content operators (BMC/EMC and a BDC with an inline property
/// list).
/// </summary>
public sealed class Sample06_Transparency : ISample
{
    public string FileName => "06-transparency.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);

        page.AddExtGState("GSopaque", new PdfDictionary { ["ca"] = new PdfNumber(1.0), ["CA"] = new PdfNumber(1.0) });
        page.AddExtGState("GShalf", new PdfDictionary { ["ca"] = new PdfNumber(0.5), ["CA"] = new PdfNumber(0.5) });

        var c = page.Content;

        c.Save().SetExtGState("GSopaque").SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(150, 520, 170, 170).Fill().Restore();
        c.Save().SetExtGState("GShalf").SetRgbFill(PdfColor.Rgb(0, 1, 0)).Rectangle(230, 460, 170, 170).Fill().Restore();
        c.Save().SetExtGState("GShalf").SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(310, 400, 170, 170).Fill().Restore();

        c.BeginMarkedContent("Demo");
        c.Save().SetRgbFill(PdfColor.Rgb(0.4, 0.4, 0.4)).Rectangle(150, 250, 120, 90).Fill().Restore();
        c.EndMarkedContent();

        var props = new PdfDictionary { ["Label"] = new PdfString("Translucent overlay"), ["Index"] = new PdfNumber(1) };
        c.BeginMarkedContent("Demo", props);
        c.Save().SetExtGState("GShalf").SetRgbFill(PdfColor.Rgb(1, 0.5, 0)).Rectangle(310, 250, 120, 90).Fill().Restore();
        c.EndMarkedContent();

        doc.Save(path);
    }
}
