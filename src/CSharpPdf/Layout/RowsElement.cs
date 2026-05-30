namespace CSharpPdf.Layout;

/// <summary>
/// Stacks slots vertically. Each slot's height is its sizing intent: Fixed gives the
/// slot exactly that height, Auto sizes it to its content, Relative slots share the
/// height left over after Fixed and Auto by weight. Pagination: when a slot does not
/// fit, the partial slot's overflow and any following slots become a continuation
/// Rows on the next page.
/// </summary>
public sealed class RowsElement : UIElement<RowsElement>
{
    private readonly List<SlotElement> _slots;

    public RowsElement() { _slots = new List<SlotElement>(); }
    internal RowsElement(List<SlotElement> slots) { _slots = slots; }

    /// <summary>Add children as Auto-sized rows (back-compat with the simple Rows API).</summary>
    public RowsElement Children(params UIElement[] children)
    {
        foreach (var child in children)
        {
            _slots.Add(new SlotElement { Sizing = SlotSizing.Auto, InnerContent = child });
        }
        return this;
    }

    public override Size MinimalSpaceRequired
    {
        get
        {
            double width = 0, height = 0;
            foreach (var slot in _slots)
            {
                var min = slot.MinimalSpaceRequired;
                width = System.Math.Max(width, min.Width);
                height += slot.Sizing == SlotSizing.Fixed ? slot.SizeValue : min.Height;
            }
            return new Size(width, height);
        }
    }

    public override Size PreferredSize
    {
        get
        {
            double width = 0, height = 0;
            foreach (var slot in _slots)
            {
                var pref = slot.PreferredSize;
                width = System.Math.Max(width, pref.Width);
                height += slot.Sizing == SlotSizing.Fixed ? slot.SizeValue : pref.Height;
            }
            return new Size(width, height);
        }
    }

    internal override double MinRenderHeight(Size available) =>
        _slots.Count > 0 ? _slots[0].MinRenderHeight(available) : 0;

    protected override Size MeasureCore(Size available)
    {
        double[] heights = ComputeHeights(available);
        double total = 0;
        foreach (double h in heights)
        {
            total += h;
        }
        return new Size(available.Width, total);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        if (_slots.Count == 0)
        {
            return new RenderResult(null, context.Cursor);
        }

        double[] heights = ComputeHeights(available);
        Point start = context.Cursor;
        double y = start.Y;
        double bottom = start.Y - available.Height;

        for (int i = 0; i < _slots.Count; i++)
        {
            double remaining = y - bottom;
            if (remaining <= 0.01)
            {
                return Overflow(i, _slots[i], i + 1, start.X, y);
            }
            double give = System.Math.Min(heights[i], remaining);
            context.Cursor = new Point(start.X, y);
            var result = _slots[i].Render(context, new Size(available.Width, give));
            y = result.Next.Y;
            if (result.Overflow is SlotElement partial)
            {
                return Overflow(-1, partial, i + 1, start.X, y);
            }
        }
        return new RenderResult(null, new Point(start.X, y));
    }

    private RenderResult Overflow(int firstIndex, SlotElement first, int restStart, double x, double y)
    {
        var rest = new List<SlotElement> { first };
        for (int j = restStart; j < _slots.Count; j++)
        {
            rest.Add(_slots[j]);
        }
        return new RenderResult(new RowsElement(rest), new Point(x, y));
    }

    private double[] ComputeHeights(Size available)
    {
        double fixedTotal = 0;
        double autoTotal = 0;
        double relativeWeight = 0;
        var autoHeight = new double[_slots.Count];
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            switch (slot.Sizing)
            {
                case SlotSizing.Fixed:
                    fixedTotal += slot.SizeValue;
                    break;
                case SlotSizing.Auto:
                    double h = slot.Measure(new Size(available.Width, double.MaxValue)).Height;
                    autoHeight[i] = h;
                    autoTotal += h;
                    break;
                case SlotSizing.Relative:
                    relativeWeight += slot.SizeValue;
                    break;
            }
        }
        double relativeSpace = System.Math.Max(0, available.Height - fixedTotal - autoTotal);
        var heights = new double[_slots.Count];
        for (int i = 0; i < _slots.Count; i++)
        {
            heights[i] = _slots[i].Sizing switch
            {
                SlotSizing.Fixed => _slots[i].SizeValue,
                SlotSizing.Auto => autoHeight[i],
                SlotSizing.Relative => relativeWeight > 0 ? relativeSpace * _slots[i].SizeValue / relativeWeight : 0,
                _ => 0,
            };
        }
        return heights;
    }
}
