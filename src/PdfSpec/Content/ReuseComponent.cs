using PdfSpec.Geometry;
using PdfSpec.Objects;
using PdfSpec.Structure;

namespace PdfSpec.Content;

/// <summary>
/// A reusable chunk of static drawing — a logo, header chrome, repeated
/// decoration. Wraps a Form XObject (ISO 32000-1 §8.10): operators are written
/// once into the component's <see cref="Content"/> stream, embedded once on
/// the document as an indirect XObject, and painted at each site via <c>Do</c>.
///
/// <para>Dedup is by reference identity — the same instance drawn N times
/// produces one XObject + N short <c>Do</c> calls.</para>
/// </summary>
public sealed class ReuseComponent
{
    private readonly FormXObject _form;
    private PdfReference? _embeddedRef;

    public double Width { get; }
    public double Height { get; }

    public ReuseComponent(PdfDoc doc, double width, double height)
    {
        Width = width;
        Height = height;
        _form = new FormXObject(doc, new PdfRectangle(0, 0, width, height));
    }

    public ContentStream Content => _form.Content;

    /// <summary>The component's own <c>/Resources</c> — not inherited from any page that paints it.</summary>
    public Resources Resources => _form.Resources;

    public PdfReference EmbedIn(PdfDoc doc)
    {
        if (_embeddedRef is { } cached) return cached;
        _embeddedRef = doc.AddObject(_form.Build());
        return _embeddedRef;
    }
}
