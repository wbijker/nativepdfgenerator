using PdfSpec.Content;
using PdfSpec.Geometry;

namespace PdfSpec.Layout;

public abstract partial class Element
{
    public abstract PdfSizeHint SizeHint(PdfSize available);

    /// <summary>
    /// Fires once per <see cref="Render"/> call, just after the element
    /// has been drawn, with a <see cref="RenderedData"/> snapshot
    /// containing the page it landed on, that page's 1-based number, and
    /// its bounding rectangle in PDF user coords (directly usable as an
    /// annotation <c>Rect</c>). <c>null</c> by default → zero cost when
    /// unused; the firing path also early-outs when the content stream
    /// isn't attached to a page (e.g. inside a Form XObject body).
    /// </summary>
    public Action<RenderedData>? OnRendered { get; set; }

    /// <summary>
    /// Sealed render entry point: invokes <see cref="RenderCore"/> for
    /// the subclass-specific drawing, then fires <see cref="OnRendered"/>
    /// against the rectangle the element actually occupied on the page.
    /// The rectangle shape comes from <see cref="GetRenderedExtent"/> —
    /// override that on subclasses that know a tighter rectangle than
    /// the default (full available width × <see cref="RenderResult.NextY"/>).
    /// </summary>
    public RenderResult Render(ContentStream cs, PdfSize available)
    {
        var result = RenderCore(cs, available);
        FireOnRendered(cs, available, result);
        return result;
    }

    /// <summary>Subclass-specific drawing. See <see cref="Render"/> for the contract.</summary>
    protected abstract RenderResult RenderCore(ContentStream cs, PdfSize available);

    /// <summary>
    /// Width / height of the on-page rectangle the element occupied,
    /// fed to the <see cref="OnRendered"/> hook. The top-left is always
    /// the sub-stream origin (0, 0). Default is "the slot you were
    /// placed into" — full <paramref name="available"/>.Width by
    /// <paramref name="result"/>.NextY. Override when a tighter
    /// rectangle is known: <see cref="Elements.BoxElement"/> reports
    /// its outerW × outerH so chrome (background, borders) defines
    /// the bounds rather than the slot.
    /// </summary>
    protected virtual (double Width, double Height) GetRenderedExtent(PdfSize available, RenderResult result) =>
        (available.Width, result.NextY);

    private void FireOnRendered(ContentStream cs, PdfSize available, RenderResult result)
    {
        if (OnRendered is not { } hook) return;
        if (cs.OwningPage is not { } page) return;

        var (w, h) = GetRenderedExtent(available, result);
        var (ux, uy) = cs.ToPageUserPoint(0, 0);
        double pageH = page.PageHeight;
        var bounds = new PdfRectangle(ux, pageH - (uy + h), ux + w, pageH - uy);
        hook(new RenderedData(page, page.PageNumber, bounds));
    }
}
