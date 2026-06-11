using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Layout;
using ImperativeElement = PdfSpec.Layout.Element;

namespace PdfSpec.Fluent;

/// <summary>
/// Base of the fluent builder layer. Each fluent type wraps an
/// imperative <see cref="ImperativeElement"/> instance and exposes
/// chainable setters that mutate the wrapped state in place. The fluent
/// hierarchy is intentionally separate from the imperative
/// <see cref="ImperativeElement"/> tree — no inheritance, no partial
/// classes, no name collisions between the two layers — so a file
/// picks one API and never mixes.
///
/// <para>
/// The static factories (<see cref="Paragraph(string, Font, double)"/>,
/// <see cref="VStack()"/>, <see cref="HStack()"/>,
/// <see cref="Container()"/>, …) are the entry points; the
/// closure-form factories (e.g. <c>Element.VStack(v => …)</c>) run the
/// builder against a freshly-constructed instance and return it, so
/// child population reads naturally inside a parent's argument list.
/// </para>
///
/// <para>
/// <see cref="Build"/> is internal — the assembly translates a fluent
/// tree to the underlying imperative tree at the document boundary
/// (<see cref="PdfDoc"/> / <see cref="PdfPage"/>), so callers never
/// need to see it.
/// </para>
/// </summary>
public abstract class Element
{
    /// <summary>Hand back the underlying imperative <see cref="ImperativeElement"/> this fluent builder describes.</summary>
    internal abstract ImperativeElement Build();

    // ===== factories =========================================================

    public static Paragraph Paragraph(string text, Font font, double size) => new(text, font, size);

    /// <summary>Helvetica 11 — the conventional body-text default.</summary>
    public static Paragraph Paragraph(string text) => new(text, StandardFont.Helvetica, 11);

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
    /// alignment) wrapping a single child. Wraps
    /// <see cref="Elements.BorderElement"/>.
    /// </summary>
    public static Container Container() => new();
    public static Container Container(Action<Container> build)
    {
        var c = new Container();
        build(c);
        return c;
    }

    /// <summary>
    /// An imperative drawing surface — inside <paramref name="draw"/> the
    /// sub-stream's (0, 0) is the surface's top-left in user coords.
    /// </summary>
    public static Canvas Canvas(double width, double height, Action<ContentStream, PdfSize> draw) =>
        new(width, height, draw);

    /// <summary>
    /// Two-phase deferred rendering — <paramref name="sizeHint"/> reserves
    /// the box during normal layout, <paramref name="render"/> runs once
    /// the page count is known and decides what actually paints there.
    /// </summary>
    public static Deferred Deferred(Element sizeHint, Func<PageData, Element> render) =>
        new(sizeHint, render);

    /// <summary>Sentinel that forces the next item in its parent container onto a new page.</summary>
    public static PageBreak PageBreak() => new();
}
