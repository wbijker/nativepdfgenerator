using CSharpPdf.Objects;

namespace CSharpPdf.Layout;

/// <summary>
/// A zero-size element that records an outline (bookmark) entry pointing at the
/// current cursor on the current page. The entries are flushed into a flat
/// outline tree by <c>LayoutEngine.Finish</c>.
/// </summary>
public sealed class BookmarkElement : UIElement
{
    public string Title { get; set; } = "";

    public BookmarkElement() { }
    public BookmarkElement(string title) { Title = title; }

    public override SpaceDimension SpaceHint(SizeRect available) => SpaceDimension.Empty;

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        Point start = context.Cursor;
        if (!string.IsNullOrEmpty(Title))
        {
            var dest = new PdfArray(
                context.Page.Reference,
                new PdfName("XYZ"),
                new PdfNumber(start.X),
                new PdfNumber(start.Y),
                new PdfNumber(0));
            context.PendingBookmarks.Add((Title, dest));
        }
        return new RenderResult(null, start);
    }
}
