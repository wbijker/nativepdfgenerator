using CSharpPdf.Text;
using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;

/// <summary>
/// Flowing, word-wrapped text. Wraps to the available width and, when it can't
/// fit vertically, renders the lines that fit and returns the rest as a
/// continuation <see cref="Paragraph"/> (the partial-render path).
/// </summary>
public sealed class Paragraph : Component<Paragraph>
{
    private readonly string _text;
    private Font _font;
    private double _size;
    private Color _color = Colors.Black;
    private double? _leading;

    public Paragraph(string text, Font font, double size)
    {
        _text = text;
        _font = font;
        _size = size;
    }

    // ----- fluent configuration -----
    public Paragraph FontSize(double size) { _size = size; return this; }
    public Paragraph FontColor(Color color) { _color = color; return this; }
    public Paragraph WithFont(Font font) { _font = font; return this; }
    public Paragraph LineHeight(double leading) { _leading = leading; return this; }

    private double Leading => _leading ?? _size * 1.2;

    public override Size MinimalSpaceRequired => new(LongestWordWidth(), Leading);

    public override Size PreferredSize => new(_font.MeasureText(_text.Replace('\n', ' '), _size), Leading);

    protected override Size MeasureCore(Size available)
    {
        var lines = TextMeasurer.WrapText(_font, _size, _text, available.Width);
        double width = 0;
        foreach (string line in lines)
        {
            width = System.Math.Max(width, _font.MeasureText(line, _size));
        }
        return new Size(width, lines.Count * Leading);
    }

    protected override RenderResult RenderCore(RenderContext context, Size available)
    {
        var lines = TextMeasurer.WrapText(_font, _size, _text, available.Width);
        double leading = Leading;
        int maxLines = (int)System.Math.Floor(available.Height / leading);
        if (maxLines <= 0)
        {
            return RenderResult.Empty;
        }

        int drawn = System.Math.Min(maxLines, lines.Count);
        var metrics = _font.GetVerticalMetrics(_size);
        double width = 0;

        context.Page.Content.Save().SetRgbFill(_color.R, _color.G, _color.B);
        for (int i = 0; i < drawn; i++)
        {
            double baseline = context.Top - metrics.Ascent - i * leading;
            context.Page.DrawText(_font, _size, context.Left, baseline, lines[i]);
            width = System.Math.Max(width, _font.MeasureText(lines[i], _size));
        }
        context.Page.Content.Restore();

        var used = new Size(width, drawn * leading);
        if (drawn < lines.Count)
        {
            string rest = string.Join("\n", lines.GetRange(drawn, lines.Count - drawn));
            var remainder = new Paragraph(rest, _font, _size) { _color = _color, _leading = _leading };
            return RenderResult.Partial(used, remainder);
        }
        return RenderResult.Full(used);
    }

    private double LongestWordWidth()
    {
        double max = 0;
        foreach (string word in _text.Split(' ', '\n'))
        {
            max = System.Math.Max(max, _font.MeasureText(word, _size));
        }
        return max;
    }
}
