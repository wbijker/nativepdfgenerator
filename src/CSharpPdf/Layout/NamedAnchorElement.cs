using CSharpPdf.Objects;

namespace CSharpPdf.Layout;

/// <summary>
/// A zero-size element that registers a named destination at the current cursor.
/// Pair with a <see cref="LinkElement"/> whose <c>Target</c> is the same name to
/// jump here from anywhere in the document.
/// </summary>
public sealed class NamedAnchorElement : UIElement
{
    public string Name { get; set; } = "";

    public NamedAnchorElement() { }
    public NamedAnchorElement(string name) { Name = name; }

    public override Size MinimalSpaceRequired => Size.Zero;
    public override Size PreferredSize => Size.Zero;

    protected override Size MeasureCore(Size available) => Size.Zero;

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        Point start = context.Cursor;
        if (!string.IsNullOrEmpty(Name))
        {
            var dest = new PdfArray(
                context.Page.Reference,
                new PdfName("XYZ"),
                new PdfNumber(start.X),
                new PdfNumber(start.Y),
                new PdfNumber(0));
            context.Document.AddNamedDestination(Name, dest);
        }
        return new RenderResult(null, start);
    }
}
