using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;

/// <summary>
/// Renders the current page number from the <see cref="PdfContext"/> at render
/// time, so the same element instance shows the right number on each page when
/// used in a repeating header or footer. The format defaults to <c>{0}</c> and
/// can be customised (e.g. <c>"Page {0}"</c>).
/// </summary>
public sealed class PageNumberElement : UIElement
{
    public Font Font { get; set; } = CSharpPdf.Text.Standard14Font.Helvetica;
    public double FontSize { get; set; } = 10;
    public Color FontColor { get; set; } = Colors.Black;
    public string Format { get; set; } = "{0}";

    public PageNumberElement() { }
    public PageNumberElement(Font font, double fontSize) { Font = font; FontSize = fontSize; }

    // Format takes the current page as {0} and the total page count as {1}; e.g.
    // "Page {0} of {1}". Width is measured against a pessimistic sample so the
    // measure phase reserves the same width as the render phase.
    private string Sample => string.Format(Format, 99, 99);

    public override SpaceDimension SpaceRequired(SizeRect available)
    {
        var metrics = Font.GetVerticalMetrics(FontSize);
        double w = Font.MeasureText(Sample, FontSize);
        double h = metrics.Ascent + metrics.Descent;
        var size = new SizeRect(w, h);
        return WithOwnInset(new SpaceDimension(size, size, verticalBreakable: false));
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        var metrics = Font.GetVerticalMetrics(FontSize);
        string text = string.Format(Format, context.PageNumber, context.TotalPages);
        double baseline = context.Cursor.Y - metrics.Ascent;
        context.DrawText(Font, FontSize, context.Cursor.X, baseline, text, FontColor);
        return new RenderResult(null, new Point(context.Cursor.X, context.Cursor.Y - metrics.LineHeight));
    }
}
