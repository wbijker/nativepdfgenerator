using System.Text;
using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// A flowed text element. Single-string form
/// (<c>new Paragraph(text, font, size)</c>) renders <paramref name="text"/>
/// in one face; the lambda form (<c>new Paragraph(font, size, t => …)</c>)
/// and the family form (<see cref="FamilyParagraph"/>) build a multi-span
/// paragraph that mixes faces and sizes within a single wrapped flow.
///
/// <para>
/// Every span carries its own <see cref="Fonts.Font"/>, size, and
/// <see cref="TextAlignment"/>. Line height is determined per-line by the
/// max ascent + max descent of the spans actually placed on that line (with
/// <see cref="TextAlignment.Sub"/> / <see cref="TextAlignment.Sup"/> baseline
/// shifts contributing extra height; <see cref="TextAlignment.Top"/> /
/// <see cref="TextAlignment.Middle"/> / <see cref="TextAlignment.Bottom"/>
/// position the span within the line box without growing it).
/// </para>
/// </summary>
public class Paragraph : Element
{
    // Typographic sub/superscript baseline offsets, as a fraction of the
    // span's own font size. Conservative defaults — callers wanting tighter
    // typography pair with a smaller size on the sub/sup span.
    private const double SupRiseFraction = 0.33;
    private const double SubRiseFraction = 0.20;

    private readonly List<TextSpan> _spans = new();
    private ContentIterator<RichWord>? _iterator;

    /// <summary>
    /// Spans accumulated through the fluent builder, exposed read-only to
    /// derived layout classes (e.g. <see cref="ReflowParagraph"/>) that
    /// build their own item stream around the same text content.
    /// </summary>
    private protected IReadOnlyList<TextSpan> Spans => _spans;

    // ===== Constructors ======================================================

    /// <summary>Legacy single-span constructor — one text, one font, one size.</summary>
    public Paragraph(string text, Font font, double fontSize)
    {
        Font = font;
        FontSize = fontSize;
        _spans.Add(new TextSpan(text, font, fontSize, TextAlignment.Baseline, isNewline: false));
    }

    /// <summary>
    /// Low-level lambda form. <paramref name="defaultFont"/> is used by
    /// <c>.Text(string)</c> calls inside the builder when no explicit font
    /// is given; per-span fonts and sizes may be overridden. <paramref name="fontSize"/>
    /// is the default size for spans that don't specify their own.
    /// </summary>
    public Paragraph(Font defaultFont, double fontSize, Action<Paragraph> build)
    {
        Font = defaultFont;
        FontSize = fontSize;
        build(this);
    }

    /// <summary>Internal continuation constructor used by <see cref="Draw"/> when paginating overflow.</summary>
    private Paragraph(ContentIterator<RichWord> iterator, Font defaultFont, double fontSize, PdfColor? color)
    {
        _iterator = iterator;
        Font = defaultFont;
        FontSize = fontSize;
        Color = color;
    }

    /// <summary>Constructor used by derived family-aware variants — pre-seeds the default font + size without adding spans.</summary>
    private protected Paragraph(Font defaultFont, double fontSize)
    {
        Font = defaultFont;
        FontSize = fontSize;
    }

    // ===== Mutable surface (defaults inherited by spans that omit them) ======

    /// <summary>Default font used by <c>.Text(string)</c> calls when no explicit font is given.</summary>
    public Font Font { get; set; }

    /// <summary>Default font size in points used by spans that don't specify their own.</summary>
    public double FontSize { get; set; }

    /// <summary>
    /// Update the paragraph's default <see cref="Font"/> / <see cref="FontSize"/>
    /// AND retag the single text span to match — used by the chainable
    /// <see cref="IText"/> facade. <see cref="Paragraph(string, Font, double)"/>
    /// snapshots the size/font into the span at construction, so later
    /// changes to the defaults wouldn't otherwise reach the rendered glyphs.
    /// No-op when the paragraph holds multiple spans (the caller is mixing
    /// rich text and the single-span fast path no longer applies).
    /// </summary>
    internal void Restyle(Font? font = null, double? size = null)
    {
        if (font is not null) Font = font;
        if (size is not null) FontSize = size.Value;
        if (_spans.Count == 1 && !_spans[0].IsNewline)
        {
            var old = _spans[0];
            _spans[0] = new TextSpan(old.Text, Font, FontSize, old.Align, isNewline: false);
        }
    }

    /// <summary>Fill colour for the glyphs. <c>null</c> = device default (black).</summary>
    public PdfColor? Color { get; set; }

    /// <summary>When true a horizontal rule is drawn under each wrapped line. Currently surface-only — preserved for the fluent <see cref="IText"/> API.</summary>
    public bool Underline { get; set; }

    /// <summary>Horizontal alignment of each wrapped line within the available width. Default <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment TextAlign { get; set; } = HorizontalAlignment.Left;

    /// <summary>Original full text — joined from spans (newline markers become <c>\n</c>).</summary>
    public string RawText => string.Concat(_spans.Select(s => s.IsNewline ? "\n" : s.Text));

    // ===== Chainable span builder ============================================

    /// <summary>
    /// Append a text span. All four span attributes are optional:
    /// <paramref name="font"/> falls back to <see cref="Font"/>,
    /// <paramref name="size"/> to <see cref="FontSize"/>,
    /// <paramref name="align"/> defaults to <see cref="TextAlignment.Baseline"/>.
    /// </summary>
    public Paragraph Text(string text, Font? font = null, double? size = null,
        TextAlignment align = TextAlignment.Baseline)
    {
        _spans.Add(new TextSpan(text, font ?? Font, size ?? FontSize, align, isNewline: false));
        _iterator = null;
        return this;
    }

    /// <summary>Force a line break — the next span starts on a new line regardless of remaining width.</summary>
    public Paragraph Newline()
    {
        _spans.Add(new TextSpan(string.Empty, Font, FontSize, TextAlignment.Baseline, isNewline: true));
        _iterator = null;
        return this;
    }

    // ===== Layout ============================================================

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        double maxWordWidth = 0;
        double maxLineHeight = 0;
        foreach (var span in _spans)
        {
            if (span.IsNewline) continue;
            var metrics = span.Font.GetVerticalMetrics(span.Size);
            double lh = metrics.LineHeight;
            // Sub/sup spans contribute their baseline-shifted extents to line height.
            double rise = span.Align switch
            {
                TextAlignment.Sup => SupRiseFraction * span.Size,
                TextAlignment.Sub => -SubRiseFraction * span.Size,
                _ => 0,
            };
            if (rise != 0)
            {
                double top = Math.Max(0, metrics.Ascent + rise);
                double bot = Math.Max(0, metrics.Descent - rise);
                lh = Math.Max(lh, top + bot);
            }
            if (lh > maxLineHeight) maxLineHeight = lh;

            foreach (var word in span.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                double w = span.Font.MeasureText(word, span.Size);
                if (w > maxWordWidth) maxWordWidth = w;
            }
        }
        if (maxLineHeight == 0)
        {
            // Empty / newline-only paragraph — fall back to the default font's line height.
            maxLineHeight = Font.GetVerticalMetrics(FontSize).LineHeight;
        }
        return new PdfSizeHint(maxWordWidth, maxLineHeight, null, null);
    }

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        _iterator ??= new ContentIterator<RichWord>(Tokenize(_spans));

        if (_iterator.Done) return RenderResult.Done(0);

        var text = cs.AddText(Font, FontSize);
        if (Color is { } colour) text.SetFillColor(colour);

        Font currentFont = Font;
        double currentSize = FontSize;
        double currentRise = 0;

        double yTop = 0;
        var lineRuns = new List<LineRun>();
        var consumedThisLine = new List<RichWord>();

        while (!_iterator.Done)
        {
            lineRuns.Clear();
            consumedThisLine.Clear();
            bool consumed = TakeRichLine(available.Width, lineRuns, consumedThisLine, out bool hardNewline);
            if (!consumed && !hardNewline) break;

            // Compute this line's metrics from the runs.
            ComputeLineMetrics(lineRuns, out double lineAscent, out double lineDescent);
            double lineHeight = lineAscent + lineDescent;

            if (yTop + lineHeight > available.Height)
            {
                // Roll back any words we consumed for this line — they'll
                // lead the continuation Paragraph's render on the next page.
                for (int i = consumedThisLine.Count - 1; i >= 0; i--)
                    _iterator.Putback(consumedThisLine[i]);
                break;
            }

            double xOffset = 0;
            if (TextAlign != HorizontalAlignment.Left)
            {
                double lineWidth = 0;
                foreach (var run in lineRuns) lineWidth += run.Font.MeasureText(run.Text, run.Size);
                double slack = Math.Max(0, available.Width - lineWidth);
                xOffset = TextAlign == HorizontalAlignment.Center ? slack / 2 : slack;
            }

            double baselineY = yTop + lineAscent;
            text.SetBaseline(xOffset, baselineY);

            foreach (var run in lineRuns)
            {
                if (!ReferenceEquals(run.Font, currentFont) || run.Size != currentSize)
                {
                    text.SetFont(run.Font, run.Size);
                    currentFont = run.Font;
                    currentSize = run.Size;
                }
                double rise = ComputeBaselineRise(run, lineAscent, lineDescent);
                if (rise != currentRise)
                {
                    text.SetTextRise(rise);
                    currentRise = rise;
                }
                text.ShowText(run.Text);
            }

            yTop += lineHeight;
        }

        // Reset Ts so the next text block / restore doesn't inherit a stray rise.
        if (currentRise != 0) text.SetTextRise(0);
        text.Build();

        if (_iterator.Done) return RenderResult.Done(yTop);

        var cont = new Paragraph(_iterator, Font, FontSize, Color) { TextAlign = TextAlign };
        return new RenderResult(yTop, cont);
    }

    private static void ComputeLineMetrics(List<LineRun> runs, out double lineAscent, out double lineDescent)
    {
        lineAscent = 0;
        lineDescent = 0;
        foreach (var run in runs)
        {
            var m = run.Font.GetVerticalMetrics(run.Size);
            // Baseline-rise contribution for sub/sup only (top/middle/bottom
            // place within the existing line box and don't grow it).
            double rise = run.Align switch
            {
                TextAlignment.Sup => SupRiseFraction * run.Size,
                TextAlignment.Sub => -SubRiseFraction * run.Size,
                _ => 0,
            };
            double top = Math.Max(0, m.Ascent + rise);
            double bot = Math.Max(0, m.Descent - rise);
            if (top > lineAscent) lineAscent = top;
            if (bot > lineDescent) lineDescent = bot;
        }
    }

    private static double ComputeBaselineRise(LineRun run, double lineAscent, double lineDescent)
    {
        var m = run.Font.GetVerticalMetrics(run.Size);
        return run.Align switch
        {
            TextAlignment.Baseline => 0,
            TextAlignment.Sup => SupRiseFraction * run.Size,
            TextAlignment.Sub => -SubRiseFraction * run.Size,
            // Top: glyph top aligns to line top → rise = lineAscent - spanAscent.
            TextAlignment.Top => lineAscent - m.Ascent,
            // Bottom: glyph bottom aligns to line bottom → rise = spanDescent - lineDescent.
            TextAlignment.Bottom => m.Descent - lineDescent,
            // Middle: glyph centre aligns to line centre.
            TextAlignment.Middle => (lineAscent - lineDescent) / 2.0 - (m.Ascent - m.Descent) / 2.0,
            _ => 0,
        };
    }

    /// <summary>
    /// Greedily consume words from the iterator until the next one would
    /// overflow <paramref name="width"/>, grouping consecutive same-
    /// (font, size, align) words into runs. A single word wider than the
    /// slot is force-emitted on its own line to guarantee forward
    /// progress. Records consumed words in <paramref name="consumed"/> so
    /// caller can roll back if the resulting line doesn't fit vertically.
    /// </summary>
    private bool TakeRichLine(double width, List<LineRun> runs, List<RichWord> consumed, out bool hardNewline)
    {
        hardNewline = false;
        double x = 0;
        Font? runFont = null;
        double runSize = 0;
        TextAlignment runAlign = TextAlignment.Baseline;
        StringBuilder? runText = null;

        void FlushRun()
        {
            if (runText is { Length: > 0 } && runFont is not null)
                runs.Add(new LineRun(runText.ToString(), runFont, runSize, runAlign));
            runText = null;
        }

        while (_iterator!.TryPeek(out var word))
        {
            if (word.IsNewline)
            {
                _iterator.MoveNext();
                consumed.Add(word);
                hardNewline = true;
                break;
            }

            double wWidth = word.Font.MeasureText(word.Word, word.Size);
            double spaceBefore = x == 0 ? 0 : word.Font.MeasureText(" ", word.Size);
            double nextX = x + spaceBefore + wWidth;

            if (nextX > width)
            {
                if (x == 0)
                {
                    // Force-emit an oversized word on its own line.
                    _iterator.MoveNext();
                    consumed.Add(word);
                    runs.Add(new LineRun(word.Word, word.Font, word.Size, word.Align));
                    return true;
                }
                break;
            }

            bool newRun = runFont is null
                          || !ReferenceEquals(runFont, word.Font)
                          || runSize != word.Size
                          || runAlign != word.Align;
            if (newRun)
            {
                FlushRun();
                runText = new StringBuilder();
                runFont = word.Font;
                runSize = word.Size;
                runAlign = word.Align;
                if (x > 0) runText.Append(' ');
            }
            else if (runText!.Length > 0)
            {
                runText.Append(' ');
            }
            runText!.Append(word.Word);

            x = nextX;
            _iterator.MoveNext();
            consumed.Add(word);
        }
        FlushRun();
        return runs.Count > 0;
    }

    private static IReadOnlyList<RichWord> Tokenize(IReadOnlyList<TextSpan> spans)
    {
        var words = new List<RichWord>();
        foreach (var span in spans)
        {
            if (span.IsNewline)
            {
                words.Add(new RichWord(string.Empty, span.Font, span.Size, span.Align, IsNewline: true));
                continue;
            }
            var lines = span.Text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) words.Add(new RichWord(string.Empty, span.Font, span.Size, span.Align, IsNewline: true));
                foreach (var w in lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    words.Add(new RichWord(w, span.Font, span.Size, span.Align, IsNewline: false));
                }
            }
        }
        return words;
    }

    private protected sealed class TextSpan
    {
        public string Text { get; }
        public Font Font { get; }
        public double Size { get; }
        public TextAlignment Align { get; }
        public bool IsNewline { get; }

        public TextSpan(string text, Font font, double size, TextAlignment align, bool isNewline)
        {
            Text = text;
            Font = font;
            Size = size;
            Align = align;
            IsNewline = isNewline;
        }
    }

    private readonly record struct RichWord(string Word, Font Font, double Size, TextAlignment Align, bool IsNewline);

    private sealed record LineRun(string Text, Font Font, double Size, TextAlignment Align);
}
