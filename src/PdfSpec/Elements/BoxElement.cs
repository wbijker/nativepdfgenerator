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
    /// Outer width of the box, in user units. <c>null</c> means "use the
    /// full <c>available.Width</c>". Explicit widths are clamped down to
    /// the available area; the parent decides what to do with any leftover.
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Outer height of the box. <c>null</c> means "shrink to content + chrome"
    /// when <see cref="VerticalAlignment"/> is <see cref="Alignment.Start"/>;
    /// otherwise the box claims the full <c>available.Height</c> so the
    /// chrome paints to that height and content positions inside.
    /// </summary>
    public double? Height { get; set; }

    public double PaddingTop { get; set; }
    public double PaddingRight { get; set; }
    public double PaddingBottom { get; set; }
    public double PaddingLeft { get; set; }

    public PdfColor? Background { get; set; }

    /// <summary>
    /// Where content sits inside the inner area when its
    /// <see cref="DrawNaturalWidth"/> is narrower than the inner width.
    /// Slack distributes as 0 / slack/2 / slack for Start / Center / End.
    /// </summary>
    public Alignment HorizontalAlignment { get; set; } = Alignment.Start;

    /// <summary>
    /// Where content sits inside the inner area vertically when
    /// <see cref="Height"/> is explicit. Without an explicit Height the
    /// box shrinks to content + chrome and there's no slack to align in
    /// — column / row alignment for that case lives on the parent
    /// container (e.g. <see cref="Rows.DefaultVerticalAlignment"/>),
    /// which positions the entire box within its band.
    /// </summary>
    public Alignment VerticalAlignment { get; set; } = Alignment.Start;

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

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        // Outer width: explicit Width, clamped to available, else available.
        double outerW = Math.Min(Width ?? available.Width, available.Width);

        double innerX = PaddingLeft + BorderLeftWidth;
        double innerY = PaddingTop + BorderTopWidth;
        double innerW = Math.Max(0, outerW - HorizontalChrome);

        // Inner height: explicit Height (clamped) - chrome, else
        // available.Height - chrome. The actual outer height the box
        // settles on depends on alignment + content height, computed
        // after Draw.
        double maxOuterH = Math.Min(Height ?? available.Height, available.Height);
        double innerH = Math.Max(0, maxOuterH - VerticalChrome);

        // Horizontal slack: only applies when the subclass advertises a
        // narrower natural width than innerW.
        double? natural = DrawNaturalWidth(new PdfSize(innerW, innerH));
        double drawW = natural is double nw ? Math.Min(innerW, nw) : innerW;
        double hSlack = Math.Max(0, innerW - drawW);
        double xOffset = HorizontalAlignment switch
        {
            Alignment.Center => hSlack / 2,
            Alignment.End => hSlack,
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
            Alignment.Center => vSlack / 2,
            Alignment.End => vSlack,
            _ => 0,
        };

        if (yOffset != 0) sub.SetParentPosition(innerX + xOffset, innerY + yOffset);

        PaintBackgroundAndBorders(cs, outerW, outerH);
        sub.Build();

        return RenderResult.Done(outerH);
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
