namespace CSharpPdf.Layout;

/// <summary>
/// Renders its child without space constraints and consumes no layout space, so
/// the child can overflow its parent (e.g. an overlay/watermark). The cursor is
/// left where it started, so following content draws on top.
/// </summary>
public sealed class UnconstrainedElement : UIElement<UnconstrainedElement>
{
    private readonly UIElement _child;

    public UnconstrainedElement(UIElement child) => _child = child;

    public override Size MinimalSpaceRequired => Size.Zero;
    public override Size PreferredSize => Size.Zero;

    internal override double MinRenderHeight(Size available) => 0;

    protected override Size MeasureCore(Size available) => Size.Zero;

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        Point start = context.Cursor;
        _child.Render(context, new Size(double.MaxValue, double.MaxValue));
        context.Cursor = start;
        return new RenderResult(null, start);
    }
}
