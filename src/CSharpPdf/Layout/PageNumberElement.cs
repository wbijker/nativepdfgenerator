using CSharpPdf.Content;
using Font = PdfSpec.Fonts.Font;

using PdfSpec.Geometry;
namespace CSharpPdf.Layout;

/// <summary>
/// Renders the current page number from the <see cref="PdfCanvas"/> at render
/// time, so the same element instance shows the right number on each page when
/// used in a repeating header or footer. The format defaults to <c>{0}</c> and
/// can be customised (e.g. <c>"Page {0}"</c>).
/// </summary>
public sealed class PageNumberElement : Element
{
    public Font Font { get; set; } = PdfSpec.Fonts.StandardFont.Helvetica;
    public double FontSize { get; set; } = 10;
    public Color FontColor { get; set; } = Colors.Black;
    public string Format { get; set; } = "{0}";

    public PageNumberElement() { }
    public PageNumberElement(Font font, double fontSize) { Font = font; FontSize = fontSize; }

    // Format takes the current page as {0} and the total page count as {1}; e.g.
    // "Page {0} of {1}". Width is measured against a pessimistic sample so the
    // measure phase reserves the same width as the render phase.
    private string Sample => string.Format(Format, 99, 99);

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var metrics = Font.GetVerticalMetrics(FontSize);
        double w = Font.MeasureText(Sample, FontSize);
        double h = metrics.Ascent + metrics.Descent;
        var size = new SizeRect(w, h);
        return WithOwnInset(new SpaceDimension(size, size, verticalBreakable: false));
    }

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        // Defer the actual draw — at this point we know PageNumber (the
        // current page) but TotalPages is still 0 because the layout pass
        // hasn't finished yet. Reserve the pessimistic width (measured against
        // the Sample) so the column never reflows when the deferred closure
        // paints the real value.
        var metrics = Font.GetVerticalMetrics(FontSize);
        double w = Font.MeasureText(Sample, FontSize);
        double h = metrics.Ascent + metrics.Descent;
        var font = Font;
        var fontSize = FontSize;
        var format = Format;
        var color = FontColor;
        context.Defer(w, h, sub =>
        {
            string text = string.Format(format, sub.PageNumber, sub.TotalPages);
            double baseline = h - metrics.Ascent;
            sub.DrawText(font, fontSize, 0, baseline, text, color);
        });
        return new RenderResult(null, new Point(context.Cursor.X, context.Cursor.Y - metrics.LineHeight));
    }
}
