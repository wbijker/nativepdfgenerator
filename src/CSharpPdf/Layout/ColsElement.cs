using CSharpPdf.Content;
namespace CSharpPdf.Layout;

/// <summary>
/// Lays slots side by side. Each slot's width is its sizing intent: Fixed gives the
/// slot exactly that width, Auto sizes it to its content's natural width, Relative
/// slots share the width left over after Fixed and Auto by weight. Row height is the
/// tallest slot; each slot is positioned vertically within that height per its
/// vertical alignment. The row is placed as a unit (moves to the next page whole).
/// </summary>
public sealed class ColsElement : UIElement
{
    /// <summary>The columns in left-to-right order. Populate via object initializer or .Add.</summary>
    public List<SlotElement> Slots { get; } = new();

    public ColsElement() { }
    internal ColsElement(IEnumerable<SlotElement> slots) { Slots.AddRange(slots); }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var inner = InnerAvailable(available);
        double[] widths = ComputeWidths(inner);
        double totalWidth = 0, maxRecHeight = 0;
        for (int i = 0; i < Slots.Count; i++)
        {
            var s = Slots[i].SpaceHint(new SizeRect(widths[i], inner.Height));
            maxRecHeight = System.Math.Max(maxRecHeight, s.Recommended.Height ?? 0);
            totalWidth += widths[i];
        }
        // A Cols row is atomic — it cannot split vertically across pages.
        return WithOwnInset(new SpaceDimension(
            new SizeRect(totalWidth, maxRecHeight),
            new SizeRect(totalWidth, maxRecHeight),
            verticalBreakable: false));
    }

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        if (Slots.Count == 0)
        {
            return new RenderResult(null, context.Cursor);
        }

        double[] widths = ComputeWidths(new SizeRect(available.Width, available.Height));

        // Natural row height = tallest cell at its allocated width with no height
        // constraint, then capped at the height actually offered. The cap is what
        // makes Cols paginatable: when the engine force-renders us into a box
        // that's smaller than the natural content, each slot is offered just
        // `rowHeight` and any cell taller than that returns overflow, which we
        // collect into a continuation Cols below.
        double rowNatural = 0;
        for (int i = 0; i < Slots.Count; i++)
        {
            rowNatural = System.Math.Max(rowNatural,
                Slots[i].SpaceHint(new SizeRect(widths[i], null)).Recommended.Height ?? 0);
        }
        double rowHeight = System.Math.Min(rowNatural, available.Height);

        Point start = context.Cursor;
        double x = start.X;
        var overflows = new SlotElement?[Slots.Count];
        bool anyOverflow = false;
        for (int i = 0; i < Slots.Count; i++)
        {
            var slot = Slots[i];
            double slotNatural = slot.SpaceHint(new SizeRect(widths[i], rowHeight)).Recommended.Height ?? 0;
            double slotHeight = System.Math.Min(slotNatural, rowHeight);
            double vOffset = slot.VAlign switch
            {
                VerticalAlignment.Middle => (rowHeight - slotHeight) / 2,
                VerticalAlignment.Bottom => rowHeight - slotHeight,
                _ => 0,
            };
            double drawHeight = slot.VAlign == VerticalAlignment.Top ? rowHeight : slotHeight;
            context.Cursor = new Point(x, start.Y - vOffset);
            var result = slot.Render(context, new Size(widths[i], drawHeight));
            if (result.Overflow is SlotElement partial)
            {
                overflows[i] = partial;
                anyOverflow = true;
            }
            x += widths[i];
        }

        var next = new Point(start.X, start.Y - rowHeight);
        if (!anyOverflow)
        {
            return new RenderResult(null, next);
        }

        // Build a continuation Cols. Finished slots become bare placeholders that
        // preserve column geometry (Sizing + Length, no background or content) so
        // ComputeWidths reproduces the same layout on the next page; the slots
        // that overflowed carry their continuation content forward.
        var continuation = new ColsElement();
        for (int i = 0; i < Slots.Count; i++)
        {
            if (overflows[i] is { } o)
            {
                continuation.Slots.Add(o);
            }
            else
            {
                continuation.Slots.Add(new SlotElement
                {
                    Sizing = Slots[i].Sizing,
                    Length = Slots[i].Length,
                    Unit = Slots[i].Unit,
                });
            }
        }
        CopyStyleTo(continuation);
        return new RenderResult(continuation, next);
    }

    private double[] ComputeWidths(SizeRect available)
    {
        double fixedTotal = 0;
        double autoTotal = 0;
        double relativeWeight = 0;
        var autoWidth = new double[Slots.Count];
        for (int i = 0; i < Slots.Count; i++)
        {
            var slot = Slots[i];
            switch (slot.Sizing)
            {
                case Sizing.Fixed:
                    fixedTotal += slot.Length;
                    break;
                case Sizing.Auto:
                    double w = slot.SpaceHint(new SizeRect(double.MaxValue, available.Height)).Recommended.Width;
                    autoWidth[i] = w;
                    autoTotal += w;
                    break;
                case Sizing.Relative:
                    relativeWeight += slot.Length;
                    break;
            }
        }
        double relativeSpace = System.Math.Max(0, available.Width - fixedTotal - autoTotal);
        var widths = new double[Slots.Count];
        for (int i = 0; i < Slots.Count; i++)
        {
            widths[i] = Slots[i].Sizing switch
            {
                Sizing.Fixed => Slots[i].Length,
                Sizing.Auto => autoWidth[i],
                Sizing.Relative => relativeWeight > 0 ? relativeSpace * Slots[i].Length / relativeWeight : 0,
                _ => 0,
            };
        }
        return widths;
    }
}
