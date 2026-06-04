using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Objects;
using PdfSpec.Structure;

namespace PdfSpec.Content;

/// <summary>
/// A form XObject (ISO 32000-1 §8.10): a reusable, self-contained content
/// stream painted with the Do operator. <see cref="Resources"/> carries the
/// form's named resources (fonts, XObjects, ExtGStates, …). The Do operator
/// wraps painting in an implicit q/Q and clips to the BBox. Bound to a
/// <see cref="PdfDoc"/> at construction so typed <c>SetFont(Font, …)</c> /
/// <c>SetExtGState(ExtGState)</c> auto-register on the form's own resources
/// (and the document-wide font registry).
/// </summary>
public sealed class FormXObject
{
    private readonly PdfDoc _doc;
    private readonly PdfRectangle _boundingBox;

    public FormXObject(PdfDoc doc, PdfRectangle boundingBox)
    {
        _doc = doc;
        _boundingBox = boundingBox;
        Content = new ContentStream(this);
    }

    /// <summary>The form's content stream.</summary>
    public ContentStream Content { get; }

    /// <summary>The form's <c>/Resources</c> sub-object; not inherited from any page that paints the form.</summary>
    public Resources Resources { get; } = new();

    // ===== Resource registration (mirrors PdfPage) =============================
    // Forms have their own /Resources, so font/ExtGState registration lives
    // here on the form, not on whatever page eventually paints the form.

    private readonly Dictionary<PdfReference, string> _fontNames = new();
    private readonly Dictionary<ExtGState, string> _extGStateNames = new();
    private readonly Dictionary<ExtGState, PdfReference> _extGStateRefs = new();
    private readonly Dictionary<PdfReference, string> _extGStateNamesByRef = new();
    private int _extGStateSeq;

    /// <summary>
    /// Register <paramref name="font"/> on this form (and the document,
    /// deduped by <see cref="Font.Key"/>), returning the indirect reference
    /// to its <c>/Font</c> dictionary.
    /// </summary>
    public PdfReference UseFont(Font font)
    {
        var resource = _doc.UseFont(font);
        if (_fontNames.TryAdd(resource.Reference, resource.Name))
        {
            Resources.AddFont(resource.Name, resource.Reference);
        }
        return resource.Reference;
    }

    /// <summary>
    /// Per-form resource name for a font reference. If the reference is
    /// known to the document but hasn't been added to this form's resources
    /// yet, it's registered on demand.
    /// </summary>
    public string FontNameOf(PdfReference fontRef)
    {
        if (_fontNames.TryGetValue(fontRef, out var name)) return name;
        var resource = _doc.FindFont(fontRef) ?? throw new InvalidOperationException(
            "Font reference is not known to the document.");
        Resources.AddFont(resource.Name, resource.Reference);
        _fontNames[resource.Reference] = resource.Name;
        return resource.Name;
    }

    /// <summary>Register <paramref name="gs"/> on this form (dedup by instance), returning the indirect reference to its dictionary.</summary>
    public PdfReference UseExtGState(ExtGState gs)
    {
        if (!_extGStateNames.TryGetValue(gs, out var name))
        {
            name = $"GS{++_extGStateSeq}";
            var reference = _doc.AddObject(gs.Dictionary);
            Resources.AddExtGState(name, reference);
            _extGStateNames[gs] = name;
            _extGStateRefs[gs] = reference;
            _extGStateNamesByRef[reference] = name;
        }
        return _extGStateRefs[gs];
    }

    /// <summary>Per-form resource name for an ExtGState reference returned by <see cref="UseExtGState"/>.</summary>
    public string ExtGStateNameOf(PdfReference gsRef) =>
        _extGStateNamesByRef.TryGetValue(gsRef, out var name)
            ? name
            : throw new InvalidOperationException("ExtGState reference is not registered on this form. Call FormXObject.UseExtGState first.");

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
