namespace CSharpPdf.Layout;

/// <summary>
/// Stacks children vertically. Renders them top-to-bottom, advancing the cursor,
/// and paginates: when a child does not fit (or only partly fits), the column
/// returns a continuation column holding the remaining children (and the partial
/// child's remainder) for the next page.
/// </summary>
public sealed class Column : Component<Column>
{
    private readonly List<Component> _children = new();
    private double _spacing;

    public Column Children(params Component[] children)
    {
        _children.AddRange(children);
        return this;
    }

    public Column Spacing(double spacing)
    {
        _spacing = spacing;
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
            for (int i = 0; i < _children.Count; i++)
            {
                var pref = _children[i].PreferredSize;
                width = System.Math.Max(width, pref.Width);
                height += pref.Height + (i > 0 ? _spacing : 0);
            }
            return new Size(width, height);
        }
    }

    protected override Size MeasureCore(Size available)
    {
        double width = 0, height = 0;
        for (int i = 0; i < _children.Count; i++)
        {
            var size = _children[i].Measure(new Size(available.Width, double.MaxValue));
            width = System.Math.Max(width, size.Width);
            height += size.Height + (i > 0 ? _spacing : 0);
        }
        return new Size(width, height);
    }

    protected override RenderResult RenderCore(RenderContext context, Size available)
    {
        double top = context.Top;
        double usedHeight = 0, maxWidth = 0;

        for (int i = 0; i < _children.Count; i++)
        {
            double remaining = available.Height - usedHeight;
            var childContext = context.At(context.Left, top);
            var result = _children[i].Render(childContext, new Size(available.Width, remaining));

            if (result.Status == RenderStatus.Empty)
            {
                if (i == 0 && usedHeight == 0)
                {
                    return RenderResult.Empty;
                }
                return RenderResult.Partial(new Size(maxWidth, usedHeight), Continuation(i, null));
            }

            usedHeight += result.Used.Height;
            top -= result.Used.Height;
            maxWidth = System.Math.Max(maxWidth, result.Used.Width);

            if (result.Status == RenderStatus.Partial)
            {
                return RenderResult.Partial(new Size(maxWidth, usedHeight), Continuation(i + 1, result.Remainder));
            }

            if (i < _children.Count - 1)
            {
                usedHeight += _spacing;
                top -= _spacing;
            }
        }
        return RenderResult.Full(new Size(maxWidth, usedHeight));
    }

    // Build the column that continues on the next page: an optional leading
    // remainder (the partly-rendered child) followed by the not-yet-started children.
    private Column Continuation(int fromIndex, Component? leadingRemainder)
    {
        var next = new Column { _spacing = _spacing };
        if (leadingRemainder is not null)
        {
            next._children.Add(leadingRemainder);
        }
        for (int i = fromIndex; i < _children.Count; i++)
        {
            next._children.Add(_children[i]);
        }
        return next;
    }
}
