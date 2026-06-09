using PdfSpec.Geometry;
using PdfSpec.Images;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 07 — a procedurally generated DeviceRGB image embedded as an
/// Image XObject (Flate-compressed) and painted at two different sizes,
/// showing that one resource can be reused with different transforms.
/// </summary>
public sealed class Sample07_RasterImage : ISample
{
    public string FileName => "07-raster-image.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);

        const int w = 128, h = 128;
        var image = PdfImage.Rgb(SampleImages.MakeGradient(w, h), w, h);
        page.AddXObject("Im1", image.EmbedIn(doc));

        page.Content.DrawImage("Im1", 80, 430, 280, 280);
        page.Content.DrawImage("Im1", 380, 430, 120, 120);

        doc.Save(path);
    }
}
