namespace CSharpPdf.Layout;

/// <summary>
/// A slot inside a Rows or Cols: carries the sizing intent (<see cref="Sizing"/>)
/// and length value, optional content, plus the shared UI styling from
/// <see cref="UIElement"/>. A slot always fills the size its parent allocates (so
/// a coloured background spans the full allocation, not just the content), and is
/// the unit the parent paginates on.
/// </summary>
public sealed class SlotElement : UIElement
{
    /// <summary>How this slot is sized within its parent.</summary>
    public Sizing Sizing { get; set; } = Sizing.Auto;

    /// <summary>For <c>Sizing.Fixed</c>: length in <see cref="Unit"/>. For <c>Sizing.Relative</c>: the weight (defaults to 1).</summary>
    public double Length { get; set; } = 1;

    /// <summary>Length unit when <see cref="Sizing"/> is <c>Fixed</c>.</summary>
    public Unit Unit { get; set; } = Unit.Px;

    /// <summary>The element drawn inside this slot. <c>null</c> = an empty coloured band.</summary>
    public UIElement? Content { get; set; }

    public SlotElement() { }
    public SlotElement(UIElement content) { Content = content; }

    public override SpaceDimension SpaceRequired(SizeRect available)
    {
        double inset = Padding + BorderThickness;
        var inner = InnerAvailable(available);
        SpaceDimension contentSpace = Content?.SpaceRequired(inner)
            ?? new SpaceDimension(SizeRect.Zero, SizeRect.Zero, verticalBreakable: false);

        // Sizing intent overrides the natural height: Fixed locks it, Relative
        // collapses to inset only (parent decides the share), Auto passes through.
        double recHeight = Sizing switch
        {
            Sizing.Fixed => Length,
            Sizing.Relative => 2 * inset,
            _ => (contentSpace.Recommended.Height ?? 0) + 2 * inset,
        };
        double minHeight = Sizing switch
        {
            Sizing.Fixed => Length,
            Sizing.Relative => 2 * inset,
            _ => (contentSpace.Minimal.Height ?? 0) + 2 * inset,
        };
        double recWidth = contentSpace.Recommended.Width + 2 * inset;
        double minWidth = contentSpace.Minimal.Width + 2 * inset;

        return new SpaceDimension(
            new SizeRect(minWidth, minHeight),
            new SizeRect(recWidth, recHeight),
            contentSpace.VerticalBreakable);
    }

    public override RenderResult Render(PdfContext context, Size available)
    {
        CSharpPdf.LayoutTrace.Mark($"Slot.Render sizing={Sizing} length={Length:F1} avail=({available.Width:F1},{available.Height:F1}) content={Content?.GetType().Name ?? "null"}");
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
        context.Cursor = new Point(box.X + inset, box.Y - inset);
        var result = Content.Render(context, inner);
        context.Cursor = next;

        if (result.Overflow is { } overflow)
        {
            // The content deferred itself (returned the same instance). For a Fixed
            // slot the allocation can't grow, so propagating the deferral as overflow
            // would loop forever — each page would allocate the same too-small length
            // and report fake "progress". Drop the content instead so the slot keeps
            // its background and rendering moves on. Auto/Relative slots may simply
            // need a fresh page to fit the content, so propagate normally.
            if (ReferenceEquals(overflow, Content) && Sizing == Sizing.Fixed)
            {
                return new RenderResult(null, next);
            }
            var rest = new SlotElement
            {
                Sizing = Sizing,
                Length = Length,
                Unit = Unit,
                Content = overflow,
            };
            CopyStyleTo(rest);
            return new RenderResult(rest, next);
        }
        return new RenderResult(null, next);
    }

    // Render is overridden directly; RenderCore still has to exist for the base abstract.
    protected override RenderResult RenderCore(PdfContext context, Size available) => Render(context, available);
}
