using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec.Layout;

public abstract class Element
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

    // ===== fluent factories ==================================================
    //
    // Entry points for the fluent builder layer — Element.Paragraph(text, font,
    // size), Element.VStack(v => ...), Element.Container(), and so on — return
    // PdfSpec.Fluent.* wrapper instances. The closure-form factories run the
    // builder against a freshly-constructed instance and return it, so child
    // population reads naturally inside a parent's argument list.
    //
    // Lives on the imperative base so a file with `using PdfSpec.Layout;` can
    // reach them via `Element.X(...)` without also importing PdfSpec.Fluent.

    public static Fluent.Paragraph Paragraph(string text, Font font, double size) => new(text, font, size);

    /// <summary>Helvetica 11 — the conventional body-text default.</summary>
    public static Fluent.Paragraph Paragraph(string text) => new(text, StandardFont.Helvetica, 11);

    public static Fluent.VStack VStack() => new();
    public static Fluent.VStack VStack(Action<Fluent.VStack> build)
    {
        var v = new Fluent.VStack();
        build(v);
        return v;
    }

    public static Fluent.HStack HStack() => new();
    public static Fluent.HStack HStack(Action<Fluent.HStack> build)
    {
        var h = new Fluent.HStack();
        build(h);
        return h;
    }

    public static Fluent.VFrame VFrame() => new();
    public static Fluent.VFrame VFrame(Action<Fluent.VFrame> build)
    {
        var f = new Fluent.VFrame();
        build(f);
        return f;
    }

    /// <summary>
    /// A styled-chrome container (background, padding, per-side borders,
    /// alignment) wrapping a single child. Wraps
    /// <see cref="Elements.BorderElement"/>.
    /// </summary>
    public static Fluent.Container Container() => new();
    public static Fluent.Container Container(Action<Fluent.Container> build)
    {
        var c = new Fluent.Container();
        build(c);
        return c;
    }

    /// <summary>
    /// An imperative drawing surface — inside <paramref name="draw"/> the
    /// sub-stream's (0, 0) is the surface's top-left in user coords.
    /// </summary>
    public static Fluent.Canvas Canvas(double width, double height, Action<ContentStream, PdfSize> draw) =>
        new(width, height, draw);

    /// <summary>
    /// Two-phase deferred rendering — <paramref name="sizeHint"/> reserves
    /// the box during normal layout, <paramref name="render"/> runs once
    /// the page count is known and decides what actually paints there.
    /// </summary>
    public static Fluent.Deferred Deferred(Fluent.Element sizeHint, Func<PageData, Fluent.Element> render) =>
        new(sizeHint, render);

    /// <summary>Sentinel that forces the next item in its parent container onto a new page.</summary>
    public static Fluent.PageBreak PageBreak() => new();
}
