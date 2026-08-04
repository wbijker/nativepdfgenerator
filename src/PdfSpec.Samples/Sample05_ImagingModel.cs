using PdfSpec.Geometry;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 05 — vector graphics through the content-stream API: the
/// painter's model (later paints over earlier), paths, Bézier curves,
/// the three device colour spaces, coordinate transforms, and clipping.
/// </summary>
public sealed class Sample05_ImagingModel : ISample
{
    public string FileName => "05-imaging-model.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);
        var c = page.Content;

        // Painter's model: later shapes paint over earlier ones.
        c.SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(60, 660, 110, 90).Fill();
        c.SetRgbFill(PdfColor.Rgb(0, 1, 0)).Rectangle(110, 635, 110, 90).Fill();
        c.SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(160, 610, 110, 90).Fill();

        // A Bézier circle, both filled (orange) and stroked (dark blue, dashed).
        c.Save()
            .SetRgbFill(PdfColor.Rgb(1, 0.6, 0))
            .SetRgbStroke(PdfColor.Rgb(0, 0, 0.5))
            .SetLineWidth(2)
            .SetDash(new double[] { 5, 2 })
            .Circle(470, 690, 55).FillStroke()
            .Restore();

        // The three device colour spaces, as thick strokes.
        c.Save().SetLineWidth(10);
        c.SetGrayStroke(0.5).MoveTo(60, 560).LineTo(260, 560).Stroke();
        c.SetRgbStroke(PdfColor.Rgb(1, 0, 0)).MoveTo(60, 530).LineTo(260, 530).Stroke();
        c.SetCmykStroke(PdfColor.Cmyk(1, 0, 0, 0)).MoveTo(60, 500).LineTo(260, 500).Stroke();
        c.Restore();

        // Line caps and joins on a zigzag (round) vs a closed shape (bevel).
        c.Save()
            .SetRgbStroke(PdfColor.Rgb(0, 0.6, 0))
            .SetLineWidth(10).SetLineCap(1).SetLineJoin(1);
        c.MoveTo(330, 560).LineTo(380, 530).LineTo(430, 560).LineTo(480, 530).LineTo(530, 560).Stroke();
        c.Restore();

        // Transforms: a 50%-scaled square, a translated square, a rotated square.
        c.Save().Translate(60, 360).Scale(0.5, 0.5)
            .SetRgbFill(PdfColor.Rgb(0.8, 0, 0)).Rectangle(0, 0, 100, 100).Fill().Restore();
        c.Save().Translate(180, 360)
            .SetRgbFill(PdfColor.Rgb(0, 0.7, 0)).Rectangle(0, 0, 100, 100).Fill().Restore();
        c.Save().Translate(360, 410).Rotate(45)
            .SetRgbFill(PdfColor.Rgb(0, 0, 0.8)).Rectangle(-50, -50, 100, 100).Fill().Restore();

        // Clipping: two rectangles clipped to a circular region.
        c.Save();
        c.Circle(200, 180, 90).Clip().EndPath();
        c.SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(110, 90, 90, 180).Fill();
        c.SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(200, 90, 90, 180).Fill();
        c.Restore();

        doc.Save(path);
    }
}
