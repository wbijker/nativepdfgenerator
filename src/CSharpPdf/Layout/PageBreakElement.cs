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
    public override SpaceDimension SpaceRequired(SizeRect available) => SpaceDimension.Empty;

    protected override RenderResult RenderCore(PdfContext context, Size available) =>
        new(null, context.Cursor);
}
