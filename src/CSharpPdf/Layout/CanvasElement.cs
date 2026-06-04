using CSharpPdf.Content;

using PdfSpec.Geometry;
namespace CSharpPdf.Layout;

/// <summary>
/// Reserves a fixed-size rectangle and hands a fresh sub-<see cref="PdfCanvas"/>
/// to a user-supplied draw callback — the fluent equivalent of writing a custom
/// <see cref="Element"/>. Used by <c>Container.Canvas(width, height, draw)</c>.
///
/// The sub-canvas given to the callback has local <c>(0,0)</c> at the bottom-left
/// of the allocation, Y-up; <c>canvas.Cursor</c> starts at the top-left
/// <c>(0, height)</c>; <c>canvas.Width</c> and <c>canvas.Height</c> match the
/// requested allocation (or the actually-available size if smaller). Atomic —
/// never breaks across pages.
/// </summary>
public sealed class CanvasElement : Element
{
    /// <summary>Desired width of the drawing surface in points.</summary>
    public double CanvasWidth { get; set; }

    /// <summary>Desired height of the drawing surface in points.</summary>
    public double CanvasHeight { get; set; }

    /// <summary>The callback invoked once per render to fill the surface.</summary>
    public System.Action<PdfCanvas, Size>? Draw { get; set; }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        double w = System.Math.Min(CanvasWidth, available.Width);
        double h = CanvasHeight;
        var size = new SizeRect(w, h);
        return new SpaceDimension(size, size, verticalBreakable: false);
    }

    protected override RenderResult RenderCore(PdfCanvas canvas, Size available)
    {
        Point start = canvas.Cursor;
        double w = System.Math.Min(CanvasWidth, available.Width);
        double h = System.Math.Min(CanvasHeight, available.Height);
        if (Draw is { } draw)
        {
            var sub = canvas.Sub(start.X, start.Y, w, h);
            draw(sub, new Size(w, h));
        }
        return new RenderResult(null, new Point(start.X, start.Y - h));
    }
}
