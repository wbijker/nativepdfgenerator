namespace CSharpPdf.Layout;

/// <summary>
/// Renders its child without space constraints and consumes no layout space, so
/// the child can overflow its parent (e.g. an overlay/watermark). The cursor is
/// left where it started, so following content draws on top.
/// </summary>
public sealed class UnconstrainedElement : UIElement
{
    public UIElement? Child { get; set; }

    public UnconstrainedElement() { }
    public UnconstrainedElement(UIElement child) { Child = child; }

    public override SpaceDimension SpaceRequired(SizeRect available) => SpaceDimension.Empty;

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        Point start = context.Cursor;
        Child?.Render(context, new Size(double.MaxValue, double.MaxValue));
        context.Cursor = start;
        return new RenderResult(null, start);
    }
}
