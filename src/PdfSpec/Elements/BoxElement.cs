using PdfSpec.Content;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Abstract styled-box base. Centralises everything a renderable region
/// can carry around its content: optional explicit <see cref="Width"/> and
/// <see cref="Height"/>, four-sided padding, per-side border widths and
/// colours, an optional background fill, and horizontal / vertical
/// alignment of the content inside the inner area.
///
/// <para>
/// <see cref="Render"/> is sealed in shape: it sizes the outer box from
/// <see cref="Width"/> / <see cref="Height"/> (or, when null, from the
/// available area combined with the chosen alignment), renders the
/// subclass's content into a deferred sub-stream by calling
/// <see cref="Draw"/>, then sizes the chrome to the resulting box and
/// paints background + borders before flushing. Subclasses provide
/// <see cref="Draw"/> — the per-subclass content rendering — and may
/// override <see cref="DrawNaturalWidth"/> to advertise a narrower
/// natural drawing width so <see cref="HorizontalAlignment"/> can
/// distribute horizontal slack.
/// </para>
/// </summary>
public abstract class BoxElement : Element
{
    /// <summary>
    /// Outer width of the box, as a <see cref="Length"/> (a value tagged
    /// with a <see cref="Unit"/>). <c>null</c> means "use the full
    /// <c>available.Width</c>"; explicit widths resolve to points
    /// (percent → <c>available.Width × Value / 100</c>) and clamp down
    /// to the available area. <c>Width = 100</c> implicitly means 100 pt;
    /// <c>Width = Length.Mm(50)</c> for typed input.
    /// </summary>
    public Length? Width { get; set; }

    /// <summary>
    /// Outer height of the box, as a <see cref="Length"/>. <c>null</c>
    /// shrinks to content + chrome (per the alignment rules below); an
    /// explicit value makes the box exactly that tall (resolved against
    /// <c>available.Height</c> for percent units, clamped to it).
    /// </summary>
    public Length? Height { get; set; }

    /// <summary>Set <see cref="Width"/> from a value + <see cref="Unit"/>. Returns <c>this</c> for chaining.</summary>
    public BoxElement SetWidth(double value, Unit unit = Unit.Pt)
    {
        Width = new Length(value, unit);
        return this;
    }

    /// <summary>Set <see cref="Height"/> from a value + <see cref="Unit"/>. Returns <c>this</c> for chaining.</summary>
    public BoxElement SetHeight(double value, Unit unit = Unit.Pt)
    {
        Height = new Length(value, unit);
        return this;
    }

    /// <summary>Resolve <see cref="Width"/> to points against <paramref name="availableWidth"/> for percent units; <c>null</c> when unset.</summary>
    public double? ResolveWidth(double availableWidth) =>
        Width is { } w ? w.ToPoints(availableWidth) : null;

    /// <summary>Resolve <see cref="Height"/> to points against <paramref name="availableHeight"/>; <c>null</c> when unset.</summary>
    public double? ResolveHeight(double availableHeight) =>
        Height is { } h ? h.ToPoints(availableHeight) : null;

    public double PaddingTop { get; set; }
    public double PaddingRight { get; set; }
    public double PaddingBottom { get; set; }
    public double PaddingLeft { get; set; }

    public PdfColor? Background { get; set; }

    /// <summary>
    /// Where content sits inside the inner area when its
    /// <see cref="DrawNaturalWidth"/> is narrower than the inner width.
    /// Slack distributes as 0 / slack/2 / slack for Left / Center / Right.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>
    /// Where content sits inside the inner area vertically when
    /// <see cref="Height"/> is explicit. Without an explicit Height the
    /// box shrinks to content + chrome and there's no slack to align in
    /// — column / row alignment for that case lives on the parent
    /// container (e.g. <see cref="HStack.DefaultVerticalAlignment"/>),
    /// which positions the entire box within its band.
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    public double BorderTopWidth { get; set; }
    public double BorderRightWidth { get; set; }
    public double BorderBottomWidth { get; set; }
    public double BorderLeftWidth { get; set; }
    public PdfColor? BorderTopColor { get; set; }
    public PdfColor? BorderRightColor { get; set; }
    public PdfColor? BorderBottomColor { get; set; }
    public PdfColor? BorderLeftColor { get; set; }

    public double HorizontalChrome => PaddingLeft + PaddingRight + BorderLeftWidth + BorderRightWidth;
    public double VerticalChrome => PaddingTop + PaddingBottom + BorderTopWidth + BorderBottomWidth;

    /// <summary>Set uniform padding on all four sides.</summary>
    public BoxElement SetPadding(double all)
    {
        PaddingTop = PaddingRight = PaddingBottom = PaddingLeft = all;
        return this;
    }

    /// <summary>Set a uniform border (same width and colour on every side).</summary>
    public BoxElement SetBorder(double width, PdfColor color)
    {
        BorderTopWidth = BorderRightWidth = BorderBottomWidth = BorderLeftWidth = width;
        BorderTopColor = BorderRightColor = BorderBottomColor = BorderLeftColor = color;
        return this;
    }

    /// <summary>
    /// Optional natural drawing width inside the inner area. When the
    /// returned value is smaller than the inner width,
    /// <see cref="HorizontalAlignment"/> distributes the horizontal slack.
    /// Default <c>null</c> means "use the full inner width" — alignment
    /// has no effect. Override on subclasses whose content has a smaller
    /// preferred width (e.g. wrapping a child whose
    /// <see cref="PdfSizeHint.MaxWidth"/> is narrower).
    /// </summary>
    protected virtual double? DrawNaturalWidth(PdfSize innerAvailable) => null;

    /// <summary>
    /// Render this box's content into <paramref name="cs"/> at (0, 0) of
    /// the inner area. The sub-stream's bounding box is already the inner
    /// rectangle (chrome subtracted, horizontal alignment offset applied);
    /// return the actual rendered height via
    /// <see cref="RenderResult.Done(double)"/>.
    /// </summary>
    protected abstract RenderResult Draw(ContentStream cs, PdfSize available);

    /// <summary>
    /// Report the on-page rectangle as the *outer box* (chrome included),
    /// not the slot it was placed into. Lets <see cref="Element.OnRendered"/>
    /// give callers the actual painted rectangle even when the box is
    /// narrower than its allocated width (explicit <see cref="Width"/>)
    /// or shorter than its slot (no explicit <see cref="Height"/> +
    /// content-shrunk). Width is recomputed deterministically from
    /// <paramref name="available"/>; height comes back via
    /// <see cref="RenderResult.NextY"/>, which <see cref="RenderCore"/>
    /// sets to the outer height.
    /// </summary>
    protected override (double Width, double Height) GetRenderedExtent(PdfSize available, RenderResult result) =>
        (Math.Min(ResolveWidth(available.Width) ?? available.Width, available.Width), result.NextY);

    protected override RenderResult RenderCore(ContentStream cs, PdfSize available)
    {
        // Outer width: explicit Width (resolved + clamped to available),
        // else the full available width.
        double outerW = Math.Min(ResolveWidth(available.Width) ?? available.Width, available.Width);

        double innerX = PaddingLeft + BorderLeftWidth;
        double innerY = PaddingTop + BorderTopWidth;
        double innerW = Math.Max(0, outerW - HorizontalChrome);

        // Inner height: explicit Height (resolved + clamped) - chrome, else
        // available.Height - chrome. The actual outer height the box
        // settles on depends on alignment + content height, computed
        // after Draw.
        double maxOuterH = Math.Min(ResolveHeight(available.Height) ?? available.Height, available.Height);
        double innerH = Math.Max(0, maxOuterH - VerticalChrome);

        // Horizontal slack: only applies when the subclass advertises a
        // narrower natural width than innerW.
        double? natural = DrawNaturalWidth(new PdfSize(innerW, innerH));
        double drawW = natural is double nw ? Math.Min(innerW, nw) : innerW;
        double hSlack = Math.Max(0, innerW - drawW);
        double xOffset = HorizontalAlignment switch
        {
            HorizontalAlignment.Center => hSlack / 2,
            HorizontalAlignment.Right => hSlack,
            _ => 0,
        };

        // Render content into a deferred sub. We hold its buffer so we can
        // size the chrome to the actual rendered height and apply vertical
        // alignment by re-positioning the sub before flushing.
        var sub = cs.CreateSubStream(innerX + xOffset, innerY, drawW, innerH);
        var result = Draw(sub, new PdfSize(drawW, innerH));

        // Outer height + vertical slack:
        //  - Height set → box is exactly that tall (clamped). Slack lives
        //    between rendered content and inner area; VerticalAlignment
        //    distributes it.
        //  - Height null → shrink to content + chrome regardless of
        //    VerticalAlignment. Filling on alignment alone would inflate
        //    the box to the parent's entire available height, which is
        //    what flex containers (Rows / Cols) want to avoid. Per-band
        //    positioning of the whole box belongs to the parent.
        bool fillHeight = Height is not null;
        double outerH = fillHeight ? maxOuterH : result.NextY + VerticalChrome;
        double vSlack = fillHeight ? Math.Max(0, innerH - result.NextY) : 0;
        double yOffset = VerticalAlignment switch
        {
            VerticalAlignment.Middle => vSlack / 2,
            VerticalAlignment.Bottom => vSlack,
            _ => 0,
        };

        if (yOffset != 0) sub.SetParentPosition(innerX + xOffset, innerY + yOffset);

        PaintBackgroundAndBorders(cs, outerW, outerH);
        sub.Build();

        // Propagate any continuation Draw produced — flex containers
        // (VStack, MultiColumn) hand back a Partial when their items
        // don't all fit, and the page-level Body loop relies on seeing
        // that NextElement to know it should add a new page and keep
        // going. Dropping it here would silently truncate the document.
        return new RenderResult(outerH, result.NextElement);
    }

    private void PaintBackgroundAndBorders(ContentStream cs, double width, double height)
    {
        if (Background is { } bg)
            cs.DrawRectangle(0, 0, width, height, fill: bg);

        if (BorderTopColor is { } tc && BorderTopWidth > 0)
            cs.DrawRectangle(0, 0, width, BorderTopWidth, fill: tc);
        if (BorderRightColor is { } rc && BorderRightWidth > 0)
            cs.DrawRectangle(width - BorderRightWidth, 0, BorderRightWidth, height, fill: rc);
        if (BorderBottomColor is { } bc && BorderBottomWidth > 0)
            cs.DrawRectangle(0, height - BorderBottomWidth, width, BorderBottomWidth, fill: bc);
        if (BorderLeftColor is { } lc && BorderLeftWidth > 0)
            cs.DrawRectangle(0, 0, BorderLeftWidth, height, fill: lc);
    }
}
