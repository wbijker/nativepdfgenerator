using CSharpPdf.Content;
namespace CSharpPdf.Layout;

/// <summary>
/// Stacks every child at the same origin (the element's cursor) and the same size
/// (<see cref="Height"/> tall, full available width). PDF paints in content-stream
/// order, so the first child becomes the bottom layer and subsequent children paint
/// on top — useful for background + overlay compositions (image / shapes / text).
/// The element itself occupies the box (width, Height) in the flow.
/// </summary>
public sealed class LayersElement : Element
{
    /// <summary>Children drawn back-to-front (index 0 = bottom).</summary>
    public List<Element> Children { get; } = new();

    /// <summary>Total height of the layered block in points.</summary>
    public double Height { get; set; }

    public LayersElement() { }
    public LayersElement(double height, params Element[] children)
    {
        Height = height;
        Children.AddRange(children);
    }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var inner = InnerAvailable(available);
        var size = new SizeRect(inner.Width, Height);
        return WithOwnInset(new SpaceDimension(size, size, verticalBreakable: false));
    }

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        Point start = context.Cursor;
        var size = new Size(available.Width, Height);
        foreach (var child in Children)
        {
            context.Cursor = start;
            child.Render(context, size);
        }
        return new RenderResult(null, new Point(start.X, start.Y - Height));
    }
}
