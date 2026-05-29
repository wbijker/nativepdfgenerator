namespace CSharpPdf.Layout;

/// <summary>
/// Everything a component needs to draw, supplied by the layout engine. Placement
/// is abstracted: <see cref="Left"/>/<see cref="Top"/> give the top-left corner of
/// the region (in PDF coordinates, y increasing upward) that the engine has set
/// aside for this component, so components never compute page coordinates
/// themselves.
/// </summary>
public sealed class RenderContext
{
    internal RenderContext(PdfDocument document, PdfPage page, double left, double top, int pageNumber)
    {
        Document = document;
        Page = page;
        Left = left;
        Top = top;
        PageNumber = pageNumber;
    }

    /// <summary>The document (font registry, etc.).</summary>
    public PdfDocument Document { get; }

    /// <summary>The page currently being drawn into.</summary>
    public PdfPage Page { get; }

    /// <summary>PDF x of the region's left edge.</summary>
    public double Left { get; }

    /// <summary>PDF y of the region's top edge (y increases upward).</summary>
    public double Top { get; }

    /// <summary>1-based number of the current page.</summary>
    public int PageNumber { get; }

    /// <summary>A context with the region moved to a new top-left corner (PDF coords).</summary>
    internal RenderContext At(double left, double top) => new(Document, Page, left, top, PageNumber);

    /// <summary>A context inset from the top-left by (dx right, dy down).</summary>
    internal RenderContext Inset(double dx, double dy) =>
        dx == 0 && dy == 0 ? this : new RenderContext(Document, Page, Left + dx, Top - dy, PageNumber);
}
