using CSharpPdf.Geometry;
using CSharpPdf.Navigation;

namespace CSharpPdf.Layout;

/// <summary>
/// Wraps a child element and overlays a PDF link annotation on top of its
/// rendered area. Set <see cref="Url"/> for an external URL link or
/// <see cref="Target"/> for a named-destination jump within the document.
/// </summary>
public sealed class LinkElement : UIElement
{
    public UIElement? Content { get; set; }

    /// <summary>External URL (mutually exclusive with <see cref="Target"/>).</summary>
    public string? Url { get; set; }

    /// <summary>Named destination to jump to (created via <see cref="NamedAnchorElement"/>).</summary>
    public string? Target { get; set; }

    public LinkElement() { }
    public LinkElement(UIElement content, string? url = null, string? target = null)
    {
        Content = content;
        Url = url;
        Target = target;
    }

    public override Size MinimalSpaceRequired => Content?.MinimalSpaceRequired ?? Size.Zero;
    public override Size PreferredSize => Content?.PreferredSize ?? Size.Zero;
    internal override double MinRenderHeight(Size available) => Content?.MinRenderHeight(available) ?? 0;

    protected override Size MeasureCore(Size available) => Content?.Measure(available) ?? Size.Zero;

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        if (Content is null)
        {
            return new RenderResult(null, context.Cursor);
        }
        Point start = context.Cursor;
        var result = Content.Render(context, available);
        double left = start.X;
        double top = start.Y;
        double bottom = System.Math.Min(top, result.Next.Y);
        double right = left + available.Width;
        var rect = new PdfRectangle(left, bottom, right, top);

        Objects.PdfDictionary? action = Url switch
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
