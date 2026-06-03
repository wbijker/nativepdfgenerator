using CSharpPdf.Geometry;
using CSharpPdf.Objects;

namespace CSharpPdf.Content;

/// <summary>
/// A form XObject (Chapter 3, "Vector Images"): a reusable, self-contained
/// content stream that can be painted into any page or other content stream with
/// the Do operator. The only required key is BBox; the Do operator wraps painting
/// in an implicit q/Q and clips to the BBox.
/// </summary>
public sealed class FormXObject
{
    private readonly PdfRectangle _boundingBox;
    private PdfDictionary? _resources;

    public FormXObject(PdfRectangle boundingBox) => _boundingBox = boundingBox;

    /// <summary>The form's content stream (no enclosing q/Q needed; Do adds them).</summary>
    public ContentStream Content { get; } = new();

    /// <summary>Add a resource (font, image, ExtGState, nested form) used by the form.</summary>
    public void AddResource(string category, string name, PdfObject value)
    {
        _resources ??= new PdfDictionary();
        if (_resources.Get(category) is not PdfDictionary group)
        {
            group = new PdfDictionary();
            _resources[category] = group;
        }
        group[name] = value;
    }

    public PdfStream Build()
    {
        var stream = PdfPage.MakeContentStream(Content.ToBytes());
        var d = stream.Dictionary;
        d["Type"] = new PdfName("XObject");
        d["Subtype"] = new PdfName("Form");
        d["BBox"] = _boundingBox.ToArray();
        if (_resources is not null)
        {
            d["Resources"] = _resources;
        }
        return stream;
    }
}
