using CSharpPdf.Content;

namespace CSharpPdf.Layout;

/// <summary>
/// The base of every layout construct. Carries the styling common to all UI
/// elements (background, border, padding, alignment, extend-horizontal) as plain
/// public properties, applies that styling centrally in <see cref="Render"/>,
/// and exposes one sizing query — <see cref="SpaceHint"/> — that every
/// subclass implements. Drawing happens through <see cref="RenderCore"/>
/// (the element's own logic, inset by the box's padding/border).
/// </summary>
public abstract class Element
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

    /// <summary>
    /// Sub-point tolerance for the "does it fit" comparisons. Absorbs IEEE-754
    /// noise when measured sizes are added to and then subtracted from
    /// container padding.
    /// </summary>
    internal const double FitTolerance = 1e-6;

    /// <summary>
    /// Fires once per <see cref="Render"/> call after this element's draw box
    /// has been laid out (just before the call returns), carrying a
    /// <see cref="RenderedInfo"/> snapshot — page number, absolute top-left,
    /// and bounding rectangle. An element that paginates fires once per page
    /// it lands on (one event per continuation). Null by default → zero cost
    /// when unused.
    /// </summary>
    public System.Action<RenderedInfo>? OnRendered { get; set; }

    /// <summary>
    /// The single sizing query. Returns the element's minimal and recommended
    /// outer extents (including this element's own padding/border) plus whether
    /// it can be paginated. <paramref name="available"/> may carry a
    /// <c>null</c> height meaning "unbounded".
    /// </summary>
    public abstract SpaceDimension SpaceHint(SizeRect available);

    /// <summary>
    /// Draw at the cursor: apply alignment, fill background, stroke border,
    /// inset by padding/border, render content, return overflow (re-styled
    /// for a continuation) plus the next position. If the minimum doesn't fit
    /// — or the element isn't vertically breakable and even the recommended
    /// height doesn't fit — defers to the next page.
    /// </summary>
    public virtual RenderResult Render(PdfCanvas canvas, Size available)
    {
        double inset = Padding + BorderThickness;
        var innerAvailable = new Size(Max0(available.Width - 2 * inset), Max0(available.Height - 2 * inset));
        var space = SpaceHint(new SizeRect(available.Width, available.Height));

        double minH = space.Minimal.Height ?? 0;
        double effectiveMin = space.VerticalBreakable
            ? minH
            : System.Math.Max(minH, space.Recommended.Height ?? minH);

        // Defer to the next page when we can't fit — UNLESS the engine has flipped
        // ForceRender on, which it does when this element already deferred on a
        // fresh empty page. The breakable hint is just a preference, not a rule:
        // if it cannot fit anywhere, render here and let the element's own
        // pagination produce a continuation (or, for atomic content, accept that
        // it spills off the page).
        if (!canvas.ForceRender && available.Height + FitTolerance < effectiveMin)
        {
            return new RenderResult(this, canvas.Cursor); // defer
        }

        double measuredWidth = space.Recommended.Width;
        double measuredHeight = space.Recommended.Height ?? available.Height;

        double contentWidth = ExtendHorizontal
            ? available.Width
            : System.Math.Min(measuredWidth, available.Width);

        Point box = canvas.Cursor;
        double offsetX = HAlign switch
        {
            HorizontalAlignment.Center => (available.Width - contentWidth) / 2,
            HorizontalAlignment.Right => available.Width - contentWidth,
            _ => 0,
        };
        double drawX = box.X + Max0(offsetX);
        double boxHeight = System.Math.Min(measuredHeight, available.Height);

        if (Background is { } bg)
        {
            if (BorderRadius > 0)
                canvas.FillRoundedRectangle(drawX, box.Y, contentWidth, boxHeight, bg, BorderRadius);
            else
                canvas.FillRectangle(drawX, box.Y, contentWidth, boxHeight, bg);
        }
        if (BorderColor is { } border && BorderThickness > 0)
        {
            if (BorderRadius > 0 || BorderDash is { Length: > 0 })
                canvas.StrokeRoundedRectangle(drawX, box.Y, contentWidth, boxHeight, border, BorderThickness, BorderRadius, BorderDash);
            else
                canvas.StrokeRectangle(drawX, box.Y, contentWidth, boxHeight, border, BorderThickness);
        }

        canvas.Cursor = new Point(drawX + inset, box.Y - inset);
        var result = RenderCore(canvas, new Size(Max0(contentWidth - 2 * inset), innerAvailable.Height));

        var next = new Point(box.X, result.Next.Y - inset);
        canvas.Cursor = next;
        if (result.Overflow is { } overflow)
        {
            CopyStyleTo(overflow);
        }
        // Fire the rendered hook with this element's drawn box in PDF abs coords.
        if (OnRendered is { } hook)
        {
            double absX = canvas.ToAbsoluteX(drawX);
            double absTop = canvas.ToAbsoluteY(box.Y);
            var pos = new Point(absX, absTop);
            hook(new RenderedInfo(pos, canvas.PageNumber, new Boundary(absX, absTop, contentWidth, boxHeight)));
        }
        return new RenderResult(result.Overflow, next);
    }

    protected abstract RenderResult RenderCore(PdfCanvas canvas, Size available);

    /// <summary>Copy the base styling onto <paramref name="other"/> (used when a paginated continuation needs to look the same).</summary>
    internal void CopyStyleTo(Element other)
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

    /// <summary>Add this element's padding+border inset around an inner space dimension.</summary>
    protected SpaceDimension WithOwnInset(SpaceDimension inner)
    {
        double inset = Padding + BorderThickness;
        if (inset == 0) return inner;
        return new SpaceDimension(
            InsetRect(inner.Minimal, inset),
            InsetRect(inner.Recommended, inset),
            inner.VerticalBreakable);
    }

    private static SizeRect InsetRect(SizeRect r, double inset) =>
        new(r.Width + 2 * inset, r.Height.HasValue ? r.Height.Value + 2 * inset : (double?)null);

    private protected static double Max0(double v) => v < 0 ? 0 : v;

    /// <summary>Inner-available helper for subclasses computing SpaceHint.</summary>
    protected SizeRect InnerAvailable(SizeRect available)
    {
        double inset = Padding + BorderThickness;
        if (inset == 0) return available;
        return new SizeRect(
            Max0(available.Width - 2 * inset),
            available.Height.HasValue ? Max0(available.Height.Value - 2 * inset) : (double?)null);
    }
}
