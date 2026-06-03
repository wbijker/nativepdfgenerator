using CSharpPdf.Content;
using CSharpPdf.Objects;

namespace CSharpPdf.Layout;

/// <summary>
/// A zero-size element that registers a named destination at the current cursor.
/// Pair with a <see cref="LinkElement"/> whose <c>Target</c> is the same name to
/// jump here from anywhere in the document.
/// </summary>
public sealed class NamedAnchorElement : Element
{
    public string Name { get; set; } = "";

    /// <summary>
    /// When true, register this anchor in the document's <c>/Dests</c> name
    /// tree at render time (so <see cref="LinkElement.Target"/>
    /// named-destination links can resolve it). Default is
    /// <see cref="RegisterInDestsByDefault"/>.
    ///
    /// When false, the anchor still publishes its page into the canvas
    /// capture store — <see cref="LinkExplicitElement"/> (TargetAnchor) and
    /// <see cref="PageReferenceElement"/> continue to resolve it normally —
    /// but the name tree entry is skipped. If no anchor in the document
    /// registers, the <c>/Dests</c> tree is omitted entirely, dropping the
    /// per-entry destination dictionaries from the file.
    /// </summary>
    public bool RegisterInDests { get; set; } = RegisterInDestsByDefault;

    /// <summary>
    /// Default value of <see cref="RegisterInDests"/> for newly-constructed
    /// anchors. Set to <c>false</c> once at startup if your document never
    /// uses named-destination links (<see cref="LinkElement.Target"/>) and
    /// only uses <see cref="LinkExplicitElement"/> (LinkToAnchor) for
    /// in-document jumps.
    /// </summary>
    public static bool RegisterInDestsByDefault = true;

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
            // Always publish the page number to the capture store — explicit
            // links (LinkExplicitElement) and PageReferenceElement read this
            // regardless of whether the /Dests tree is populated.
            context.Capture(PageKey(Name), context.PageNumber);

            if (RegisterInDests && context.Mode == RenderMode.Render)
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
