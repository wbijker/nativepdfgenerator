namespace CSharpPdf.Layout;

/// <summary>
/// Lays children out horizontally, left to right (use child padding for gaps).
/// Available width is shared using each child's min and preferred widths: if the
/// preferred widths all fit, each gets its preferred width; otherwise the width is
/// distributed between min and preferred in proportion to each child's flex
/// (preferred − min); if even the minimums don't fit, each gets its minimum. Row
/// height is the tallest child, and each child is positioned within that height
/// per its vertical alignment. The row moves to the next page as a unit.
/// </summary>
public sealed class Row : Component<Row>
{
    private readonly List<Component> _children = new();

    public Row Children(params Component[] children)
    {
        _children.AddRange(children);
        return this;
    }

    public override Size MinimalSpaceRequired
    {
        get
        {
            double width = 0, height = 0;
            foreach (var child in _children)
            {
                var min = child.MinimalSpaceRequired;
                width += min.Width;
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
            foreach (var child in _children)
            {
                var pref = child.PreferredSize;
                width += pref.Width;
                height = System.Math.Max(height, pref.Height);
            }
            return new Size(width, height);
        }
    }

    protected override Size MeasureCore(Size available)
    {
        double[] widths = Distribute(available.Width);
        double width = 0, height = 0;
        for (int i = 0; i < _children.Count; i++)
        {
            height = System.Math.Max(height, _children[i].Measure(new Size(widths[i], available.Height)).Height);
            width += widths[i];
        }
        return new Size(width, height);
    }

    protected override RenderResult RenderCore(RenderContext context, Size available)
    {
        if (_children.Count == 0)
        {
            return RenderResult.Full(Size.Zero);
        }

        double[] widths = Distribute(available.Width);
        double rowHeight = 0;
        for (int i = 0; i < _children.Count; i++)
        {
            rowHeight = System.Math.Max(rowHeight, _children[i].Measure(new Size(widths[i], available.Height)).Height);
        }
        if (available.Height < rowHeight)
        {
            return RenderResult.Empty; // not enough height: the whole row moves to the next page
        }

        double x = context.Left;
        for (int i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            double childHeight = child.Measure(new Size(widths[i], rowHeight)).Height;
            double vOffset = child.VAlign switch
            {
                VerticalAlignment.Middle => (rowHeight - childHeight) / 2,
                VerticalAlignment.Bottom => rowHeight - childHeight,
                _ => 0,
            };
            child.Render(context.At(x, context.Top - vOffset), new Size(widths[i], rowHeight - vOffset));
            x += widths[i];
        }
        return RenderResult.Full(new Size(x - context.Left, rowHeight));
    }

    // Share available width using min + preferred widths (CSS-table-ish auto sizing).
    private double[] Distribute(double available)
    {
        int n = _children.Count;
        var min = new double[n];
        var pref = new double[n];
        double sumMin = 0, sumPref = 0;
        for (int i = 0; i < n; i++)
        {
            min[i] = _children[i].MinimalSpaceRequired.Width;
            pref[i] = _children[i].PreferredSize.Width;
            sumMin += min[i];
            sumPref += pref[i];
        }

        var widths = new double[n];
        if (sumPref <= available || sumPref <= sumMin)
        {
            System.Array.Copy(pref, widths, n); // everything fits at preferred width
        }
        else if (sumMin >= available)
        {
            System.Array.Copy(min, widths, n); // even minimums overflow
        }
        else
        {
            double scale = (available - sumMin) / (sumPref - sumMin);
            for (int i = 0; i < n; i++)
            {
                widths[i] = min[i] + (pref[i] - min[i]) * scale;
            }
        }
        return widths;
    }
}
