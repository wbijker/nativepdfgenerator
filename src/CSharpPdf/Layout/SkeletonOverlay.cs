using CSharpPdf.Content;
using Font = PdfSpec.Fonts.Font;

using PdfSpec.Geometry;
namespace CSharpPdf.Layout;

/// <summary>
/// Renders stroked rectangles for every <see cref="RenderedInfo"/> in
/// <c>captured</c> whose <see cref="RenderedInfo.Page"/> matches
/// <c>targetPage</c>, plus a small caption next to each box. Used by sample
/// 49 to overlay a "skeleton" of one page's layout onto another. Pages share
/// a content-area origin, so the captured PDF-absolute boundary translated
/// back to this canvas's local coords lands at the same visual position.
/// </summary>
public sealed class SkeletonOverlay : Element
{
    private readonly System.Collections.Generic.IReadOnlyList<(string Label, RenderedInfo Info)> _captured;
    private readonly int _targetPage;

    public Color Stroke { get; set; } = Colors.Red;
    public double LineWidth { get; set; } = 0.5;
    public Font LabelFont { get; set; } = PdfSpec.Fonts.Standard14Font.Helvetica;
    public double LabelSize { get; set; } = 8;

    public SkeletonOverlay(System.Collections.Generic.IReadOnlyList<(string Label, RenderedInfo Info)> captured, int targetPage)
    {
        _captured = captured;
        _targetPage = targetPage;
    }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        // Claim the parent's full offered height: the boundaries are drawn at
        // absolute coords that mirror the previous page's layout, so the
        // overlay's slot should span the same vertical band rather than
        // collapsing to zero height and letting other items sit on top.
        double width = available.Width;
        double height = available.Height ?? 0;
        return new SpaceDimension(
            new SizeRect(width, 0),
            new SizeRect(width, height),
            verticalBreakable: false);
    }

    protected override RenderResult RenderCore(PdfCanvas canvas, Size available)
    {
        foreach (var (label, info) in _captured)
        {
            if (info.Page != _targetPage) continue;
            double localX = canvas.ToLocalX(info.Boundary.X);
            double localTop = canvas.ToLocalY(info.Boundary.Y);
            canvas.StrokeRectangle(localX, localTop, info.Boundary.Width, info.Boundary.Height, Stroke, LineWidth);

            // Tiny caption just inside the top edge.
            double captionBaseline = localTop - 2;
            canvas.DrawText(LabelFont, LabelSize, localX + 2, captionBaseline, label, Stroke);
        }

        var next = new Point(canvas.Cursor.X, canvas.Cursor.Y - available.Height);
        return new RenderResult(null, next);
    }
}
