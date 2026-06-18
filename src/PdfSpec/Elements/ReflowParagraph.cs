using System.Text;
using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// A <see cref="FamilyParagraph"/> that supports floated <see cref="Element"/>
/// blocks — text wraps around rectangular regions reserved by
/// <see cref="Float(Element, ReflowSide, double, double)"/>. The fluent
/// text surface (<c>.Text</c>, <c>.Bold</c>, <c>.Italic</c>, <c>.BoldItalic</c>,
/// <c>.Newline</c>) is inherited and reshaped to return
/// <see cref="ReflowParagraph"/> so the chain stays on the derived type.
///
/// <para>
/// Each <c>Float()</c> call forces a line break; the float's top sits at
/// the next line's cursor. Subsequent lines reduce their horizontal range
/// to clear the float until the line cursor passes the float's bottom,
/// after which the text resumes full-width. Multiple same-side floats
/// stack vertically.
/// </para>
/// </summary>
public sealed class ReflowParagraph : FamilyParagraph
{
    private const double SupRiseFraction = 0.33;
    private const double SubRiseFraction = 0.20;

    // Float markers stored with the span index at which they were
    // inserted. BuildItems interleaves them with text at render time.
    private readonly List<FloatMarker> _floats = new();
    private ContentIterator<ReflowItem>? _iterator;

    public ReflowParagraph(FontFamily family, double fontSize) : base(family, fontSize) { }

    public ReflowParagraph(FontFamily family, double fontSize, Action<ReflowParagraph> build)
        : base(family, fontSize)
    {
        build(this);
    }

    /// <summary>
    /// Insert a floated <paramref name="element"/> of <paramref name="width"/>
    /// × <paramref name="height"/> anchored to <paramref name="side"/>. The
    /// float occupies a rectangular region of the paragraph's layout, and
    /// subsequent lines wrap around it. Forces a line break — the float
    /// starts on the line that would have been next.
    /// </summary>
    public ReflowParagraph Float(Element element, ReflowSide side, double width, double height)
    {
        _floats.Add(new FloatMarker(Spans.Count, element, side, width, height));
        _iterator = null;
        return this;
    }

    // ===== Narrowed return types =============================================

    public new ReflowParagraph Text(string text, Font? font = null, double? size = null,
        TextAlignment align = TextAlignment.Baseline)
    {
        base.Text(text, font, size, align);
        return this;
    }

    public new ReflowParagraph Bold(string text, double? size = null, TextAlignment align = TextAlignment.Baseline)
    {
        base.Bold(text, size, align);
        return this;
    }

    public new ReflowParagraph Italic(string text, double? size = null, TextAlignment align = TextAlignment.Baseline)
    {
        base.Italic(text, size, align);
        return this;
    }

    public new ReflowParagraph Italics(string text, double? size = null, TextAlignment align = TextAlignment.Baseline)
        => Italic(text, size, align);

    public new ReflowParagraph BoldItalic(string text, double? size = null, TextAlignment align = TextAlignment.Baseline)
    {
        base.BoldItalic(text, size, align);
        return this;
    }

    public new ReflowParagraph Newline()
    {
        base.Newline();
        return this;
    }

    // ===== Layout ============================================================

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        // Inherit base text metrics, then widen by the widest float so a
        // narrow column doesn't decide it can render us when no float can fit.
        var baseHint = base.SizeHint(available);
        double minW = baseHint.MinWidth;
        foreach (var f in _floats)
        {
            if (f.Width > minW) minW = f.Width;
        }
        return new PdfSizeHint(minW, baseHint.MinHeight, baseHint.MaxWidth, baseHint.MaxHeight);
    }

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        _iterator ??= new ContentIterator<ReflowItem>(BuildItems());

        if (_iterator.Done) return RenderResult.Done(0);

        var text = cs.AddText(Font, FontSize);
        if (Color is { } colour) text.SetFillColor(colour);

        Font currentFont = Font;
        double currentSize = FontSize;
        double currentRise = 0;

        double yTop = 0;
        var activeFloats = new List<ActiveFloat>();
        var placedFloats = new List<ActiveFloat>();
        var lineRuns = new List<LineRun>();
        var consumedThisLine = new List<ReflowItem>();
        bool aborted = false;

        while (!_iterator.Done && !aborted)
        {
            // Skip floats fully above yTop — clear them out before laying out lines.
            for (int i = activeFloats.Count - 1; i >= 0; i--)
                if (activeFloats[i].Bottom <= yTop)
                    activeFloats.RemoveAt(i);

            // Horizontal range for this line, shrunk by active floats.
            ComputeLineHorizontalRange(activeFloats, yTop, available.Width, out double leftBound, out double rightBound);
            double lineWidth = rightBound - leftBound;

            if (lineWidth <= 0)
            {
                // Floats meet in the middle — skip down to the nearest float clear.
                double nextClear = double.PositiveInfinity;
                foreach (var f in activeFloats)
                    if (f.Bottom < nextClear) nextClear = f.Bottom;
                if (double.IsPositiveInfinity(nextClear))
                    break; // shouldn't happen — no floats but lineWidth <= 0
                if (nextClear > available.Height)
                {
                    // No room to clear on this page — bail.
                    break;
                }
                yTop = nextClear;
                continue;
            }

            lineRuns.Clear();
            consumedThisLine.Clear();
            bool consumed = TakeReflowLine(lineWidth, lineRuns, consumedThisLine,
                out bool hardNewline, out FloatMarker? floatHit);
            if (!consumed && !hardNewline && floatHit is null) break;

            // Float marker hit — anchor the float at *this* line's yTop
            // (not after a forced line break). If it would land at the
            // current cursor, roll back any text we tentatively consumed
            // and re-take the line with the float's bounds now active —
            // the text wraps tight against the float starting on the very
            // line where the float was declared.
            if (floatHit is { } fh)
            {
                // Stack against any same-side active float so we don't overlap.
                double floatTop = yTop;
                foreach (var af in activeFloats)
                    if (af.Side == fh.Side && af.Bottom > floatTop)
                        floatTop = af.Bottom;

                if (floatTop + fh.Height > available.Height)
                {
                    // Float doesn't fit on this page — defer the marker
                    // and any tentatively-consumed text to the continuation.
                    _iterator.Putback(new FloatItem(fh));
                    for (int i = consumedThisLine.Count - 1; i >= 0; i--)
                        if (consumedThisLine[i] is not FloatItem)
                            _iterator.Putback(consumedThisLine[i]);
                    aborted = true;
                    break;
                }

                double left, right;
                if (fh.Side == ReflowSide.Left)
                {
                    left = 0;
                    right = fh.Width;
                }
                else
                {
                    right = available.Width;
                    left = available.Width - fh.Width;
                }

                var af2 = new ActiveFloat(fh.Element, fh.Side, left, right, floatTop, floatTop + fh.Height);
                activeFloats.Add(af2);
                placedFloats.Add(af2);

                if (floatTop == yTop && lineRuns.Count > 0)
                {
                    // Tight case: text was tentatively consumed at the old
                    // (wider) bounds. Put the text back; the next iteration
                    // re-takes this line with the new float now active.
                    // The float marker itself stays consumed.
                    for (int i = consumedThisLine.Count - 1; i >= 0; i--)
                        if (consumedThisLine[i] is not FloatItem)
                            _iterator.Putback(consumedThisLine[i]);
                    continue;
                }

                // Stacked / deferred case: float anchors below current cursor,
                // so the line we just took is fine as-is. Fall through to emit
                // the text (if any), then loop.
            }

            // Emit any text accumulated on this line.
            if (lineRuns.Count > 0)
            {
                ComputeLineMetrics(lineRuns, out double lineAscent, out double lineDescent);
                double lineHeight = lineAscent + lineDescent;
                if (yTop + lineHeight > available.Height)
                {
                    // Line doesn't fit — roll back the consumed items so the
                    // continuation picks up exactly here.
                    RollbackConsumed(consumedThisLine, null);
                    break;
                }

                double baselineY = yTop + lineAscent;
                text.SetBaseline(leftBound, baselineY);

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
        }

        if (currentRise != 0) text.SetTextRise(0);
        text.Build();

        // Render each placed float into its assigned sub-stream rectangle.
        // For Element floats — the common case from Element.Container(...) —
        // we force Width/Height to match the reservation so the box visibly
        // fills the rectangle the text is wrapping around. Without this the
        // Element defaults to "shrink height to content" and the visible
        // box can be much shorter than the reservation, leaving an empty band
        // below the float that the text correctly (but confusingly) avoids.
        foreach (var f in placedFloats)
        {
            double fw = f.Right - f.Left;
            double fh = f.Bottom - f.Top;
            if (f.Element is Element be) be.Width(fw).Height(fh);
            var sub = cs.CreateSubStream(f.Left, f.Top, fw, fh);
            f.Element.Render(sub, new PdfSize(fw, fh));
            sub.Build();
        }

        // The paragraph's rendered extent must cover both the text bottom
        // and the bottommost placed float — otherwise the surrounding
        // layout puts the next item on top of a float that extends past
        // the text.
        double height = yTop;
        foreach (var f in placedFloats)
            if (f.Bottom > height) height = f.Bottom;

        if (_iterator.Done) return RenderResult.Done(height);

        // Continuation — wrap the remaining iterator into a fresh ReflowParagraph
        // that resumes from there. The continuation inherits font/colour but
        // doesn't carry forward the active floats — they were either rendered on
        // this page or rolled back as a deferred FloatItem.
        var cont = new ReflowParagraph(Family, FontSize)
        {
            Color = Color,
            Font = Font,
            _iterator = _iterator,
        };
        return new RenderResult(height, cont);
    }

    private void RollbackConsumed(List<ReflowItem> consumed, FloatMarker? floatHit)
    {
        if (floatHit is not null)
            _iterator!.Putback(new FloatItem(floatHit));
        for (int i = consumed.Count - 1; i >= 0; i--)
            _iterator!.Putback(consumed[i]);
    }

    private static void ComputeLineHorizontalRange(List<ActiveFloat> active, double yTop, double availableWidth,
        out double leftBound, out double rightBound)
    {
        leftBound = 0;
        rightBound = availableWidth;
        foreach (var f in active)
        {
            // A float that hasn't started yet (e.g. stacked below another
            // same-side float) doesn't constrain this line.
            if (f.Top > yTop) continue;
            if (f.Side == ReflowSide.Left)
            {
                if (f.Right > leftBound) leftBound = f.Right;
            }
            else
            {
                if (f.Left < rightBound) rightBound = f.Left;
            }
        }
    }

    private static void ComputeLineMetrics(List<LineRun> runs, out double lineAscent, out double lineDescent)
    {
        lineAscent = 0;
        lineDescent = 0;
        foreach (var run in runs)
        {
            var m = run.Font.GetVerticalMetrics(run.Size);
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
            TextAlignment.Top => lineAscent - m.Ascent,
            TextAlignment.Bottom => m.Descent - lineDescent,
            TextAlignment.Middle => (lineAscent - lineDescent) / 2.0 - (m.Ascent - m.Descent) / 2.0,
            _ => 0,
        };
    }

    /// <summary>
    /// Consume items from the iterator until: the next word would overflow
    /// <paramref name="width"/>, a hard newline arrives, or a float marker
    /// arrives. Records consumed items in <paramref name="consumed"/> so the
    /// caller can roll back on vertical overflow. <paramref name="floatHit"/>
    /// is set when a float marker is the reason we stopped.
    /// </summary>
    private bool TakeReflowLine(double width, List<LineRun> runs, List<ReflowItem> consumed,
        out bool hardNewline, out FloatMarker? floatHit)
    {
        hardNewline = false;
        floatHit = null;
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

        while (_iterator!.TryPeek(out var item))
        {
            switch (item)
            {
                case NewlineItem:
                    _iterator.MoveNext();
                    consumed.Add(item);
                    hardNewline = true;
                    FlushRun();
                    return runs.Count > 0 || true;
                case FloatItem fi:
                    _iterator.MoveNext();
                    consumed.Add(item);
                    floatHit = fi.Marker;
                    FlushRun();
                    return runs.Count > 0 || true;
                case TextItem ti:
                {
                    double wWidth = ti.Font.MeasureText(ti.Word, ti.Size);
                    double spaceBefore = x == 0 ? 0 : ti.Font.MeasureText(" ", ti.Size);
                    double nextX = x + spaceBefore + wWidth;
                    if (nextX > width)
                    {
                        if (x == 0)
                        {
                            _iterator.MoveNext();
                            consumed.Add(item);
                            runs.Add(new LineRun(ti.Word, ti.Font, ti.Size, ti.Align));
                            return true;
                        }
                        FlushRun();
                        return runs.Count > 0;
                    }

                    bool newRun = runFont is null
                                  || !ReferenceEquals(runFont, ti.Font)
                                  || runSize != ti.Size
                                  || runAlign != ti.Align;
                    if (newRun)
                    {
                        FlushRun();
                        runText = new StringBuilder();
                        runFont = ti.Font;
                        runSize = ti.Size;
                        runAlign = ti.Align;
                        if (x > 0) runText.Append(' ');
                    }
                    else if (runText!.Length > 0)
                    {
                        runText.Append(' ');
                    }
                    runText!.Append(ti.Word);

                    x = nextX;
                    _iterator.MoveNext();
                    consumed.Add(item);
                    break;
                }
            }
        }
        FlushRun();
        return runs.Count > 0;
    }

    /// <summary>
    /// Interleave text items (split from <see cref="Paragraph.Spans"/>) and
    /// the float markers recorded by <see cref="Float"/> at their span-index
    /// anchors, producing the flat <see cref="ReflowItem"/> stream consumed
    /// by the layout walker.
    /// </summary>
    private List<ReflowItem> BuildItems()
    {
        var items = new List<ReflowItem>();
        int floatIdx = 0;
        var spans = Spans;
        for (int s = 0; s < spans.Count; s++)
        {
            // Inject any floats anchored at-or-before this span.
            while (floatIdx < _floats.Count && _floats[floatIdx].SpanIndex <= s)
            {
                items.Add(new FloatItem(_floats[floatIdx]));
                floatIdx++;
            }
            var span = spans[s];
            if (span.IsNewline)
            {
                items.Add(new NewlineItem());
                continue;
            }
            var lines = span.Text.Split('\n');
            for (int li = 0; li < lines.Length; li++)
            {
                if (li > 0) items.Add(new NewlineItem());
                foreach (var w in lines[li].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    items.Add(new TextItem(w, span.Font, span.Size, span.Align));
                }
            }
        }
        // Trailing floats (declared after the last span).
        while (floatIdx < _floats.Count)
        {
            items.Add(new FloatItem(_floats[floatIdx]));
            floatIdx++;
        }
        return items;
    }

    // ===== Nested types ======================================================

    private sealed record FloatMarker(int SpanIndex, Element Element, ReflowSide Side, double Width, double Height);

    private abstract record ReflowItem;
    private sealed record TextItem(string Word, Font Font, double Size, TextAlignment Align) : ReflowItem;
    private sealed record NewlineItem : ReflowItem;
    private sealed record FloatItem(FloatMarker Marker) : ReflowItem;

    private sealed record LineRun(string Text, Font Font, double Size, TextAlignment Align);

    private sealed record ActiveFloat(Element Element, ReflowSide Side,
        double Left, double Right, double Top, double Bottom);
}
