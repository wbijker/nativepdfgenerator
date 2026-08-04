using PdfSpec.Geometry;
using PdfSpec.Images;
using PdfSpec.Objects;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 08 — the three masking techniques, each drawn over a
/// coloured background so the see-through areas are obvious:
/// soft (alpha) mask, colour-key mask, and a 1-bit stencil mask
/// painted in the current fill colour.
/// </summary>
public sealed class Sample08_ImageMasks : ISample
{
    public string FileName => "08-image-masks.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);
        var c = page.Content;
        const int w = 128, h = 128;

        // Soft mask — solid magenta image, radial alpha fades the edges out.
        var soft = PdfImage.Rgb(SampleImages.MakeSolid(w, h, 220, 30, 140), w, h);
        soft.SoftMask = PdfImage.Alpha(SampleImages.MakeRadialAlpha(w, h), w, h);
        page.AddXObject("ImSoft", soft.EmbedIn(doc));
        c.Save().SetRgbFill(PdfColor.Rgb(1, 0.95, 0.4)).Rectangle(60, 560, 200, 160).Fill().Restore();
        c.DrawImage("ImSoft", 60, 560, 200, 160);

        // Colour-key mask — white pixels are dropped, leaving the blue disc.
        var keyed = PdfImage.Rgb(SampleImages.MakeDiscOnWhite(w, h), w, h);
        keyed.ColorKeyMask = new PdfArray(
            new PdfNumber(255), new PdfNumber(255), new PdfNumber(255),
            new PdfNumber(255), new PdfNumber(255), new PdfNumber(255));
        page.AddXObject("ImKey", keyed.EmbedIn(doc));
        c.Save().SetRgbFill(PdfColor.Rgb(0.3, 0.8, 0.3)).Rectangle(320, 560, 200, 160).Fill().Restore();
        c.DrawImage("ImKey", 320, 560, 200, 160);

        // Stencil mask — 1-bit ImageMask painted in the current fill colour.
        page.AddXObject("ImStencil", PdfImage.Stencil(SampleImages.MakeCheckerBits(w, h), w, h).EmbedIn(doc));
        c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.85, 0.85)).Rectangle(60, 340, 200, 160).Fill().Restore();
        c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).DrawImage("ImStencil", 60, 340, 200, 160).Restore();

        doc.Save(path);
    }
}
