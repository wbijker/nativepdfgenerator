using CSharpPdf.Svg;

namespace CSharpPdf.Layout;

/// <summary>
/// Draws an SVG fragment at a fixed display size by emitting PDF content-stream
/// operators (see <see cref="SvgRenderer"/>). The source SVG's viewBox is mapped
/// to the requested <see cref="DisplayWidth"/> × <see cref="DisplayHeight"/>
/// rectangle.
/// </summary>
public sealed class SvgElement : UIElement
{
    /// <summary>The raw SVG XML to render.</summary>
    public string Svg { get; set; } = "";

    public double DisplayWidth { get; set; }
    public double DisplayHeight { get; set; }

    public SvgElement() { }
    public SvgElement(string svg, double displayWidth, double displayHeight)
    {
        Svg = svg;
        DisplayWidth = displayWidth;
        DisplayHeight = displayHeight;
    }

    public override Size MinimalSpaceRequired => new(DisplayWidth, DisplayHeight);
    public override Size PreferredSize => new(DisplayWidth, DisplayHeight);

    protected override Size MeasureCore(Size available) => new(DisplayWidth, DisplayHeight);

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        var renderer = new SvgRenderer(context.Page.Content);
        renderer.Render(Svg, context.Cursor.X, context.Cursor.Y, DisplayWidth, DisplayHeight);
        return new RenderResult(null, new Point(context.Cursor.X, context.Cursor.Y - DisplayHeight));
    }
}
