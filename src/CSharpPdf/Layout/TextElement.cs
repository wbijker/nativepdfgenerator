using CSharpPdf.Content;
using CSharpPdf.Text;
using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;



/// <summary>Flowing, word-wrapped text. Renders the lines that fit and returns the rest as overflow.</summary>
public sealed class TextElement : Element
{
    public string Text { get; set; } = "";
    public Font Font { get; set; } = Standard14Font.Helvetica;
    public double FontSize { get; set; } = 12;
    public Color FontColor { get; set; } = Colors.Black;

    /// <summary>Override the leading (line-to-line distance). Defaults to <c>FontSize * 1.2</c>.</summary>
    public double? LineHeight { get; set; }

    /// <summary>
    /// When true, the per-word widths measured for this element's text+font+size
    /// are published into the rendering canvas's shared
    /// <see cref="PdfCanvas.WordWidthCache"/> during render, so other text
    /// elements with the same words can be measured by lookup instead of
    /// remeasured through the font.
    /// </summary>
    public bool SaveMetric { get; set; }

    public TextElement() { }
    public TextElement(string text) { Text = text; }
    public TextElement(string text, Font font, double fontSize) { Text = text; Font = font; FontSize = fontSize; }

    private double Leading => LineHeight ?? FontSize * 1.2;

    // Pre-measured per-word widths for the current Text/Font/FontSize. Built
    // lazily on the first measurement query and reused thereafter — every
    // SpaceHint and RenderCore call would otherwise rescan words through the
    // font's metric tables. Cleared implicitly if Text/Font/FontSize change
    // (the consumer should rebuild a fresh element rather than mutate, since
    // the dictionary is keyed by exact value).
    private Dictionary<string, double>? _wordWidths;
    private string? _measuredText;
    private string? _measuredFontKey;
    private double _measuredFontSize;

    /// <summary>Snapshot of the per-word width measurements (null until the first measurement query). Exposed for tests/diagnostics.</summary>
    public IReadOnlyDictionary<string, double>? WordWidths => _wordWidths;

    /// <summary>
    /// Pre-measure every distinct word in <see cref="Text"/> at the current
    /// <see cref="Font"/>/<see cref="FontSize"/>. Idempotent: a second call with
    /// the same text/font/size is a no-op. Returns the populated dictionary.
    /// </summary>
    private Dictionary<string, double> EnsureWordWidths()
    {
        string fontKey = Font.BaseFont;
        if (_wordWidths is not null
            && _measuredText == Text
            && _measuredFontKey == fontKey
            && _measuredFontSize == FontSize)
        {
            return _wordWidths;
        }
        var widths = new Dictionary<string, double>();
        foreach (var word in Text.Split(' ', '\n'))
        {
            if (word.Length == 0 || widths.ContainsKey(word)) continue;
            widths[word] = Font.MeasureText(word, FontSize);
        }
        _wordWidths = widths;
        _measuredText = Text;
        _measuredFontKey = fontKey;
        _measuredFontSize = FontSize;
        return widths;
    }

    /// <summary>If <see cref="SaveMetric"/> is on, publish this element's word widths into <paramref name="cache"/>.</summary>
    private void PublishWordWidths(Dictionary<(string Font, double Size, string Word), double> cache)
    {
        if (!SaveMetric || _wordWidths is null) return;
        string fontKey = _measuredFontKey ?? Font.BaseFont;
        foreach (var (word, width) in _wordWidths)
        {
            cache[(fontKey, _measuredFontSize, word)] = width;
        }
    }

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

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        EnsureWordWidths();
        var inner = InnerAvailable(available);

        // Minimal — squeezed to the longest single word (text would wrap there
        // and not narrower). Height at that minimal width is one line.
        double minWidth = LongestWordWidth();
        double singleLineHeight = RowHeight(1);

        // Recommended — wrap to whatever width the parent offered. If the parent
        // didn't offer one (rare), fall back to the unwrapped single-line width.
        double wrapWidth = inner.Width > 0 ? inner.Width : Font.MeasureText(Text.Replace('\n', ' '), FontSize);
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, wrapWidth);
        double recWidth = 0;
        foreach (string line in lines)
        {
            recWidth = System.Math.Max(recWidth, Font.MeasureText(line, FontSize));
        }
        double recHeight = RowHeight(lines.Count);

        return WithOwnInset(new SpaceDimension(
            new SizeRect(minWidth, singleLineHeight),
            new SizeRect(recWidth, recHeight),
            verticalBreakable: true));
    }

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        EnsureWordWidths();
        PublishWordWidths(context.WordWidthCache);
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, available.Width);
        double leading = Leading;
        var metrics = Font.GetVerticalMetrics(FontSize);
        double glyphBox = metrics.Ascent + metrics.Descent;
        // How many lines fit in `available.Height` given the formula:
        // height(n) = glyphBox + (n−1)·leading  ⇒  n = 1 + (available − glyphBox) / leading.
        // FitTolerance keeps the floor honest when available.Height is exactly
        // RowHeight(N): IEEE-754 noise in (N-1)·leading can leave the ratio at
        // (N-1) − ε, which would otherwise floor to N-2 and silently truncate.
        int maxLines = available.Height + FitTolerance >= glyphBox
            ? System.Math.Max(1, 1 + (int)System.Math.Floor((available.Height - glyphBox + FitTolerance) / leading))
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
        var widths = EnsureWordWidths();
        double max = 0;
        foreach (var width in widths.Values)
        {
            if (width > max) max = width;
        }
        return max;
    }
}
