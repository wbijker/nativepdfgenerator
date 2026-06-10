using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Reserve a <see cref="Width"/> × <see cref="Height"/> drawing surface
/// inside a layout and hand its sub-content-stream to <see cref="Draw"/>
/// — the escape hatch for imperative drawing (paths, transforms, raw
/// text positioning) inside a flex container. Coordinates inside Draw
/// are top-left origin in the sub's local space, so a sample originally
/// written against page-absolute (x, y) maps cleanly by treating the
/// sub's (0, 0) as the section's origin.
/// </summary>
public sealed class Canvas : Element
{
    public double Width { get; set; }
    public double Height { get; set; }

    /// <summary>Called with the sub-content-stream and its size at render time.</summary>
    public Action<ContentStream, PdfSize>? Draw { get; set; }

    public override PdfSizeHint SizeHint(PdfSize available) =>
        PdfSizeHint.Fixed(Math.Min(Width, available.Width), Math.Min(Height, available.Height));

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        double w = Math.Min(Width, available.Width);
        double h = Math.Min(Height, available.Height);
        if (Draw is { } draw) draw(cs, new PdfSize(w, h));
        return RenderResult.Done(h);
    }
}
