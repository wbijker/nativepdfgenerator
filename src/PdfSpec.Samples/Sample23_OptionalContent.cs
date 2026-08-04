using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 23 — three optional-content layers (Red / Green / Blue)
/// marked in the content stream via <c>BDC /OC</c>. The viewer panel
/// orders them as Red → Green → Blue; Blue is OFF in the default
/// configuration so it stays hidden until the user enables it.
/// </summary>
public sealed class Sample23_OptionalContent : ISample
{
    public string FileName => "23-optional-content.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);
        page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));

        var redOcg = doc.AddOptionalContentGroup("Red layer");
        var greenOcg = doc.AddOptionalContentGroup("Green layer");
        var blueOcg = doc.AddOptionalContentGroup("Blue layer");
        page.AddProperty("OCR", redOcg);
        page.AddProperty("OCG", greenOcg);
        page.AddProperty("OCB", blueOcg);

        doc.OptionalContentConfig["Order"] = new PdfArray(redOcg, greenOcg, blueOcg);
        doc.OptionalContentConfig["OFF"] = new PdfArray(blueOcg);

        var c = page.Content;
        c.AddText(StandardFont.Helvetica, 22).Show(60, 740, "Optional Content (Layers)").Build();
        c.AddText(StandardFont.Helvetica, 12).Show(60, 712, "Red and Green are ON by default; Blue is OFF.").Build();

        c.BeginOptionalContent("OCR").SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(80, 560, 160, 120).Fill().EndMarkedContent();
        c.BeginOptionalContent("OCG").SetRgbFill(PdfColor.Rgb(0, 0.7, 0)).Rectangle(180, 560, 160, 120).Fill().EndMarkedContent();
        c.BeginOptionalContent("OCB").SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(280, 560, 160, 120).Fill().EndMarkedContent();

        doc.Save(path);
    }
}
