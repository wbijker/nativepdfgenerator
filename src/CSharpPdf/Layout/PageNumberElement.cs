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

    private string Sample => string.Format(Format, 99);

    public override Size MinimalSpaceRequired =>
        new(Font.MeasureText(Sample, FontSize), Font.GetVerticalMetrics(FontSize).LineHeight);
    public override Size PreferredSize => MinimalSpaceRequired;

    protected override Size MeasureCore(Size available) => MinimalSpaceRequired;

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        var metrics = Font.GetVerticalMetrics(FontSize);
        string text = string.Format(Format, context.PageNumber);
        double baseline = context.Cursor.Y - metrics.Ascent;
        context.DrawText(Font, FontSize, context.Cursor.X, baseline, text, FontColor);
        return new RenderResult(null, new Point(context.Cursor.X, context.Cursor.Y - metrics.LineHeight));
    }
}
