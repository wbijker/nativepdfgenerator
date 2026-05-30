namespace CSharpPdf.Layout;

/// <summary>
/// Renders its child with an infinite available height so the child cannot be
/// paginated — everything is drawn in one stretch starting at the current cursor.
/// If the child is taller than the remaining page area it will spill past the
/// bottom margin; the engine then resumes layout on the next page automatically
/// (because the cursor lands below the content area, the next element triggers a
/// new page via the normal flow). Useful for content that must not break.
/// </summary>
public sealed class ShowAllElement : UIElement
{
    public UIElement? Content { get; set; }

    public ShowAllElement() { }
    public ShowAllElement(UIElement content) { Content = content; }

    public override Size MinimalSpaceRequired => Content?.MinimalSpaceRequired ?? Size.Zero;
    public override Size PreferredSize => Content?.PreferredSize ?? Size.Zero;
    internal override double MinRenderHeight(Size available) => Content?.MinRenderHeight(available) ?? 0;

    protected override Size MeasureCore(Size available) =>
        Content?.Measure(new Size(available.Width, double.MaxValue)) ?? Size.Zero;

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        if (Content is null)
        {
            return new RenderResult(null, context.Cursor);
        }
        var result = Content.Render(context, new Size(available.Width, double.MaxValue));
        // Drop any reported overflow — "show all" means render the child whole.
        return new RenderResult(null, result.Next);
    }
}
