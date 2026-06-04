using CSharpPdf.Content;

using PdfSpec.Geometry;
namespace CSharpPdf.Layout;

/// <summary>
/// Block whose visible content is determined late — after the layout pass has
/// settled the page count and every <see cref="NamedAnchorElement"/> /
/// <see cref="Element.OnRendered"/> hook has recorded its data. Two parts:
/// <list type="number">
///   <item>An <c>initial</c> element whose <see cref="Element.SpaceHint"/>
///         is queried to decide how much space the block reserves. The
///         initial is <i>not</i> drawn — its only job is to measure.</item>
///   <item>A <c>deferred</c> callback that runs after the layout pass
///         completes (via <see cref="PdfCanvas.Defer"/>) and draws the actual
///         content into the reserved area, with the now-final
///         <see cref="DynamicContext"/> in hand.</item>
/// </list>
///
/// Constraint: the deferred content must fit in the size the initial measured.
/// Overflow doesn't crash — it just bleeds outside the reserved box. Pick an
/// initial that represents the worst-case footprint of any deferred content
/// you intend to draw (e.g. a long placeholder string of the right length).
/// </summary>
public sealed class DynamicContentElement : Element
{
    private readonly Element _initial;
    private readonly System.Action<PdfCanvas, DynamicContext> _deferred;

    public DynamicContentElement(Element initial, System.Action<PdfCanvas, DynamicContext> deferred)
    {
        _initial = initial;
        _deferred = deferred;
    }

    public override SpaceDimension SpaceHint(SizeRect available) => _initial.SpaceHint(available);

    protected override RenderResult RenderCore(PdfCanvas canvas, Size available)
    {
        // Measure the initial to fix the reservation size.
        var space = _initial.SpaceHint(new SizeRect(available.Width, available.Height));
        double width = System.Math.Min(space.Recommended.Width, available.Width);
        double height = space.Recommended.Height ?? 0;

        // Reserve the area; the deferred queue runs after the layout pass.
        var deferred = _deferred;
        canvas.Defer(width, height, sub =>
        {
            deferred(sub, new DynamicContext(sub.PageNumber, sub.TotalPages));
        });

        // Advance the cursor by the reserved height so following items flow
        // below it. Width isn't propagated through the cursor — the parent
        // (a slot/row) already knows the layout's horizontal axis.
        return new RenderResult(null, new Point(canvas.Cursor.X, canvas.Cursor.Y - height));
    }
}
