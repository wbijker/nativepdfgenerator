using PdfSpec.Content;
using PdfSpec.Geometry;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 09 — a reusable form XObject (a gold star) defined once
/// inside a 100x100 bounding box and painted many times with different
/// CTM transforms (full size, scaled, rotated, plus a row of small
/// stamps). One resource backs every instance.
/// </summary>
public sealed class Sample09_FormXObject : ISample
{
    public string FileName => "09-form-xobject.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);

        var star = new FormXObject(doc, PdfRectangle.FromSize(100, 100));
        star.Content
            .SetRgbFill(PdfColor.Rgb(1, 0.78, 0))
            .SetRgbStroke(PdfColor.Rgb(0.5, 0.35, 0))
            .SetLineWidth(3);
        AppendStar(star.Content, 50, 50, 45, 18);
        star.Content.CloseFillStroke();
        page.AddXObject("Star", doc.AddObject(star.Build()));

        var c = page.Content;
        c.Save().Translate(70, 600).PaintXObject("Star").Restore();
        c.Save().Translate(250, 640).Scale(0.6, 0.6).PaintXObject("Star").Restore();
        c.Save().Translate(420, 650).Rotate(20).Scale(0.8, 0.8).PaintXObject("Star").Restore();
        for (int i = 0; i < 5; i++)
        {
            c.Save().Translate(70 + i * 90, 430).Scale(0.45, 0.45).PaintXObject("Star").Restore();
        }

        doc.Save(path);
    }

    /// <summary>Append a five-pointed star subpath centred at (cx, cy).</summary>
    private static void AppendStar(ContentStream c, double cx, double cy, double outer, double inner)
    {
        for (int i = 0; i < 10; i++)
        {
            double r = (i % 2 == 0) ? outer : inner;
            double angle = -Math.PI / 2 + i * Math.PI / 5;
            double x = cx + r * Math.Cos(angle);
            double y = cy + r * Math.Sin(angle);
            if (i == 0) c.MoveTo(x, y); else c.LineTo(x, y);
        }
        c.ClosePath();
    }
}
