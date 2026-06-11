using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec.Layout;

public abstract class Element
{
    public abstract PdfSizeHint SizeHint(PdfSize available);

    /// <summary>
    /// Fires once per <see cref="Render"/> call, just after the element
    /// has been drawn. Backing field — concrete subclasses expose
    /// chainable setter methods (e.g.
    /// <see cref="Elements.BorderElement.OnRendered"/>); the snapshot
    /// contains the page it landed on, that page's 1-based number, and
    /// its bounding rectangle in PDF user coords (directly usable as an
    /// annotation <c>Rect</c>). <c>null</c> by default → zero cost when
    /// unused; the firing path also early-outs when the content stream
    /// isn't attached to a page (e.g. inside a Form XObject body).
    /// </summary>
    protected internal Action<RenderedData>? _onRendered;

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
        if (_onRendered is not { } hook) return;
        if (cs.OwningPage is not { } page) return;

        var (w, h) = GetRenderedExtent(available, result);
        var (ux, uy) = cs.ToPageUserPoint(0, 0);
        double pageH = page.PageHeight;
        var bounds = new PdfRectangle(ux, pageH - (uy + h), ux + w, pageH - uy);
        hook(new RenderedData(page, page.PageNumber, bounds));
    }

    // ===== fluent factories ==================================================
    //
    // Entry points for the fluent builder surface — Element.Paragraph(text, font,
    // size), Element.VStack(v => ...), Element.Container(), and so on. Each
    // returns an instance of the concrete imperative layout type with all its
    // fluent chainable setters available. The closure-form factories run the
    // builder against a freshly-constructed instance and return it, so child
    // population reads naturally inside a parent's argument list.

    public static Elements.Paragraph Paragraph(string text, Font font, double size) => new(text, font, size);

    /// <summary>Helvetica 11 — the conventional body-text default.</summary>
    public static Elements.Paragraph Paragraph(string text) => new(text, StandardFont.Helvetica, 11);

    public static Elements.VStack VStack() => new();
    public static Elements.VStack VStack(Action<Elements.VStack> build)
    {
        var v = new Elements.VStack();
        build(v);
        return v;
    }

    public static Elements.HStack HStack() => new();
    public static Elements.HStack HStack(Action<Elements.HStack> build)
    {
        var h = new Elements.HStack();
        build(h);
        return h;
    }

    public static Elements.VFrame VFrame() => new();
    public static Elements.VFrame VFrame(Action<Elements.VFrame> build)
    {
        var f = new Elements.VFrame();
        build(f);
        return f;
    }

    /// <summary>
    /// A styled-chrome container (background, padding, per-side borders,
    /// alignment) wrapping a single child — backed by
    /// <see cref="Elements.BorderElement"/>.
    /// </summary>
    public static Elements.BorderElement Container() => new();
    public static Elements.BorderElement Container(Action<Elements.BorderElement> build)
    {
        var c = new Elements.BorderElement();
        build(c);
        return c;
    }

    /// <summary>
    /// An imperative drawing surface — inside <paramref name="draw"/> the
    /// sub-stream's (0, 0) is the surface's top-left in user coords.
    /// </summary>
    public static Elements.Canvas Canvas(double width, double height, Action<ContentStream, PdfSize> draw) =>
        new() { Width = width, Height = height, Draw = draw };

    /// <summary>
    /// Two-phase deferred rendering — <paramref name="sizeHint"/> reserves
    /// the box during normal layout, <paramref name="render"/> runs once
    /// the page count is known and decides what actually paints there.
    /// </summary>
    public static Elements.DeferredComponent Deferred(Element sizeHint, Func<PageData, Element> render) =>
        new(sizeHint, render);

    /// <summary>Sentinel that forces the next item in its parent container onto a new page.</summary>
    public static Elements.PageBreak PageBreak() => new();
}
