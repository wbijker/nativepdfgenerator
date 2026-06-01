using CSharpPdf.Content;
using CSharpPdf.Objects;

namespace CSharpPdf.Layout;

/// <summary>
/// A zero-size element that registers a named destination at the current cursor.
/// Pair with a <see cref="LinkElement"/> whose <c>Target</c> is the same name to
/// jump here from anywhere in the document.
/// </summary>
public sealed class NamedAnchorElement : UIElement
{
    public string Name { get; set; } = "";

    public NamedAnchorElement() { }
    public NamedAnchorElement(string name) { Name = name; }

    public override SpaceDimension SpaceHint(SizeRect available) => SpaceDimension.Empty;

    /// <summary>Key under which the anchor publishes its page number into the context's capture store.</summary>
    public static string PageKey(string name) => $"anchor.{name}.page";

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        Point start = context.Cursor;
        if (!string.IsNullOrEmpty(Name))
        {
            // Two-phase capture: measure phase records this anchor's page so
            // PageReferenceElement (or any future reader) can format the right
            // number during render. No-ops in render.
            context.Capture(PageKey(Name), context.PageNumber);

            if (context.Mode == RenderMode.Render)
            {
                // Destinations carry absolute PDF coordinates, but `start`
                // lives in this canvas's local space. Translate before writing.
                var dest = new PdfArray(
                    context.Page.Reference,
                    new PdfName("XYZ"),
                    new PdfNumber(context.ToAbsoluteX(start.X)),
                    new PdfNumber(context.ToAbsoluteY(start.Y)),
                    new PdfNumber(0));
                context.Document.AddNamedDestination(Name, dest);
            }
        }
        return new RenderResult(null, start);
    }
}
