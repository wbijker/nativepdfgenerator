using PdfSpec.Geometry;
using PdfSpec.Objects;
using PdfSpec.Structure;

namespace PdfSpec.Content;

/// <summary>
/// A form XObject (ISO 32000-1 §8.10): a reusable, self-contained content
/// stream painted with the Do operator. <see cref="Resources"/> carries the
/// form's named resources (fonts, XObjects, ExtGStates, …). The Do operator
/// wraps painting in an implicit q/Q and clips to the BBox.
/// </summary>
public sealed class FormXObject
{
    private readonly PdfRectangle _boundingBox;

    public FormXObject(PdfRectangle boundingBox) => _boundingBox = boundingBox;

    /// <summary>The form's content stream.</summary>
    public ContentStream Content { get; } = new();

    /// <summary>The form's <c>/Resources</c> sub-object; not inherited from any page that paints the form.</summary>
    public Resources Resources { get; } = new();

    public PdfStream Build()
    {
        var stream = PdfPage.MakeContentStream(Content.ToBytes());
        var d = stream.Dictionary;
        d.SetName("Type", "XObject");
        d.SetName("Subtype", "Form");
        d.Add("BBox", _boundingBox.ToArray());
        if (!Resources.IsEmpty) d.Add("Resources", Resources.Dictionary);
        return stream;
    }
}
