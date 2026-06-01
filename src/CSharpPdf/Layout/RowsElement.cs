namespace CSharpPdf.Layout;

/// <summary>
/// Stacks slots vertically. Each slot's height is its sizing intent: Fixed gives the
/// slot exactly that height, Auto sizes it to its content, Relative slots share the
/// height left over after Fixed and Auto by weight. Pagination: when a slot does not
/// fit, the partial slot's overflow and any following slots become a continuation
/// Rows on the next page.
/// </summary>
public sealed class RowsElement : UIElement
{
    /// <summary>The rows in top-to-bottom order. Populate via object initializer or .Add.</summary>
    public List<SlotElement> Slots { get; } = new();

    public RowsElement() { }
    internal RowsElement(IEnumerable<SlotElement> slots) { Slots.AddRange(slots); }

    public override SpaceDimension SpaceRequired(SizeRect available)
    {
        // The orphan threshold = the first slot's minimum (we render top-down).
        double minHeight = Slots.Count > 0
            ? Slots[0].SpaceRequired(available).Minimal.Height ?? 0
            : 0;

        double recHeight = 0;
        foreach (var slot in Slots)
        {
            var s = slot.SpaceRequired(available);
            recHeight += s.Recommended.Height ?? 0;
        }
        return new SpaceDimension(
            new SizeRect(available.Width, minHeight),
            new SizeRect(available.Width, recHeight),
            verticalBreakable: true);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        if (Slots.Count == 0)
        {
            return new RenderResult(null, context.Cursor);
        }

        double[] heights = ComputeHeights(available);
        Point start = context.Cursor;
        double y = start.Y;
        double bottom = start.Y - available.Height;

        for (int i = 0; i < Slots.Count; i++)
        {
            // A PageBreak slot ends the current Rows render here; the remaining
            // slots become a continuation Rows so the engine renders them on the
            // next page. The PageBreak itself is consumed.
            if (Slots[i].Content is PageBreakElement)
            {
                if (i + 1 >= Slots.Count)
                {
                    return new RenderResult(null, new Point(start.X, y));
                }
                var rest = new List<SlotElement>();
                for (int j = i + 1; j < Slots.Count; j++) rest.Add(Slots[j]);
                return new RenderResult(new RowsElement(rest), new Point(start.X, y));
            }

            double remaining = y - bottom;
            CSharpPdf.LayoutTrace.Mark($"Rows[{Slots.Count}] slot[{i}] sizing={Slots[i].Sizing} length={Slots[i].Length:F1} h={heights[i]:F1} remaining={remaining:F1} y={y:F1}");
            if (remaining <= 0.01)
            {
                return Overflow(Slots[i], i + 1, start.X, y);
            }
            double give = System.Math.Min(heights[i], remaining);
            context.Cursor = new Point(start.X, y);
            var result = Slots[i].Render(context, new Size(available.Width, give));
            y = result.Next.Y;
            if (result.Overflow is SlotElement partial)
            {
                return Overflow(partial, i + 1, start.X, y);
            }
        }
        return new RenderResult(null, new Point(start.X, y));
    }

    private RenderResult Overflow(SlotElement first, int restStart, double x, double y)
    {
        var rest = new List<SlotElement> { first };
        for (int j = restStart; j < Slots.Count; j++)
        {
            rest.Add(Slots[j]);
        }
        return new RenderResult(new RowsElement(rest), new Point(x, y));
    }

    private double[] ComputeHeights(Size available)
    {
        double fixedTotal = 0;
        double autoTotal = 0;
        double relativeWeight = 0;
        var autoHeight = new double[Slots.Count];
        for (int i = 0; i < Slots.Count; i++)
        {
            var slot = Slots[i];
            switch (slot.Sizing)
            {
                case Sizing.Fixed:
                    fixedTotal += slot.Length;
                    break;
                case Sizing.Auto:
                    double h = slot.SpaceRequired(new SizeRect(available.Width, null)).Recommended.Height ?? 0;
                    autoHeight[i] = h;
                    autoTotal += h;
                    break;
                case Sizing.Relative:
                    relativeWeight += slot.Length;
                    break;
            }
        }
        double relativeSpace = System.Math.Max(0, available.Height - fixedTotal - autoTotal);
        var heights = new double[Slots.Count];
        for (int i = 0; i < Slots.Count; i++)
        {
            heights[i] = Slots[i].Sizing switch
            {
                Sizing.Fixed => Slots[i].Length,
                Sizing.Auto => autoHeight[i],
                Sizing.Relative => relativeWeight > 0 ? relativeSpace * Slots[i].Length / relativeWeight : 0,
                _ => 0,
            };
        }
        return heights;
    }
}
