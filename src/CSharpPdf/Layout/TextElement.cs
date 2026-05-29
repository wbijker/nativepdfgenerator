using CSharpPdf.Text;
using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;

/// <summary>Flowing, word-wrapped text. Renders the lines that fit and returns the rest as overflow.</summary>
public sealed class TextElement : UIElement<TextElement>
{
    private readonly string _text;
    private Font _font;
    private double _size;
    private Color _color = Colors.Black;
    private double? _leading;

    public TextElement(string text, Font font, double size)
    {
        _text = text;
        _font = font;
        _size = size;
    }

    public TextElement FontSize(double size) { _size = size; return this; }
    public TextElement FontColor(Color color) { _color = color; return this; }
    public TextElement WithFont(Font font) { _font = font; return this; }
    public TextElement LineHeight(double leading) { _leading = leading; return this; }

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

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        var lines = TextMeasurer.WrapText(_font, _size, _text, available.Width);
        double leading = Leading;
        int maxLines = System.Math.Max(1, (int)System.Math.Floor(available.Height / leading));
        int drawn = System.Math.Min(maxLines, lines.Count);

        var metrics = _font.GetVerticalMetrics(_size);
        Point start = context.Cursor;
        for (int i = 0; i < drawn; i++)
        {
            double baseline = start.Y - metrics.Ascent - i * leading;
            context.DrawText(_font, _size, start.X, baseline, lines[i], _color);
        }

        var next = new Point(start.X, start.Y - drawn * leading);
        if (drawn < lines.Count)
        {
            string rest = string.Join("\n", lines.GetRange(drawn, lines.Count - drawn));
            var overflow = new TextElement(rest, _font, _size) { _color = _color, _leading = _leading };
            return new RenderResult(overflow, next);
        }
        return new RenderResult(null, next);
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
