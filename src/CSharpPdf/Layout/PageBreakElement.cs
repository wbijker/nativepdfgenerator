namespace CSharpPdf.Layout;

/// <summary>
/// A zero-size sentinel that forces a fresh page wherever it appears. The
/// <see cref="LayoutEngine"/> recognises it as a top-level element and starts a
/// new page; <see cref="RowsElement"/> recognises it inside its slots, ends the
/// current Rows render at that point, and emits a continuation Rows containing
/// the slots that come after — which the engine then renders on the next page.
/// </summary>
public sealed class PageBreakElement : UIElement
{
    public override Size MinimalSpaceRequired => Size.Zero;
    public override Size PreferredSize => Size.Zero;
    internal override double MinRenderHeight(Size available) => 0;

    protected override Size MeasureCore(Size available) => Size.Zero;

    protected override RenderResult RenderCore(PdfContext context, Size available) =>
        new(null, context.Cursor);
}
