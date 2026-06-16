using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

public abstract class Element
{
    public abstract PdfSizeHint SizeHint(PdfSize available);

    /// <summary>
    /// Fires once per <see cref="Render"/> call, just after the element
    /// has been drawn. Backing field — concrete subclasses expose
    /// chainable setter methods (e.g.
    /// <see cref="BorderElement.OnRendered"/>); the snapshot
    /// contains the page it landed on, that page's 1-based number, and
    /// its bounding rectangle in PDF user coords (directly usable as an
    /// annotation <c>Rect</c>). <c>null</c> by default → zero cost when
    /// unused; the firing path also early-outs when the content stream
    /// isn't attached to a page (e.g. inside a Form XObject body).
    /// </summary>
    protected internal Action<RenderedData>? OnRendered;

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
    /// rectangle is known: <see cref="BoxElement"/> reports
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


    public static Paragraph Paragraph(string text, Font font, double size) => new(text, font, size);

    /// <summary>Helvetica 11 — the conventional body-text default.</summary>
    public static Paragraph Paragraph(string text) => new(text, StandardFont.Helvetica, 11);

    /// <summary>
    /// Multi-span paragraph (low-level lambda form). <paramref name="defaultFont"/>
    /// is used by <c>.Text(string)</c> calls in the builder; per-span fonts
    /// can be passed explicitly. All spans share <paramref name="size"/>.
    /// </summary>
    public static Paragraph Paragraph(Font defaultFont, double size, Action<Paragraph> build) =>
        new(defaultFont, size, build);

    /// <summary>
    /// Multi-span paragraph (high-level family form). Returns a
    /// <see cref="FamilyParagraph"/> onto which spans are added via
    /// <c>.Bold(...)</c>, <c>.Italic(...)</c>, <c>.BoldItalic(...)</c>,
    /// <c>.Text(...)</c>, and <c>.Newline()</c>.
    /// </summary>
    public static FamilyParagraph Paragraph(FontFamily family, double size) => new(family, size);

    /// <summary>
    /// Multi-span paragraph (family + lambda). Combines the family form's
    /// face-aware setters with a builder lambda — useful when the spans
    /// are built imperatively rather than chained.
    /// </summary>
    public static FamilyParagraph Paragraph(FontFamily family, double size, Action<FamilyParagraph> build) =>
        new(family, size, build);

    /// <summary>
    /// Reflow paragraph (family form). Returns a <see cref="ReflowParagraph"/>
    /// onto which spans and floats are added via the chainable builder.
    /// </summary>
    public static ReflowParagraph ReflowParagraph(FontFamily family, double size) => new(family, size);

    /// <summary>
    /// Reflow paragraph (family + lambda). Builds the paragraph imperatively
    /// inside <paramref name="build"/>.
    /// </summary>
    public static ReflowParagraph ReflowParagraph(FontFamily family, double size, Action<ReflowParagraph> build) =>
        new(family, size, build);

    public static VStack VStack() => new();

    public static VStack VStack(Action<VStack> build)
    {
        var v = new VStack();
        build(v);
        return v;
    }

    public static HStack HStack() => new();

    public static HStack HStack(Action<HStack> build)
    {
        var h = new HStack();
        build(h);
        return h;
    }

    public static VFrame VFrame() => new();

    public static VFrame VFrame(Action<VFrame> build)
    {
        var f = new VFrame();
        build(f);
        return f;
    }

    /// <summary>
    /// A styled-chrome container (background, padding, per-side borders,
    /// alignment) wrapping a single child — backed by
    /// <see cref="BorderElement"/>.
    /// </summary>
    public static BorderElement Container() => new();

    public static BorderElement Container(Action<BorderElement> build)
    {
        var c = new BorderElement();
        build(c);
        return c;
    }

    /// <summary>
    /// An imperative drawing surface — inside <paramref name="draw"/> the
    /// sub-stream's (0, 0) is the surface's top-left in user coords.
    /// </summary>
    public static Canvas Canvas(double width, double height, Action<ContentStream, PdfSize> draw) =>
        new() { Width = width, Height = height, Draw = draw };

    /// <summary>
    /// Two-phase deferred rendering — <paramref name="sizeHint"/> reserves
    /// the box during normal layout, <paramref name="render"/> runs once
    /// the page count is known and decides what actually paints there.
    /// </summary>
    public static DeferredComponent Deferred(Element sizeHint, Func<PageData, Element> render) =>
        new(sizeHint, render);

    /// <summary>Sentinel that forces the next item in its parent container onto a new page.</summary>
    public static PageBreak PageBreak() => new();
}