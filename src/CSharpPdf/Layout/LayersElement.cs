namespace CSharpPdf.Layout;

/// <summary>
/// Stacks every child at the same origin (the element's cursor) and the same size
/// (<see cref="Height"/> tall, full available width). PDF paints in content-stream
/// order, so the first child becomes the bottom layer and subsequent children paint
/// on top — useful for background + overlay compositions (image / shapes / text).
/// The element itself occupies the box (width, Height) in the flow.
/// </summary>
public sealed class LayersElement : UIElement
{
    /// <summary>Children drawn back-to-front (index 0 = bottom).</summary>
    public List<UIElement> Children { get; } = new();

    /// <summary>Total height of the layered block in points.</summary>
    public double Height { get; set; }

    public LayersElement() { }
    public LayersElement(double height, params UIElement[] children)
    {
        Height = height;
        Children.AddRange(children);
    }

    public override Size MinimalSpaceRequired
    {
        get
        {
            double w = 0;
            foreach (var c in Children) w = System.Math.Max(w, c.MinimalSpaceRequired.Width);
            return new Size(w, Height);
        }
    }

    public override Size PreferredSize
    {
        get
        {
            double w = 0;
            foreach (var c in Children) w = System.Math.Max(w, c.PreferredSize.Width);
            return new Size(w, Height);
        }
    }

    internal override double MinRenderHeight(Size available) => Height;

    protected override Size MeasureCore(Size available) => new(available.Width, Height);

    protected override RenderResult RenderCore(PdfContext context, Size available)
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
