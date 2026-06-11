using PdfSpec.Content;
using PdfSpec.Elements;
using PdfSpec.Fonts;

namespace PdfSpec.Layout;

/// <summary>
/// Static factories for the layout primitives. The fluent surface is a
/// thin convenience on top of the imperative API — <c>Element.VStack()</c>
/// is equivalent to <c>new VStack()</c>, <c>Element.Paragraph(text, font,
/// size)</c> is equivalent to <c>new Paragraph(text, font, size)</c>, and
/// so on. The closure-form factories (<c>Element.VStack(v => …)</c>) run
/// the builder against the freshly-constructed instance and return it,
/// so child population reads naturally inside a parent's argument list.
/// </summary>
public abstract partial class Element
{
    /// <summary>A wrapped text run at the given font + size.</summary>
    public static Paragraph Paragraph(string text, Font font, double size) =>
        new(text, font, size);

    /// <summary>A wrapped text run at Helvetica 11 — the conventional body font.</summary>
    public static Paragraph Paragraph(string text) =>
        new(text, StandardFont.Helvetica, 11);

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
    /// alignment) wrapping a single child. Backed by
    /// <see cref="BorderElement"/>; the child is set via the fluent
    /// <c>.Content(child)</c> / <c>.Paragraph(text, …)</c> / etc. methods.
    /// </summary>
    public static BorderElement Container() => new();
    public static BorderElement Container(Action<BorderElement> build)
    {
        var b = new BorderElement();
        build(b);
        return b;
    }

    /// <summary>
    /// An imperative drawing surface of the given size — the escape
    /// hatch into raw content-stream operators. Inside <paramref name="draw"/>
    /// the sub-stream's (0, 0) is the surface's top-left.
    /// </summary>
    public static Canvas Canvas(double width, double height, Action<ContentStream, PdfSize> draw) =>
        new() { Width = width, Height = height, Draw = draw };

    /// <summary>
    /// Layout-side sentinel that forces the next item in its parent
    /// container onto a new page (see <see cref="Elements.PageBreak"/>).
    /// </summary>
    public static PageBreak PageBreak() => new();
}
