using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;

/// <summary>
/// Renders the current page number from the <see cref="PdfContext"/> at render
/// time, so the same element instance shows the right number on each page when
/// used in a repeating header or footer. The format defaults to <c>{0}</c> and
/// can be customised (e.g. <c>"Page {0}"</c>).
/// </summary>
public sealed class PageNumberElement : UIElement<PageNumberElement>
{
    private readonly Font _font;
    private readonly double _size;
    private Color _color = Colors.Black;
    private string _format = "{0}";

    public PageNumberElement(Font font, double size)
    {
        _font = font;
        _size = size;
    }

    public PageNumberElement FontColor(Color color) { _color = color; return this; }
    public PageNumberElement Format(string format) { _format = format; return this; }

    private string Sample => string.Format(_format, 99);

    public override Size MinimalSpaceRequired =>
        new(_font.MeasureText(Sample, _size), _font.GetVerticalMetrics(_size).LineHeight);
    public override Size PreferredSize => MinimalSpaceRequired;

    protected override Size MeasureCore(Size available) => MinimalSpaceRequired;

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        var metrics = _font.GetVerticalMetrics(_size);
        string text = string.Format(_format, context.PageNumber);
        double baseline = context.Cursor.Y - metrics.Ascent;
        context.DrawText(_font, _size, context.Cursor.X, baseline, text, _color);
        return new RenderResult(null, new Point(context.Cursor.X, context.Cursor.Y - metrics.LineHeight));
    }
}
