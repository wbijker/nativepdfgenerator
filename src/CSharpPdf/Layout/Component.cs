namespace CSharpPdf.Layout;

/// <summary>
/// A layout component. It advertises intrinsic sizes (a floor and a natural
/// "grow-to" size — hints the engine uses to allocate space), reports its concrete
/// size for a given width via <see cref="Measure"/>, and draws via
/// <see cref="Render"/>. Background fill, padding, and horizontal alignment are
/// handled centrally here; subclasses implement <see cref="MeasureCore"/> and
/// <see cref="RenderCore"/>. (Vertical alignment is honored by containers such as
/// <c>Row</c>, which control a bounded cell height.)
/// </summary>
public abstract class Component
{
    internal Color? BackgroundColor;
    internal double PaddingValue;
    internal HorizontalAlignment HAlign = HorizontalAlignment.Left;
    internal VerticalAlignment VAlign = VerticalAlignment.Top;
    internal bool ExtendWidth;

    /// <summary>The smallest space the component can render in (its floor).</summary>
    public abstract Size MinimalSpaceRequired { get; }

    /// <summary>The natural size given unlimited room (the auto-grow target).</summary>
    public abstract Size PreferredSize { get; }

    /// <summary>The concrete size this component occupies for the given available space.</summary>
    public Size Measure(Size available)
    {
        double p = PaddingValue;
        var inner = MeasureCore(new Size(Max0(available.Width - 2 * p), Max0(available.Height - 2 * p)));
        double width = ExtendWidth ? available.Width : inner.Width + 2 * p;
        return new Size(width, inner.Height + 2 * p);
    }

    /// <summary>Draw the component (background, padding, alignment) into the available space.</summary>
    public RenderResult Render(RenderContext context, Size available)
    {
        var measured = Measure(available);
        double contentWidth = ExtendWidth ? available.Width : System.Math.Min(measured.Width, available.Width);
        double offsetX = HAlign switch
        {
            HorizontalAlignment.Center => (available.Width - contentWidth) / 2,
            HorizontalAlignment.Right => available.Width - contentWidth,
            _ => 0,
        };
        if (offsetX < 0)
        {
            offsetX = 0;
        }

        if (BackgroundColor is { } bg)
        {
            double bgHeight = System.Math.Min(measured.Height, available.Height);
            context.Page.Content.Save().SetRgbFill(bg.R, bg.G, bg.B)
                .Rectangle(context.Left + offsetX, context.Top - bgHeight, contentWidth, bgHeight).Fill().Restore();
        }

        double p = PaddingValue;
        var innerContext = context.At(context.Left + offsetX + p, context.Top - p);
        var innerAvailable = new Size(Max0(contentWidth - 2 * p), Max0(available.Height - 2 * p));
        var result = RenderCore(innerContext, innerAvailable);

        if (result.Status == RenderStatus.Empty)
        {
            return RenderResult.Empty;
        }
        var used = new Size(contentWidth, result.Used.Height + 2 * p);
        return result.Status == RenderStatus.Partial
            ? RenderResult.Partial(used, result.Remainder!)
            : RenderResult.Full(used);
    }

    protected abstract Size MeasureCore(Size available);
    protected abstract RenderResult RenderCore(RenderContext context, Size available);

    private static double Max0(double v) => v < 0 ? 0 : v;
}

/// <summary>
/// Adds fluent, chainable configuration that returns the concrete component type,
/// so <c>UI.Row().Background(..).AlignCenter().Children(..)</c> keeps working. The
/// non-generic <see cref="Component"/> is what containers store, so heterogeneous
/// children remain possible.
/// </summary>
public abstract class Component<TSelf> : Component
    where TSelf : Component<TSelf>
{
    public TSelf Background(Color color) { BackgroundColor = color; return (TSelf)this; }
    public TSelf Padding(double padding) { PaddingValue = padding; return (TSelf)this; }

    public TSelf AlignLeft() { HAlign = HorizontalAlignment.Left; return (TSelf)this; }
    public TSelf AlignCenter() { HAlign = HorizontalAlignment.Center; return (TSelf)this; }
    public TSelf AlignRight() { HAlign = HorizontalAlignment.Right; return (TSelf)this; }

    public TSelf AlignTop() { VAlign = VerticalAlignment.Top; return (TSelf)this; }
    public TSelf AlignMiddle() { VAlign = VerticalAlignment.Middle; return (TSelf)this; }
    public TSelf AlignBottom() { VAlign = VerticalAlignment.Bottom; return (TSelf)this; }

    /// <summary>Make the component take the full available width (so a background fills it).</summary>
    public TSelf ExtendHorizontal() { ExtendWidth = true; return (TSelf)this; }
}
