using PdfSpec.Objects;

namespace PdfSpec.Actions;

/// <summary>
/// An explicit destination (ISO 32000-1 §12.3.2.2): a target page plus a
/// view-fit instruction telling the viewer how to position the page in its
/// window. Used by <see cref="GoToAction"/> for clickable jumps, by named
/// destinations in <c>PdfDoc.AddNamedDestination</c>, and (where supported)
/// by outline items and the catalog's OpenAction.
///
/// <para>
/// All distances are in PDF user-space points (origin at the bottom-left of
/// the page). Construct via the static factory methods; each corresponds to
/// one of the eight fit modes defined in Table 151.
/// </para>
/// </summary>
public sealed class Destination
{
    private readonly PdfArray _array;

    private Destination(PdfArray array) => _array = array;

    /// <summary>The underlying destination array <c>[page /fit-mode args…]</c>.</summary>
    public PdfArray Build() => _array;

    /// <summary>
    /// <c>[page /XYZ left top zoom]</c> — display the page positioned so the
    /// coordinates (<paramref name="left"/>, <paramref name="top"/>) appear
    /// at the upper-left of the window, at the given <paramref name="zoom"/>
    /// factor (1.0 = 100%). Pass <c>null</c> for any of the three to leave
    /// that aspect of the current view unchanged.
    /// </summary>
    public static Destination Xyz(PdfPage page, double? left, double? top, double? zoom)
    {
        var array = new PdfArray(page.Reference, new PdfName("XYZ"));
        array.Add(left is { } l ? new PdfNumber(l) : PdfNull.Instance);
        array.Add(top is { } t ? new PdfNumber(t) : PdfNull.Instance);
        array.Add(zoom is { } z ? new PdfNumber(z) : PdfNull.Instance);
        return new Destination(array);
    }

    /// <summary>
    /// <c>[page /Fit]</c> — fit the entire page within the window, centering
    /// it both horizontally and vertically. The most common destination for
    /// "jump to page N" links and for outline items.
    /// </summary>
    public static Destination Fit(PdfPage page) =>
        new(new PdfArray(page.Reference, new PdfName("Fit")));

    /// <summary>
    /// <c>[page /FitH top]</c> — fit the page width to the window and scroll
    /// so the horizontal line at user-space coordinate <paramref name="top"/>
    /// sits at the top of the window. Vertical zoom matches the horizontal fit.
    /// </summary>
    public static Destination FitH(PdfPage page, double top) =>
        new(new PdfArray(page.Reference, new PdfName("FitH"), new PdfNumber(top)));

    /// <summary>
    /// <c>[page /FitV left]</c> — fit the page height to the window and scroll
    /// so the vertical line at user-space coordinate <paramref name="left"/>
    /// sits at the left of the window. Horizontal zoom matches the vertical fit.
    /// </summary>
    public static Destination FitV(PdfPage page, double left) =>
        new(new PdfArray(page.Reference, new PdfName("FitV"), new PdfNumber(left)));

    /// <summary>
    /// <c>[page /FitR left bottom right top]</c> — fit the given rectangle
    /// (in user-space coordinates) entirely within the window, choosing the
    /// smaller of the two fit factors. Use to zoom directly onto a region of
    /// interest such as a figure or table.
    /// </summary>
    public static Destination FitR(PdfPage page, double left, double bottom, double right, double top) =>
        new(new PdfArray(page.Reference, new PdfName("FitR"),
            new PdfNumber(left), new PdfNumber(bottom),
            new PdfNumber(right), new PdfNumber(top)));

    /// <summary>
    /// <c>[page /FitB]</c> — fit the page's <i>bounding box</i> (the
    /// smallest rectangle enclosing the page's actual visible content)
    /// within the window. Behaves like <see cref="Fit"/> for pages with no
    /// distinct bounding box. PDF 1.1+.
    /// </summary>
    public static Destination FitB(PdfPage page) =>
        new(new PdfArray(page.Reference, new PdfName("FitB")));

    /// <summary>
    /// <c>[page /FitBH top]</c> — like <see cref="FitH"/> but fits the
    /// width of the bounding box rather than the full media box, and scrolls
    /// to put <paramref name="top"/> at the top of the window. PDF 1.1+.
    /// </summary>
    public static Destination FitBH(PdfPage page, double top) =>
        new(new PdfArray(page.Reference, new PdfName("FitBH"), new PdfNumber(top)));

    /// <summary>
    /// <c>[page /FitBV left]</c> — like <see cref="FitV"/> but fits the
    /// height of the bounding box rather than the full media box, and scrolls
    /// to put <paramref name="left"/> at the left of the window. PDF 1.1+.
    /// </summary>
    public static Destination FitBV(PdfPage page, double left) =>
        new(new PdfArray(page.Reference, new PdfName("FitBV"), new PdfNumber(left)));
}
