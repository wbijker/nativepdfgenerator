using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;

/// <summary>
/// The most primitive text component: a single line in one font and size (no
/// wrapping). For flowing, wrapping text use <see cref="Paragraph"/>.
/// </summary>
public sealed class TextBlock : Component<TextBlock>
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

    protected override Size MeasureCore(Size available) => Intrinsic();

    protected override RenderResult RenderCore(RenderContext context, Size available)
    {
        var metrics = _font.GetVerticalMetrics(_size);
        if (available.Height < metrics.LineHeight)
        {
            return RenderResult.Empty;
        }
        double baseline = context.Top - metrics.Ascent;
        context.Page.DrawText(_font, _size, context.Left, baseline, _text);
        return RenderResult.Full(new Size(_font.MeasureText(_text, _size), metrics.LineHeight));
    }
}
