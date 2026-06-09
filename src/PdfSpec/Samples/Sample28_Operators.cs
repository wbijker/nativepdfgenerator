using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 28 — extra content-stream operators not exercised elsewhere:
/// the two pentagram fill rules (nonzero <c>b</c> vs even-odd <c>b*</c>),
/// the <c>v</c>/<c>y</c> Bézier curve variants forming a leaf, the
/// quote operator <c>"</c> that sets word + char spacing and shows on
/// the next line, and an inline image (<c>BI</c>/<c>ID</c>/<c>EI</c>)
/// with a tiny 4×4 RGB checker scaled up.
/// </summary>
public sealed class Sample28_Operators : ISample
{
    public string FileName => "28-operators.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);
        page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
        var c = page.Content;
        c.AddText().SetFont("F1", 22).Show(60, 740, "Additional Operators").Build();

        c.AddText().SetFont("F1", 11).Show(60, 700, "Pentagram fill: nonzero (b) vs even-odd (b*)").Build();
        c.Save().SetRgbFill(PdfColor.Rgb(1, 0.75, 0)).SetRgbStroke(PdfColor.Rgb(0.6, 0.4, 0)).SetLineWidth(2);
        AppendPentagram(c, 140, 620, 55);
        c.CloseFillStroke().Restore();
        c.Save().SetRgbFill(PdfColor.Rgb(1, 0.75, 0)).SetRgbStroke(PdfColor.Rgb(0.6, 0.4, 0)).SetLineWidth(2);
        AppendPentagram(c, 300, 620, 55);
        c.CloseFillStrokeEvenOdd().Restore();

        c.AddText().SetFont("F1", 11).Show(420, 700, "v / y Bézier curves").Build();
        c.Save().SetRgbFill(PdfColor.Rgb(0.2, 0.6, 0.9));
        c.MoveTo(440, 590).CurveToV(440, 660, 520, 660).CurveToY(520, 590, 440, 590).Fill().Restore();

        c.AddText().SetFont("F1", 14).SetLeading(20).SetTextMatrix(1, 0, 0, 1, 60, 540)
            .ShowText("The quote operator sets spacing and shows a line:")
            .NextLineShowText(wordSpacing: 6, charSpacing: 1, text: "spaced out via the quote operator")
            .Build();

        c.AddText().SetFont("F1", 11).Show(60, 470, "Inline image (BI/ID/EI):").Build();
        c.DrawInlineImageRgb(MakeTinyChecker(), 4, 4, 60, 380, 80, 80);

        doc.Save(path);
    }

    private static void AppendPentagram(ContentStream c, double cx, double cy, double r)
    {
        for (int i = 0; i < 5; i++)
        {
            int index = (i * 2) % 5;
            double a = -Math.PI / 2 + index * 2 * Math.PI / 5;
            double x = cx + r * Math.Cos(a), y = cy + r * Math.Sin(a);
            if (i == 0) c.MoveTo(x, y); else c.LineTo(x, y);
        }
        c.ClosePath();
    }

    private static byte[] MakeTinyChecker()
    {
        var rgb = new byte[4 * 4 * 3];
        int i = 0;
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            bool on = ((x + y) & 1) == 0;
            rgb[i++] = on ? (byte)230 : (byte)40;
            rgb[i++] = on ? (byte)60 : (byte)120;
            rgb[i++] = on ? (byte)60 : (byte)200;
        }
        return rgb;
    }
}
