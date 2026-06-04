using CSharpPdf.Content;
using PdfSpec.Geometry;
using PdfSpec.Navigation;
using PdfSpec.Objects;

namespace CSharpPdf.Layout;

/// <summary>
/// Wraps a child element and overlays a PDF link annotation on top of its
/// rendered area. Set <see cref="Url"/> for an external URL link or
/// <see cref="Target"/> for a named-destination jump within the document.
/// </summary>
public sealed class LinkElement : Element
{
    public Element? Content { get; set; }

    /// <summary>External URL (mutually exclusive with <see cref="Target"/>).</summary>
    public string? Url { get; set; }

    /// <summary>Named destination to jump to (created via <see cref="NamedAnchorElement"/>).</summary>
    public string? Target { get; set; }

    public LinkElement() { }
    public LinkElement(Element content, string? url = null, string? target = null)
    {
        Content = content;
        Url = url;
        Target = target;
    }

    public override SpaceDimension SpaceHint(SizeRect available) =>
        Content?.SpaceHint(available) ?? SpaceDimension.Empty;

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        if (Content is null)
        {
            return new RenderResult(null, context.Cursor);
        }
        Point start = context.Cursor;
        var result = Content.Render(context, available);
        // Annotation rectangles use absolute PDF coordinates — translate
        // from this canvas's local cursor before building the rect.
        double left = context.ToAbsoluteX(start.X);
        double top = context.ToAbsoluteY(start.Y);
        double bottom = System.Math.Min(top, context.ToAbsoluteY(result.Next.Y));
        double right = left + available.Width;
        var rect = new PdfRectangle(left, bottom, right, top);

        PdfDictionary? action = Url switch
        {
            { } u => PdfAction.Uri(u),
            _ => Target is { } t ? PdfAction.GoToNamed(t) : null,
        };
        if (action is not null)
        {
            context.Page.AddLinkAnnotation(rect, action);
        }
        return result;
    }
}
