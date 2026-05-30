namespace CSharpPdf.Layout;

/// <summary>
/// Lays slots side by side. Each slot's width is its sizing intent: Fixed gives the
/// slot exactly that width, Auto sizes it to its content's natural width, Relative
/// slots share the width left over after Fixed and Auto by weight. Row height is the
/// tallest slot; each slot is positioned vertically within that height per its
/// vertical alignment. The row is placed as a unit (moves to the next page whole).
/// </summary>
public sealed class ColsElement : UIElement<ColsElement>
{
    private readonly List<SlotElement> _slots;

    public ColsElement() { _slots = new List<SlotElement>(); }
    internal ColsElement(List<SlotElement> slots) { _slots = slots; }

    /// <summary>Add children as Auto-sized columns (back-compat with the simple Cols API).</summary>
    public ColsElement Children(params UIElement[] children)
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
                width += slot.Sizing == SlotSizing.Fixed ? slot.SizeValue : min.Width;
                height = System.Math.Max(height, min.Height);
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
                width += slot.Sizing == SlotSizing.Fixed ? slot.SizeValue : pref.Width;
                height = System.Math.Max(height, pref.Height);
            }
            return new Size(width, height);
        }
    }

    internal override double MinRenderHeight(Size available) => MeasureCore(available).Height;

    protected override Size MeasureCore(Size available)
    {
        double[] widths = ComputeWidths(available);
        double width = 0, height = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            height = System.Math.Max(height, _slots[i].Measure(new Size(widths[i], available.Height)).Height);
            width += widths[i];
        }
        return new Size(width, height);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        if (_slots.Count == 0)
        {
            return new RenderResult(null, context.Cursor);
        }

        double[] widths = ComputeWidths(available);
        double rowHeight = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            rowHeight = System.Math.Max(rowHeight, _slots[i].Measure(new Size(widths[i], available.Height)).Height);
        }

        Point start = context.Cursor;
        double x = start.X;
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            double slotHeight = slot.Measure(new Size(widths[i], rowHeight)).Height;
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

    private double[] ComputeWidths(Size available)
    {
        double fixedTotal = 0;
        double autoTotal = 0;
        double relativeWeight = 0;
        var autoWidth = new double[_slots.Count];
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            switch (slot.Sizing)
            {
                case SlotSizing.Fixed:
                    fixedTotal += slot.SizeValue;
                    break;
                case SlotSizing.Auto:
                    double w = slot.Measure(new Size(double.MaxValue, available.Height)).Width;
                    autoWidth[i] = w;
                    autoTotal += w;
                    break;
                case SlotSizing.Relative:
                    relativeWeight += slot.SizeValue;
                    break;
            }
        }
        double relativeSpace = System.Math.Max(0, available.Width - fixedTotal - autoTotal);
        var widths = new double[_slots.Count];
        for (int i = 0; i < _slots.Count; i++)
        {
            widths[i] = _slots[i].Sizing switch
            {
                SlotSizing.Fixed => _slots[i].SizeValue,
                SlotSizing.Auto => autoWidth[i],
                SlotSizing.Relative => relativeWeight > 0 ? relativeSpace * _slots[i].SizeValue / relativeWeight : 0,
                _ => 0,
            };
        }
        return widths;
    }
}
