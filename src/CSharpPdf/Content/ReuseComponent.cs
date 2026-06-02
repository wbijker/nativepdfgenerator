using CSharpPdf.Geometry;
using CSharpPdf.Objects;

namespace CSharpPdf.Content;

/// <summary>
/// A reusable chunk of static drawing — a logo, header chrome, repeated
/// decoration, anything that should appear identically at many sites. Wraps
/// a Form XObject (ISO 32000-1 §8.10): the operators are written once into
/// the component's <see cref="Content"/> stream, embedded once on the
/// document as an indirect XObject, and painted at each site via the
/// <c>Do</c> operator.
///
/// <para>
/// The encoded operators appear once in the file regardless of how many
/// times the component is drawn. Dedup is by reference identity — the same
/// <see cref="ReuseComponent"/> instance drawn N times produces one XObject
/// + N short <c>Do</c> calls (each prefixed with a <c>q cm</c>/<c>Q</c> to
/// position and scale it). Two distinct instances with identical content
/// produce two XObjects.
/// </para>
///
/// <para>
/// Usage:
/// <code>
/// var logo = new ReuseComponent(width: 100, height: 40);
/// logo.Content.Save()
///   .SetRgbFill(0.86, 0.15, 0.15)
///   .Rectangle(0, 0, 100, 40).Fill()
///   .Restore();
///
/// canvas.DrawComponent(logo, x: 50, top: 750);   // first use: embeds once
/// canvas.DrawComponent(logo, x: 50, top: 100);   // second use: just Do
/// </code>
/// </para>
///
/// <para>
/// The component's own coordinate system runs (0, 0) at the lower-left and
/// (<see cref="Width"/>, <see cref="Height"/>) at the upper-right — the same
/// orientation as a PDF page. Content drawn outside the BBox is clipped.
/// </para>
/// </summary>
public sealed class ReuseComponent
{
    private readonly FormXObject _form;

    // Doc-level dedup: cached after the first time the component is embedded
    // so subsequent canvases on other pages reuse the same indirect reference.
    private PdfReference? _embeddedRef;

    /// <summary>Width of the component in points (the local x extent).</summary>
    public double Width { get; }

    /// <summary>Height of the component in points (the local y extent, Y-up).</summary>
    public double Height { get; }

    public ReuseComponent(double width, double height)
    {
        Width = width;
        Height = height;
        _form = new FormXObject(new PdfRectangle(0, 0, width, height));
    }

    /// <summary>
    /// The component's content stream — write drawing operators here. The
    /// stream is captured into the XObject on first <see cref="EmbedIn"/>;
    /// changes made after that point won't appear in the painted output.
    /// </summary>
    public ContentStream Content => _form.Content;

    /// <summary>
    /// Register a resource (font, image, ExtGState, nested form) that this
    /// component's content stream references. The component carries its own
    /// /Resources dict — resources registered here aren't inherited from any
    /// page that paints the component.
    /// </summary>
    public void AddResource(string category, string name, PdfObject value) =>
        _form.AddResource(category, name, value);

    /// <summary>
    /// Add this component to <paramref name="doc"/> as an indirect Form
    /// XObject and return the reference. Cached on the instance — subsequent
    /// calls return the same reference, so the operators appear once in the
    /// file regardless of how many pages paint the component.
    /// </summary>
    public PdfReference EmbedIn(PdfDoc doc)
    {
        if (_embeddedRef is { } cached) return cached;
        _embeddedRef = doc.AddObject(_form.Build());
        return _embeddedRef;
    }
}
