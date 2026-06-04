using PdfSpec.Actions;
using PdfSpec.Annotations;
using PdfSpec.Content;
using PdfSpec.Filters;
using PdfSpec.Geometry;
using PdfSpec.Objects;
using PdfSpec.Structure;
using PdfSpec.Fonts;

namespace PdfSpec;

/// <summary>
/// A single page — a leaf <c>/Page</c> node in the page tree (ISO 32000-1
/// §7.7.3.3). Wraps a single <see cref="PdfDictionary"/> mutated in place as
/// properties are set and annotations / content are added; <see cref="Write"/>
/// delegates to it — no per-save allocation.
/// </summary>
public sealed class PdfPage : PdfObject
{
    private readonly PdfDoc _document;
    private readonly PdfObjectStore _store;
    private readonly PdfDictionary _dictionary = new();
    private readonly Resources _resources = new();
    private PdfReference? _reference;
    private ContentStream? _content;
    private PdfArray? _annotations;
    private int? _rotation;

    // Per-page ExtGState dedup keyed by ExtGState instance.
    private readonly Dictionary<ExtGState, string> _extGStateNames = new();
    private int _extGStateSeq;

    internal PdfPage(PdfDoc document, PdfObjectStore store)
    {
        _document = document;
        _store = store;
        _dictionary.SetName("Type", "Page");
        _dictionary.Add("Resources", _resources.Dictionary);
    }

    internal void SetReference(PdfReference reference) => _reference = reference;

    /// <summary>Set the page's <c>/Parent</c> entry — the indirect reference to its containing /Pages leaf.</summary>
    internal void SetParent(PdfReference parent) => _dictionary.Set("Parent", parent);

    /// <summary>The page object's indirect reference (assigned when the page is added to the document).</summary>
    public PdfReference Reference =>
        _reference ?? throw new InvalidOperationException("Page reference is not assigned until the page is added to a document.");

    /// <summary>The owning document.</summary>
    public PdfDoc Document => _document;

    /// <summary>The page's <see cref="Structure.Resources"/> sub-object (fonts, XObjects, ExtGState, shadings, patterns, properties).</summary>
    public Resources Resources => _resources;

    /// <summary>The page's content stream. Created on first access; serialized into <c>/Contents</c> at save.</summary>
    public ContentStream Content => _content ??= new ContentStream(this);

    /// <summary>The page's media box (overrides the page-tree inherited default).</summary>
    public PdfRectangle? MediaBox { set => _dictionary.Set("MediaBox", value?.ToArray()); }

    /// <summary>The page's crop box (visible region; pinned to MediaBox by viewers).</summary>
    public PdfRectangle? CropBox { set => _dictionary.Set("CropBox", value?.ToArray()); }

    /// <summary>Page rotation in degrees clockwise — must be a multiple of 90.</summary>
    public int? Rotation
    {
        get => _rotation;
        set
        {
            if (value is { } v && v % 90 != 0)
            {
                throw new ArgumentException("Rotation must be a multiple of 90.", nameof(value));
            }
            _rotation = value;
            _dictionary.SetInteger("Rotate", value);
        }
    }

    /// <summary>The page's UserUnit scale (default 1.0 == 72 units/inch).</summary>
    public double? UserUnit { set => _dictionary.SetNumber("UserUnit", value); }

    /// <summary>
    /// When true (default), page and form content streams are FlateDecode-compressed
    /// when written.
    /// </summary>
    public static bool CompressContentStreams = true;

    /// <summary>
    /// Emit a <c>Tf</c> operator directly to this page's content stream —
    /// outside any text object — making <paramref name="font"/> at
    /// <paramref name="size"/> the current graphics-state font. Subsequent
    /// <see cref="Content.Text"/> blocks snapshot it via their <c>q</c> and
    /// inherit it on <c>BT</c>; calling <c>SetFont</c> inside a block
    /// overrides for that block only. New pages created after
    /// <see cref="PdfDoc.SetDefaultFont"/> have its value applied here
    /// automatically by <see cref="PdfDoc.AddPage"/>.
    /// </summary>
    public void SetDefaultFont(Font font, double size)
    {
        var fontRef = UseFont(font);
        Content.Raw($"/{PdfName.Escape(FontNameOf(fontRef))} {ContentStream.N(size)} Tf");
    }

    // Per-page lookup from the doc-wide font reference to the resource
    // name used on this page (and in content streams as the Tf argument).
    private readonly Dictionary<PdfReference, string> _fontNames = new();

    /// <summary>
    /// Register <paramref name="font"/> on this page (and the document, deduped
    /// by <see cref="Font.Key"/>), returning the indirect reference to its
    /// <c>/Font</c> dictionary. The resource name needed for the <c>Tf</c>
    /// operator is recoverable via <see cref="FontNameOf"/>.
    /// </summary>
    public PdfReference UseFont(Font font)
    {
        var resource = _document.UseFont(font);
        if (_fontNames.TryAdd(resource.Reference, resource.Name))
        {
            _resources.AddFont(resource.Name, resource.Reference);
        }
        return resource.Reference;
    }

    /// <summary>
    /// Get the per-page resource name (e.g. <c>Fnt1</c>) for a font reference
    /// returned by <see cref="UseFont"/> — needed by content-stream emission
    /// to fill the <c>Tf</c> argument.
    /// </summary>
    internal string FontNameOf(PdfReference fontRef) =>
        _fontNames.TryGetValue(fontRef, out var name)
            ? name
            : throw new InvalidOperationException("Font reference is not registered on this page. Call PdfPage.UseFont first.");

    /// <summary>Register an ExtGState on this page (dedup by instance), returning the resource name to pass to <c>gs</c>.</summary>
    public string UseExtGState(ExtGState gs)
    {
        if (!_extGStateNames.TryGetValue(gs, out var name))
        {
            name = $"GS{++_extGStateSeq}";
            _resources.AddExtGState(name, _store.Add(gs.Dictionary));
            _extGStateNames[gs] = name;
        }
        return name;
    }

    /// <summary>
    /// Wrap content-stream bytes in a <see cref="PdfStream"/>, applying
    /// FlateDecode when <see cref="CompressContentStreams"/> is on.
    /// </summary>
    internal static PdfStream MakeContentStream(byte[] bytes)
    {
        if (CompressContentStreams)
        {
            var compressed = FlateFilter.Encode(bytes);
            var stream = new PdfStream(compressed);
            stream.Dictionary.SetName("Filter", "FlateDecode");
            return stream;
        }
        return new PdfStream(bytes);
    }

    /// <summary>Add a typed annotation to the page; the <c>/P</c> link is set automatically.</summary>
    public PdfReference AddAnnotation(Annotation annotation)
    {
        var dict = annotation.Build();
        dict.Add("P", Reference);
        var annotRef = _store.Add(dict);
        if (_annotations is null)
        {
            _annotations = new PdfArray();
            _dictionary.Add("Annots", _annotations);
        }
        _annotations.Add(annotRef);
        return annotRef;
    }

    public PdfReference AddLink(PdfRectangle rect, PdfAction action) =>
        AddAnnotation(new LinkAnnotation(rect, action));

    public PdfReference AddUrlLink(PdfRectangle rect, string url) =>
        AddLink(rect, new UriAction(url));

    public PdfReference AddGoToLink(PdfRectangle rect, Destination destination) =>
        AddLink(rect, new GoToAction(destination));

    public PdfReference AddGoToLink(PdfRectangle rect, string namedDestination) =>
        AddLink(rect, new NamedDestinationAction(namedDestination));

    public void AddTextNote(PdfRectangle iconRect, string contents, TextAnnotationIcon icon, PdfRectangle popupRect, bool open = true)
    {
        var noteDict = new TextAnnotation(iconRect, contents, icon).Build();
        noteDict.Add("P", Reference);
        var noteRef = _store.Add(noteDict);

        var popupDict = new PopupAnnotation(popupRect, open) { Parent = noteRef }.Build();
        popupDict.Add("P", Reference);
        var popupRef = _store.Add(popupDict);

        noteDict.Add("Popup", popupRef);

        if (_annotations is null)
        {
            _annotations = new PdfArray();
            _dictionary.Add("Annots", _annotations);
        }
        _annotations.Add(noteRef);
        _annotations.Add(popupRef);
    }

    internal void FlushContent()
    {
        if (_content is not null)
        {
            var stream = MakeContentStream(_content.ToBytes());
            _dictionary.Add("Contents", _store.Add(stream));
        }
    }

    public override void Write(Stream stream) => _dictionary.Write(stream);
}
