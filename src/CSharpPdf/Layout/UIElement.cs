namespace CSharpPdf.Layout;

/// <summary>
/// The base of every layout construct. It owns the styling common to all UI
/// elements (background, border, padding, horizontal/vertical alignment, the
/// extend-horizontal flag) as plain public properties, and applies that styling
/// centrally in <see cref="Render"/>. Subclasses implement <see cref="MeasureCore"/>
/// and <see cref="RenderCore"/> and add their own content properties. This is the
/// programmatic layer: no fluent chaining, no static factories — those are meant
/// to live in a wrapper above this layer.
/// </summary>
public abstract class UIElement
{
    public Color? Background { get; set; }
    public Color? BorderColor { get; set; }
    public double BorderThickness { get; set; }

    /// <summary>Dash pattern for the border in points (null or empty = solid).</summary>
    public double[]? BorderDash { get; set; }

    /// <summary>Corner radius in points for background, border and clip (0 = sharp).</summary>
    public double BorderRadius { get; set; }

    public double Padding { get; set; }
    public HorizontalAlignment HAlign { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment VAlign { get; set; } = VerticalAlignment.Top;

    /// <summary>Take the full available width (so background and border span it).</summary>
    public bool ExtendHorizontal { get; set; }

    /// <summary>The smallest space the element can render in (its floor).</summary>
    public abstract Size MinimalSpaceRequired { get; }

    /// <summary>The natural size given unlimited room (the auto-grow target).</summary>
    public abstract Size PreferredSize { get; }

    /// <summary>The minimum height needed to render something here (for break decisions).</summary>
    internal virtual double MinRenderHeight(Size available) => MinimalSpaceRequired.Height;

    /// <summary>
    /// Sub-point tolerance for the "does it fit" comparisons (defer / break / wrap).
    /// At PDF point precision (sub-pixel) this is invisible, but it absorbs the
    /// IEEE-754 noise that accumulates when a measured size is added to and then
    /// subtracted from a container's padding/border.
    /// </summary>
    internal const double FitTolerance = 1e-6;

    /// <summary>The concrete size this element occupies for the given available space (incl. padding/border).</summary>
    public virtual Size Measure(Size available)
    {
        double inset = Padding + BorderThickness;
        var inner = MeasureCore(new Size(Max0(available.Width - 2 * inset), Max0(available.Height - 2 * inset)));
        double width = ExtendHorizontal ? available.Width : inner.Width + 2 * inset;
        return new Size(width, inner.Height + 2 * inset);
    }

    /// <summary>
    /// Draw at the context cursor: apply alignment, fill the background, stroke the
    /// border, inset by padding/border, render the content, and return the overflow
    /// (re-styled so a continuation keeps its look) plus the next position. If even
    /// the minimum cannot fit, defers untouched to the next page.
    /// </summary>
    public virtual RenderResult Render(PdfContext context, Size available)
    {
        double inset = Padding + BorderThickness;
        var innerAvailable = new Size(Max0(available.Width - 2 * inset), Max0(available.Height - 2 * inset));
        double minH = MinRenderHeight(innerAvailable);
        if (innerAvailable.Height + FitTolerance < minH)
        {
            CSharpPdf.LayoutTrace.Mark($"DEFER {GetType().Name} innerAvail.H={innerAvailable.Height:F2} minH={minH:F2}");
            return new RenderResult(this, context.Cursor);
        }

        var measured = Measure(available);
        double contentWidth = ExtendHorizontal ? available.Width : System.Math.Min(measured.Width, available.Width);
        Point box = context.Cursor;
        double offsetX = HAlign switch
        {
            HorizontalAlignment.Center => (available.Width - contentWidth) / 2,
            HorizontalAlignment.Right => available.Width - contentWidth,
            _ => 0,
        };
        double drawX = box.X + Max0(offsetX);
        double boxHeight = System.Math.Min(measured.Height, available.Height);

        if (Background is { } bg)
        {
            if (BorderRadius > 0)
                context.FillRoundedRectangle(drawX, box.Y, contentWidth, boxHeight, bg, BorderRadius);
            else
                context.FillRectangle(drawX, box.Y, contentWidth, boxHeight, bg);
        }
        if (BorderColor is { } border && BorderThickness > 0)
        {
            if (BorderRadius > 0 || BorderDash is { Length: > 0 })
                context.StrokeRoundedRectangle(drawX, box.Y, contentWidth, boxHeight, border, BorderThickness, BorderRadius, BorderDash);
            else
                context.StrokeRectangle(drawX, box.Y, contentWidth, boxHeight, border, BorderThickness);
        }

        context.Cursor = new Point(drawX + inset, box.Y - inset);
        var result = RenderCore(context, new Size(Max0(contentWidth - 2 * inset), innerAvailable.Height));

        var next = new Point(box.X, result.Next.Y - inset);
        context.Cursor = next;
        if (result.Overflow is { } overflow)
        {
            CopyStyleTo(overflow);
        }
        return new RenderResult(result.Overflow, next);
    }

    protected abstract Size MeasureCore(Size available);
    protected abstract RenderResult RenderCore(PdfContext context, Size available);

    /// <summary>Copy the base styling onto <paramref name="other"/> (used when a paginated continuation needs to look the same).</summary>
    internal void CopyStyleTo(UIElement other)
    {
        other.Background = Background;
        other.BorderColor = BorderColor;
        other.BorderThickness = BorderThickness;
        other.BorderDash = BorderDash;
        other.BorderRadius = BorderRadius;
        other.Padding = Padding;
        other.HAlign = HAlign;
        other.VAlign = VAlign;
        other.ExtendHorizontal = ExtendHorizontal;
    }

    private protected static double Max0(double v) => v < 0 ? 0 : v;
}
