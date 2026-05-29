namespace CSharpPdf.Layout;

/// <summary>
/// Lays children out side by side (children are columns), sharing available width
/// using each child's min and preferred widths (preferred when it fits, otherwise
/// proportional shrink by flex = preferred − min, with min as the floor). Row
/// height is the tallest child; each child is positioned within that height per its
/// vertical alignment. The row is placed as a unit (moves to the next page whole).
/// </summary>
public sealed class ColsElement : UIElement<ColsElement>
{
    private readonly List<UIElement> _children = new();

    public ColsElement Children(params UIElement[] children)
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

    // The whole row must fit; its minimum render height is the full row height.
    internal override double MinRenderHeight(Size available) => MeasureCore(available).Height;

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

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        if (_children.Count == 0)
        {
            return new RenderResult(null, context.Cursor);
        }

        double[] widths = Distribute(available.Width);
        double rowHeight = 0;
        for (int i = 0; i < _children.Count; i++)
        {
            rowHeight = System.Math.Max(rowHeight, _children[i].Measure(new Size(widths[i], available.Height)).Height);
        }

        Point start = context.Cursor;
        double x = start.X;
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
            context.Cursor = new Point(x, start.Y - vOffset);
            child.Render(context, new Size(widths[i], rowHeight - vOffset));
            x += widths[i];
        }
        return new RenderResult(null, new Point(start.X, start.Y - rowHeight));
    }

    private double[] Distribute(double available)
    {
        int n = _children.Count;
        var min = new double[n];
        var pref = new double[n];
        for (int i = 0; i < n; i++)
        {
            min[i] = _children[i].MinimalSpaceRequired.Width;
            pref[i] = _children[i].PreferredSize.Width;
        }
        return Distribution.Across(min, pref, available);
    }
}
