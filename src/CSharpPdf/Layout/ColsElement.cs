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

    public override SpaceDimension SpaceRequired(SizeRect available)
    {
        double[] widths = ComputeWidths(available);
        double totalWidth = 0, maxRecHeight = 0, maxMinHeight = 0;
        for (int i = 0; i < Slots.Count; i++)
        {
            var s = Slots[i].SpaceRequired(new SizeRect(widths[i], available.Height));
            maxRecHeight = System.Math.Max(maxRecHeight, s.Recommended.Height ?? 0);
            maxMinHeight = System.Math.Max(maxMinHeight, s.Minimal.Height ?? 0);
            totalWidth += widths[i];
        }
        // A Cols row is atomic — it cannot split vertically across pages.
        return new SpaceDimension(
            new SizeRect(totalWidth, maxRecHeight),  // min height is the full row height since Cols is atomic
            new SizeRect(totalWidth, maxRecHeight),
            verticalBreakable: false);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        if (Slots.Count == 0)
        {
            return new RenderResult(null, context.Cursor);
        }

        double[] widths = ComputeWidths(new SizeRect(available.Width, available.Height));
        double rowHeight = 0;
        for (int i = 0; i < Slots.Count; i++)
        {
            rowHeight = System.Math.Max(rowHeight,
                Slots[i].SpaceRequired(new SizeRect(widths[i], available.Height)).Recommended.Height ?? 0);
        }

        Point start = context.Cursor;
        double x = start.X;
        for (int i = 0; i < Slots.Count; i++)
        {
            var slot = Slots[i];
            double slotHeight = slot.SpaceRequired(new SizeRect(widths[i], rowHeight)).Recommended.Height ?? 0;
            double vOffset = slot.VAlign switch
            {
                VerticalAlignment.Middle => (rowHeight - slotHeight) / 2,
                VerticalAlignment.Bottom => rowHeight - slotHeight,
                _ => 0,
            };
            double drawHeight = slot.VAlign == VerticalAlignment.Top ? rowHeight : slotHeight;
            context.Cursor = new Point(x, start.Y - vOffset);
            slot.Render(context, new Size(widths[i], drawHeight));
            x += widths[i];
        }
        return new RenderResult(null, new Point(start.X, start.Y - rowHeight));
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
                    double w = slot.SpaceRequired(new SizeRect(double.MaxValue, available.Height)).Recommended.Width;
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
