using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;

/// <summary>
/// The most primitive component: a single line of text in one font and size.
/// Measures with the font metrics and draws via the page. (Word wrapping across
/// multiple lines / pages will be a later component.)
/// </summary>
public sealed class TextBlock : Component
{
    private readonly string _text;
    private readonly Font _font;
    private readonly double _size;

    public TextBlock(string text, Font font, double size)
    {
        _text = text;
        _font = font;
        _size = size;
    }

    public override Size MinimalSpaceRequired => Intrinsic();
    public override Size PreferredSize => Intrinsic();

    private Size Intrinsic() =>
        new(_font.MeasureText(_text, _size), _font.GetVerticalMetrics(_size).LineHeight);

    public override RenderResult Render(RenderContext context, Size available)
    {
        var metrics = _font.GetVerticalMetrics(_size);
        if (available.Height < metrics.LineHeight)
        {
            return RenderResult.Empty; // not enough vertical room: try a fresh page
        }

        // The region's top is at context.Top; the text baseline sits one ascent below it.
        double baseline = context.Top - metrics.Ascent;
        context.Page.DrawText(_font, _size, context.Left, baseline, _text);

        return RenderResult.Full(new Size(_font.MeasureText(_text, _size), metrics.LineHeight));
    }
}
