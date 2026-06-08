using CSharpPdf.Content;
using PdfSpec.Fonts;
using Font = PdfSpec.Fonts.Font;

using PdfSpec.Geometry;
namespace CSharpPdf.Layout;



/// <summary>Flowing, word-wrapped text. Renders the lines that fit and returns the rest as overflow.</summary>
public sealed class TextElement : Element
{
    public string Text { get; set; } = "";
    public Font Font { get; set; } = StandardFont.Helvetica;
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
    public bool SaveMetric { get; set; } = ForceSaveMetric;

    /// <summary>Test hook: force-enable SaveMetric on every TextElement to benchmark cache impact.</summary>
    public static bool ForceSaveMetric = false;

    public TextElement() { }
    public TextElement(string text) { Text = text ?? ""; }
    public TextElement(string text, Font font, double fontSize) { Text = text ?? ""; Font = font; FontSize = fontSize; }

    private double Leading => LineHeight ?? FontSize * 1.2;

    // Process-wide word-width cache, keyed by (font PostScript name, font size, word).
    // Every TextElement consults this on its first measurement and contributes
    // any newly-seen words, so each (font, size, word) tuple is measured
    // exactly once across the entire run, regardless of which element first
    // sees it. Single-threaded by design — matches the rest of CSharpPdf.
    private static readonly Dictionary<(string Font, double Size, string Word), double> _globalWordWidths = new();

    /// <summary>Diagnostic: number of unique (font,size,word) tuples cached so far.</summary>
    public static int GlobalWordCacheSize => _globalWordWidths.Count;

    /// <summary>Diagnostic: empty the global cache (use between benchmark runs).</summary>
    public static void ClearGlobalWordCache() => _globalWordWidths.Clear();

    // Per-element snapshot of the words in this Text, mapped to their widths in
    // the active Font/FontSize. Populated lazily by EnsureWordWidths from
    // _globalWordWidths and reused across SpaceHint / RenderCore calls — so a
    // single element never re-iterates its words after the first call.
    private Dictionary<string, double>? _wordWidths;
    private string? _measuredText;
    private string? _measuredFontKey;
    private double _measuredFontSize;
    private double _longestWordWidth;

    /// <summary>Snapshot of the per-word width measurements (null until the first measurement query). Exposed for tests/diagnostics.</summary>
    public IReadOnlyDictionary<string, double>? WordWidths => _wordWidths;

    /// <summary>
    /// Pre-measure every distinct word in <see cref="Text"/> at the current
    /// <see cref="Font"/>/<see cref="FontSize"/>, consulting the process-wide
    /// <see cref="_globalWordWidths"/> cache first. Idempotent at the element
    /// level: a second call with the same text/font/size returns immediately.
    /// Also records <see cref="_longestWordWidth"/> in the same pass so
    /// <see cref="LongestWordWidth"/> doesn't scan the dictionary later.
    /// </summary>
    private Dictionary<string, double> EnsureWordWidths()
    {
        var __t = Perf.Start();
        string fontKey = Font.BaseFont;
        if (_wordWidths is not null
            && _measuredText == Text
            && _measuredFontKey == fontKey
            && _measuredFontSize == FontSize)
        {
            Perf.End("TextElement.EnsureWordWidths.hit", __t);
            return _wordWidths;
        }
        Perf.Inc("TextElement.EnsureWordWidths.miss");
        var widths = new Dictionary<string, double>();
        double longest = 0;
        foreach (var word in Text.Split(' ', '\n'))
        {
            if (word.Length == 0 || widths.ContainsKey(word)) continue;
            var key = (fontKey, FontSize, word);
            if (!_globalWordWidths.TryGetValue(key, out var w))
            {
                w = Font.MeasureText(word, FontSize);
                _globalWordWidths[key] = w;
                Perf.Inc("TextElement.GlobalWordCache.miss");
            }
            else
            {
                Perf.Inc("TextElement.GlobalWordCache.hit");
            }
            widths[word] = w;
            if (w > longest) longest = w;
        }
        _wordWidths = widths;
        _longestWordWidth = longest;
        _measuredText = Text;
        _measuredFontKey = fontKey;
        _measuredFontSize = FontSize;
        Perf.End("TextElement.EnsureWordWidths.miss.time", __t);
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

    // Two-entry MRU cache. Each entry stores the *full* wrap result for a
    // given wrap width: the SpaceDimension AND the wrapped lines (which
    // RenderCore needs). Parents (RowsElement.ComputeHeights, ColsElement
    // RenderCore, SlotElement Atomic check) re-ask the same element at the
    // same width 3-4 times per render, and then RenderCore draws at the same
    // width again — caching the lines too saves the wrap repeat at draw time.
    // ColsElement.ComputeWidths additionally probes Auto slots at
    // width=infinity, so two entries catch both patterns.
    private sealed class WrapEntry
    {
        public double Width;
        public SpaceDimension Space;
        public List<string> Lines = null!;
    }
    private WrapEntry? _wrap0;
    private WrapEntry? _wrap1;

    /// <summary>
    /// Lookup or compute the full wrap result (SpaceDimension + wrapped lines)
    /// at the given <paramref name="wrapWidth"/>. Used by both SpaceHint and
    /// RenderCore so they share a single wrap per (element, width).
    /// </summary>
    private WrapEntry EnsureWrap(double wrapWidth)
    {
        if (_wrap0 is not null && _wrap0.Width == wrapWidth)
        {
            Perf.Inc("TextElement.Wrap.hit");
            return _wrap0;
        }
        if (_wrap1 is not null && _wrap1.Width == wrapWidth)
        {
            (_wrap0, _wrap1) = (_wrap1, _wrap0);
            Perf.Inc("TextElement.Wrap.hit");
            return _wrap0;
        }
        Perf.Inc("TextElement.Wrap.miss");

        var widths = EnsureWordWidths();

        double minWidth = LongestWordWidth();
        double singleLineHeight = RowHeight(1);

        double effectiveWrapWidth = wrapWidth > 0 ? wrapWidth : Font.MeasureText(Text.Replace('\n', ' '), FontSize);
        // Use the running-width wrapper that shares our per-word cache and
        // returns per-line widths in one pass.
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, effectiveWrapWidth, widths, out var lineWidths);
        double recWidth = 0;
        foreach (var lw in lineWidths)
        {
            if (lw > recWidth) recWidth = lw;
        }
        double recHeight = RowHeight(lines.Count);

        var space = WithOwnInset(new SpaceDimension(
            new SizeRect(minWidth, singleLineHeight),
            new SizeRect(recWidth, recHeight),
            verticalBreakable: true));

        var entry = new WrapEntry { Width = wrapWidth, Space = space, Lines = lines };
        _wrap1 = _wrap0;
        _wrap0 = entry;
        return entry;
    }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var __t = Perf.Start();
        var inner = InnerAvailable(available);
        double wrapWidth = inner.Width;
        var entry = EnsureWrap(wrapWidth);
        Perf.End("TextElement.SpaceHint", __t);
        return entry.Space;
    }

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        var __t = Perf.Start();
        EnsureWordWidths();
        PublishWordWidths(context.WordWidthCache);
        var lines = EnsureWrap(available.Width).Lines;
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
            Perf.End("TextElement.RenderCore", __t);
            return new RenderResult(overflow, next);
        }
        Perf.End("TextElement.RenderCore", __t);
        return new RenderResult(null, next);
    }

    private double LongestWordWidth()
    {
        EnsureWordWidths();
        return _longestWordWidth;
    }
}
