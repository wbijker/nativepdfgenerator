namespace CSharpPdf.Layout;

/// <summary>
/// Stacks children vertically (children are rows), flush against one another (use
/// padding for gaps). Paginates: when a child does not fully fit, the remaining
/// children (and the partial child's overflow) become a continuation Rows.
/// </summary>
public sealed class RowsElement : UIElement<RowsElement>
{
    private readonly List<UIElement> _children = new();

    public RowsElement Children(params UIElement[] children)
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
                width = System.Math.Max(width, min.Width);
                height += min.Height;
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
                width = System.Math.Max(width, pref.Width);
                height += pref.Height;
            }
            return new Size(width, height);
        }
    }

    internal override double MinRenderHeight(Size available) =>
        _children.Count > 0 ? _children[0].MinRenderHeight(available) : 0;

    protected override Size MeasureCore(Size available)
    {
        double width = 0, height = 0;
        foreach (var child in _children)
        {
            var size = child.Measure(new Size(available.Width, double.MaxValue));
            width = System.Math.Max(width, size.Width);
            height += size.Height;
        }
        return new Size(width, height);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        Point start = context.Cursor;
        double bottom = start.Y - available.Height;
        double currentY = start.Y;

        for (int i = 0; i < _children.Count; i++)
        {
            context.Cursor = new Point(start.X, currentY);
            var result = _children[i].Render(context, new Size(available.Width, currentY - bottom));
            currentY = result.Next.Y;

            if (result.Overflow is { } overflow)
            {
                var rest = new RowsElement();
                rest._children.Add(overflow);
                for (int j = i + 1; j < _children.Count; j++)
                {
                    rest._children.Add(_children[j]);
                }
                return new RenderResult(rest, new Point(start.X, currentY));
            }
        }
        return new RenderResult(null, new Point(start.X, currentY));
    }
}
