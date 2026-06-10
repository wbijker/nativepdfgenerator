using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Layout-side sentinel that forces the next item in its parent
/// container onto a new page. Render claims the full available height
/// (the current page's remaining space) and returns it as its NextY;
/// the wrapping flex container (e.g. a <see cref="VStack"/>) then sees
/// the next item exceeds the remaining height and defers it via the
/// usual Partial-continuation path, which <see cref="PdfPage.Body"/>
/// resolves by calling <see cref="PdfPage.PageBreak"/>.
/// </summary>
public sealed class PageBreak : Element
{
    public override PdfSizeHint SizeHint(PdfSize available) =>
        new(0, available.Height, 0, available.Height);

    public override RenderResult Render(ContentStream cs, PdfSize available) =>
        RenderResult.Done(available.Height);
}
