using CSharpPdf.Text;
using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;

/// <summary>Flowing, word-wrapped text. Renders the lines that fit and returns the rest as overflow.</summary>
public sealed class TextElement : UIElement
{
    public string Text { get; set; } = "";
    public Font Font { get; set; } = Standard14Font.Helvetica;
    public double FontSize { get; set; } = 12;
    public Color FontColor { get; set; } = Colors.Black;

    /// <summary>Override the leading (line-to-line distance). Defaults to <c>FontSize * 1.2</c>.</summary>
    public double? LineHeight { get; set; }

    public TextElement() { }
    public TextElement(string text) { Text = text; }
    public TextElement(string text, Font font, double fontSize) { Text = text; Font = font; FontSize = fontSize; }

    private double Leading => LineHeight ?? FontSize * 1.2;

    // Single-line height = glyph bounding box (Ascent + Descent), with no
    // trailing LineGap that would only matter if another line followed. For
    // N lines, the row is (Ascent + Descent) + (N − 1) × Leading — so the box
    // hugs the text at top *and* bottom, and inter-line spacing stays at Leading.
    private double RowHeight(int lines)
    {
        if (lines <= 0) return 0;
        var m = Font.GetVerticalMetrics(FontSize);
        return m.Ascent + m.Descent + (lines - 1) * Leading;
    }

    public override Size MinimalSpaceRequired => new(LongestWordWidth(), RowHeight(1));
    public override Size PreferredSize => new(Font.MeasureText(Text.Replace('\n', ' '), FontSize), RowHeight(1));

    protected override Size MeasureCore(Size available)
    {
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, available.Width);
        double width = 0;
        foreach (string line in lines)
        {
            width = System.Math.Max(width, Font.MeasureText(line, FontSize));
        }
        return new Size(width, RowHeight(lines.Count));
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, available.Width);
        double leading = Leading;
        var metrics = Font.GetVerticalMetrics(FontSize);
        double glyphBox = metrics.Ascent + metrics.Descent;
        // How many lines fit in `available.Height` given the formula:
        // height(n) = glyphBox + (n−1)·leading  ⇒  n = 1 + (available − glyphBox) / leading.
        int maxLines = available.Height >= glyphBox
            ? System.Math.Max(1, 1 + (int)System.Math.Floor((available.Height - glyphBox) / leading))
            : 1;
        int drawn = System.Math.Min(maxLines, lines.Count);

        Point start = context.Cursor;
        for (int i = 0; i < drawn; i++)
        {
            double baseline = start.Y - metrics.Ascent - i * leading;
            context.DrawText(Font, FontSize, start.X, baseline, lines[i], FontColor);
        }

        var next = new Point(start.X, start.Y - RowHeight(drawn));
        if (drawn < lines.Count)
        {
            string rest = string.Join("\n", lines.GetRange(drawn, lines.Count - drawn));
            var overflow = new TextElement(rest, Font, FontSize) { FontColor = FontColor, LineHeight = LineHeight };
            return new RenderResult(overflow, next);
        }
        return new RenderResult(null, next);
    }

    private double LongestWordWidth()
    {
        double max = 0;
        foreach (string word in Text.Split(' ', '\n'))
        {
            max = System.Math.Max(max, Font.MeasureText(word, FontSize));
        }
        return max;
    }
}
