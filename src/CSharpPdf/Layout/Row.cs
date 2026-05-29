namespace CSharpPdf.Layout;

/// <summary>
/// Lays children out horizontally, left to right. Each child is auto-sized to its
/// preferred (natural) width — so the row "grows with content" — and the row's
/// height is the tallest child. The row is placed as a unit: if it doesn't fit the
/// remaining height it moves to the next page (no mid-row splitting yet).
/// </summary>
public sealed class Row : Component<Row>
{
    private readonly List<Component> _children = new();
    private double _spacing;

    public Row Children(params Component[] children)
    {
        _children.AddRange(children);
        return this;
    }

    public Row Spacing(double spacing)
    {
        _spacing = spacing;
        return this;
    }

    public override Size MinimalSpaceRequired
    {
        get
        {
            double width = 0, height = 0;
            for (int i = 0; i < _children.Count; i++)
            {
                var min = _children[i].MinimalSpaceRequired;
                width += min.Width + (i > 0 ? _spacing : 0);
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
            for (int i = 0; i < _children.Count; i++)
            {
                var pref = _children[i].PreferredSize;
                width += pref.Width + (i > 0 ? _spacing : 0);
                height = System.Math.Max(height, pref.Height);
            }
            return new Size(width, height);
        }
    }

    protected override Size MeasureCore(Size available)
    {
        double width = 0, height = 0;
        foreach (var child in _children)
        {
            double childWidth = child.PreferredSize.Width;
            height = System.Math.Max(height, child.Measure(new Size(childWidth, available.Height)).Height);
            width += childWidth + _spacing;
        }
        return new Size(width, height);
    }

    protected override RenderResult RenderCore(RenderContext context, Size available)
    {
        double rowHeight = MeasureCore(available).Height;
        if (available.Height < rowHeight)
        {
            return RenderResult.Empty; // move the whole row to the next page
        }

        double x = context.Left;
        foreach (var child in _children)
        {
            double childWidth = child.PreferredSize.Width;
            child.Render(context.At(x, context.Top), new Size(childWidth, available.Height));
            x += childWidth + _spacing;
        }
        return RenderResult.Full(new Size(x - context.Left - _spacing, rowHeight));
    }
}
