namespace CSharpPdf.Layout;

/// <summary>
/// A layout component. It advertises intrinsic sizes (a floor and a natural
/// "grow-to" size — hints the engine uses to allocate space), reports its concrete
/// size for a given width via <see cref="Measure"/>, and draws via
/// <see cref="Render"/>. Background fill and padding are handled centrally here, so
/// every component supports them; subclasses implement <see cref="MeasureCore"/>
/// and <see cref="RenderCore"/>.
/// </summary>
public abstract class Component
{
    internal Color? BackgroundColor;
    internal double PaddingValue;

    /// <summary>The smallest space the component can render in (its floor).</summary>
    public abstract Size MinimalSpaceRequired { get; }

    /// <summary>The natural size given unlimited room (the auto-grow target).</summary>
    public abstract Size PreferredSize { get; }

    /// <summary>The concrete size this component occupies for the given available space.</summary>
    public Size Measure(Size available)
    {
        double p = PaddingValue;
        var inner = MeasureCore(new Size(Max0(available.Width - 2 * p), Max0(available.Height - 2 * p)));
        return new Size(inner.Width + 2 * p, inner.Height + 2 * p);
    }

    /// <summary>Draw the component (with its background and padding) into the available space.</summary>
    public RenderResult Render(RenderContext context, Size available)
    {
        double p = PaddingValue;

        if (BackgroundColor is { } bg)
        {
            double height = System.Math.Min(Measure(available).Height, available.Height);
            context.Page.Content.Save().SetRgbFill(bg.R, bg.G, bg.B)
                .Rectangle(context.Left, context.Top - height, available.Width, height).Fill().Restore();
        }

        var innerContext = context.Inset(p, p);
        var innerAvailable = new Size(Max0(available.Width - 2 * p), Max0(available.Height - 2 * p));
        var result = RenderCore(innerContext, innerAvailable);

        if (result.Status == RenderStatus.Empty)
        {
            return RenderResult.Empty;
        }
        var used = new Size(result.Used.Width + 2 * p, result.Used.Height + 2 * p);
        return result.Status == RenderStatus.Partial
            ? RenderResult.Partial(used, result.Remainder!)
            : RenderResult.Full(used);
    }

    protected abstract Size MeasureCore(Size available);
    protected abstract RenderResult RenderCore(RenderContext context, Size available);

    private static double Max0(double v) => v < 0 ? 0 : v;
}

/// <summary>
/// Adds fluent, chainable configuration (background, padding) that returns the
/// concrete component type so calls like <c>UI.Row().Background(..).Children(..)</c>
/// keep working. The non-generic <see cref="Component"/> is what containers store,
/// so heterogeneous children remain possible.
/// </summary>
public abstract class Component<TSelf> : Component
    where TSelf : Component<TSelf>
{
    public TSelf Background(Color color)
    {
        BackgroundColor = color;
        return (TSelf)this;
    }

    public TSelf Padding(double padding)
    {
        PaddingValue = padding;
        return (TSelf)this;
    }
}
