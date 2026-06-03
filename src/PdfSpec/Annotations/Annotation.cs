using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Annotations;

/// <summary>
/// Base for PDF annotation dictionaries (ISO 32000-1 §12.5). Each annotation
/// is keyed by a <c>/Subtype</c> and pinned to a rectangle on a page; the
/// /P (parent page) entry is filled in by <see cref="PdfPage.AddAnnotation"/>
/// once the annotation is added to a page.
/// </summary>
public abstract class Annotation
{
    public PdfRectangle Rect { get; }

    protected Annotation(PdfRectangle rect) => Rect = rect;

    public abstract PdfDictionary Build();

    /// <summary>Build the <c>/Type /Annot /Subtype ... /Rect ...</c> base dictionary.</summary>
    protected PdfDictionary Base(string subtype) => new()
    {
        ["Type"] = new PdfName("Annot"),
        ["Subtype"] = new PdfName(subtype),
        ["Rect"] = Rect.ToArray(),
    };
}
