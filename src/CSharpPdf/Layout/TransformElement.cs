namespace CSharpPdf.Layout;

/// <summary>
/// Wraps a child in a PDF graphics-state transform: <c>q ... cm ... Q</c>. The
/// child is rotated around a pivot inside its own measured box and / or scaled,
/// then rendered through its normal Render path; the transform applies to every
/// drawing operation the child emits into the content stream (text, images, SVG,
/// shapes). The element still consumes its <em>untransformed</em> size in the
/// surrounding layout — a rotated label can overflow its slot; combine with
/// <see cref="LayersElement"/> or a Fixed-height slot to reserve room.
/// Annotations (links, sticky notes, stamps) are page-level objects and are not
/// affected by the CTM.
/// </summary>
public sealed class TransformElement : UIElement
{
    public UIElement? Content { get; set; }

    /// <summary>Rotation in degrees, counter-clockwise positive (PDF user-space convention).</summary>
    public double Rotate { get; set; }

    public double ScaleX { get; set; } = 1;
    public double ScaleY { get; set; } = 1;

    /// <summary>Pivot point as a 0..1 fraction of the content size. Default (0.5, 0.5) = centre.</summary>
    public double PivotX { get; set; } = 0.5;
    public double PivotY { get; set; } = 0.5;

    public TransformElement() { }
    public TransformElement(UIElement content) { Content = content; }

    public override SpaceDimension SpaceRequired(SizeRect available)
    {
        if (Content is null) return SpaceDimension.Empty;
        // The element consumes its untransformed size in the surrounding flow.
        // The rotation/scale is purely visual.
        var inner = Content.SpaceRequired(available);
        return new SpaceDimension(inner.Minimal, inner.Recommended, verticalBreakable: false);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        if (Content is null)
        {
            return new RenderResult(null, context.Cursor);
        }

        Point start = context.Cursor;
        var space = Content.SpaceRequired(new SizeRect(available.Width, available.Height));
        double mW = space.Recommended.Width;
        double mH = space.Recommended.Height ?? available.Height;
        double px = start.X + PivotX * mW;
        double py = start.Y - PivotY * mH;

        var cs = context.Page.Content;
        cs.Save();
        // To rotate / scale around (px, py) in row-vector PDF math:
        // CTM ⟵ T(+p) · R · S · T(−p) · old. Issuing cm in this order leaves
        // each emitted coord shifted-by-(−p), transformed, shifted-back-by(+p).
        cs.Translate(px, py);
        if (Rotate != 0) cs.Rotate(Rotate);
        if (ScaleX != 1 || ScaleY != 1) cs.Scale(ScaleX, ScaleY);
        cs.Translate(-px, -py);

        var result = Content.Render(context, available);
        cs.Restore();
        return result;
    }
}
