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

    public override Size MinimalSpaceRequired => new(LongestWordWidth(), Leading);
    public override Size PreferredSize => new(Font.MeasureText(Text.Replace('\n', ' '), FontSize), Leading);

    protected override Size MeasureCore(Size available)
    {
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, available.Width);
        double width = 0;
        foreach (string line in lines)
        {
            width = System.Math.Max(width, Font.MeasureText(line, FontSize));
        }
        return new Size(width, lines.Count * Leading);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, available.Width);
        double leading = Leading;
        int maxLines = System.Math.Max(1, (int)System.Math.Floor(available.Height / leading));
        int drawn = System.Math.Min(maxLines, lines.Count);

        var metrics = Font.GetVerticalMetrics(FontSize);
        Point start = context.Cursor;
        for (int i = 0; i < drawn; i++)
        {
            double baseline = start.Y - metrics.Ascent - i * leading;
            context.DrawText(Font, FontSize, start.X, baseline, lines[i], FontColor);
        }

        var next = new Point(start.X, start.Y - drawn * leading);
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
