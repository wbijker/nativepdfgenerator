using CSharpPdf.Content;
namespace CSharpPdf.Layout;

/// <summary>
/// A slot inside a Rows or Cols: carries the sizing intent (<see cref="Sizing"/>)
/// and length value, optional content, plus the shared UI styling from
/// <see cref="Element"/>. A slot always fills the size its parent allocates (so
/// a coloured background spans the full allocation, not just the content), and is
/// the unit the parent paginates on.
/// </summary>
public sealed class SlotElement : Element
{
    /// <summary>How this slot is sized within its parent.</summary>
    public Sizing Sizing { get; set; } = Sizing.Auto;

    /// <summary>For <c>Sizing.Fixed</c>: length in <see cref="Unit"/>. For <c>Sizing.Relative</c>: the weight (defaults to 1).</summary>
    public double Length { get; set; } = 1;

    /// <summary>Length unit when <see cref="Sizing"/> is <c>Fixed</c>.</summary>
    public Unit Unit { get; set; } = Unit.Pt;

    /// <summary>The element drawn inside this slot. <c>null</c> = an empty coloured band.</summary>
    public Element? Content { get; set; }

    /// <summary>
    /// When true, the slot reports <c>VerticalBreakable=false</c> from
    /// <see cref="SpaceHint"/> regardless of its content, and defers to the
    /// next page (rather than letting its content render partially) if its
    /// recommended height doesn't fit in the parent's allocation. Used by
    /// Column items so each item lands whole on a single page — its
    /// background and border never appear without their content.
    /// </summary>
    public bool Atomic { get; set; }

    public SlotElement() { }
    public SlotElement(Element content) { Content = content; }

    // One-shot memoization for SpaceHint. SlotElement's result depends only on
    // the available *width* (text wraps at width; rows/cols hand it down) and
    // the Content's intrinsic measurements — not on available.Height. So we
    // key by (Content reference, width) and ignore the height. The parent
    // ComputeHeights pass passes Height=null, while the slot's own Atomic-
    // check passes Height=available.Height: same width → same answer.
    private object? _cachedContentRef;
    private double _cachedAvailWidth;
    private SpaceDimension? _cachedSpaceHint;

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        Perf.Inc("SlotElement.SpaceHint");
        if (_cachedSpaceHint is not null
            && ReferenceEquals(_cachedContentRef, Content)
            && _cachedAvailWidth == available.Width)
        {
            Perf.Inc("SlotElement.SpaceHint.hit");
            return _cachedSpaceHint;
        }
        Perf.Inc("SlotElement.SpaceHint.miss");

        // Slot always reports the *content's* natural outer extent plus slot
        // padding. The Sizing/Length intent is interpreted by the parent
        // container — RowsElement.ComputeHeights / ColsElement.ComputeWidths
        // read slot.Sizing directly to decide how much room to give this slot
        // along their distribution axis.
        double inset = Padding + BorderThickness;
        var inner = InnerAvailable(available);
        SpaceDimension contentSpace = Content?.SpaceHint(inner)
            ?? new SpaceDimension(SizeRect.Zero, SizeRect.Zero, verticalBreakable: false);

        double recHeight = (contentSpace.Recommended.Height ?? 0) + 2 * inset;
        double minHeight = (contentSpace.Minimal.Height ?? 0) + 2 * inset;
        double recWidth = contentSpace.Recommended.Width + 2 * inset;
        double minWidth = contentSpace.Minimal.Width + 2 * inset;

        var result = new SpaceDimension(
            new SizeRect(minWidth, minHeight),
            new SizeRect(recWidth, recHeight),
            !Atomic && contentSpace.VerticalBreakable);

        _cachedContentRef = Content;
        _cachedAvailWidth = available.Width;
        _cachedSpaceHint = result;
        return result;
    }

    public override RenderResult Render(PdfCanvas context, Size available)
    {
        Perf.Inc("SlotElement.Render");
        CSharpPdf.LayoutTrace.Mark($"Slot.Render sizing={Sizing} length={Length:F1} avail=({available.Width:F1},{available.Height:F1}) content={Content?.GetType().Name ?? "null"}");

        // Atomic slot: if the recommended height doesn't fit in the parent's
        // allocation, defer the whole slot to the next page rather than
        // drawing the background/border now and the content next time.
        // ForceRender bypasses this (engine flips it when an atomic slot
        // deferred on a fresh empty page).
        if (Atomic && !context.ForceRender)
        {
            var space = SpaceHint(new SizeRect(available.Width, available.Height));
            double recommended = space.Recommended.Height ?? 0;
            if (available.Height + FitTolerance < recommended)
            {
                return new RenderResult(this, context.Cursor); // defer
            }
        }

        Point box = context.Cursor;
        if (Background is { } bg)
        {
            if (BorderRadius > 0)
                context.FillRoundedRectangle(box.X, box.Y, available.Width, available.Height, bg, BorderRadius);
            else
                context.FillRectangle(box.X, box.Y, available.Width, available.Height, bg);
        }
        if (BorderColor is { } border && BorderThickness > 0)
        {
            if (BorderRadius > 0 || BorderDash is { Length: > 0 })
                context.StrokeRoundedRectangle(box.X, box.Y, available.Width, available.Height, border, BorderThickness, BorderRadius, BorderDash);
            else
                context.StrokeRectangle(box.X, box.Y, available.Width, available.Height, border, BorderThickness);
        }

        var next = new Point(box.X, box.Y - available.Height);
        if (Content is null)
        {
            context.Cursor = next;
            return new RenderResult(null, next);
        }

        double inset = Padding + BorderThickness;
        var inner = new Size(Max0(available.Width - 2 * inset), Max0(available.Height - 2 * inset));

        // Apply the slot's HAlign by pre-shifting the cursor and narrowing the
        // available width handed to Content so the content's own HAlign cannot
        // double-shift. With HAlign=Left this is a no-op (offsetX=0, content
        // gets the full inner width) — keeping existing samples byte-identical.
        double offsetX = 0;
        Size contentAvailable = inner;
        if (HAlign != HorizontalAlignment.Left)
        {
            var contentSpace = Content.SpaceHint(new SizeRect(inner.Width, inner.Height));
            double contentNatural = System.Math.Min(contentSpace.Recommended.Width, inner.Width);
            double extra = System.Math.Max(0, inner.Width - contentNatural);
            offsetX = HAlign switch
            {
                HorizontalAlignment.Center => extra / 2,
                HorizontalAlignment.Right => extra,
                _ => 0,
            };
            contentAvailable = new Size(contentNatural, inner.Height);
        }
        var contentStart = new Point(box.X + inset + offsetX, box.Y - inset);
        context.Cursor = contentStart;
        var result = Content.Render(context, contentAvailable);
        context.Cursor = next;

        if (result.Overflow is { } overflow)
        {
            bool pureDefer = ReferenceEquals(overflow, Content);
            // Detect content that produced a NEW continuation but didn't actually
            // make any progress (e.g. RowsElement whose first atomic slot deferred
            // and was simply re-wrapped). Treat as pure defer to avoid the slot
            // consuming the whole available height for nothing — which would
            // appear as progress to the engine and loop indefinitely.
            bool noContentProgress = result.Next.Y >= contentStart.Y - 0.01;
            if (noContentProgress && !pureDefer)
            {
                pureDefer = true;
            }
            if (pureDefer && Sizing == Sizing.Fixed)
            {
                // Fixed slot, content deferred. The allocation can't grow, so
                // propagating would loop forever — drop the content and let the
                // slot keep its allocated space (background already drawn).
                FireOnRendered(context, box, available);
                return new RenderResult(null, next);
            }
            if (pureDefer)
            {
                // Content rendered NOTHING — it returned itself as overflow. Don't
                // fake-advance the cursor: the engine needs to see "no progress"
                // so it can move to the next page or flip ForceRender. Return the
                // slot as overflow at the *original* cursor position.
                // For noContentProgress we substitute the continuation for our
                // own Content so the next attempt starts where this one stopped.
                if (noContentProgress && !ReferenceEquals(overflow, Content))
                {
                    Content = overflow;
                }
                return new RenderResult(this, box);
            }
            var rest = new SlotElement
            {
                Sizing = Sizing,
                Length = Length,
                Unit = Unit,
                Content = overflow,
            };
            CopyStyleTo(rest);
            FireOnRendered(context, box, available);
            return new RenderResult(rest, next);
        }
        FireOnRendered(context, box, available);
        return new RenderResult(null, next);
    }

    private void FireOnRendered(PdfCanvas canvas, Point box, Size available)
    {
        if (OnRendered is not { } hook) return;
        double absX = canvas.ToAbsoluteX(box.X);
        double absTop = canvas.ToAbsoluteY(box.Y);
        var pos = new Point(absX, absTop);
        hook(new RenderedInfo(pos, canvas.PageNumber, new Boundary(absX, absTop, available.Width, available.Height)));
    }

    // Render is overridden directly; RenderCore still has to exist for the base abstract.
    protected override RenderResult RenderCore(PdfCanvas context, Size available) => Render(context, available);
}
