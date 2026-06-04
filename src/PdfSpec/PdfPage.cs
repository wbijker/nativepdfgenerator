using PdfSpec.Actions;
using PdfSpec.Annotations;
using PdfSpec.Content;
using PdfSpec.Filters;
using PdfSpec.Geometry;
using PdfSpec.Objects;
using PdfSpec.Structure;
using PdfSpec.Text;

namespace PdfSpec;

/// <summary>
/// A single page — a leaf <c>/Page</c> node in the page tree (ISO 32000-1
/// §7.7.3.3). Holds typed fields for the page state (boxes, rotation,
/// resources, content, annotations); emits the <c>/Page</c> dictionary fresh
/// at write time.
/// </summary>
public sealed class PdfPage : PdfObject
{
    private readonly PdfDoc _document;
    private readonly PdfObjectStore _store;
    private readonly PdfReference _parent;
    private PdfReference? _reference;
    private readonly Resources _resources = new();
    private ContentStream? _content;
    private PdfReference? _contentRef;
    private List<PdfReference>? _annotations;
    private int? _rotation;

    // Per-page ExtGState dedup keyed by ExtGState instance.
    private readonly Dictionary<ExtGState, string> _extGStateNames = new();
    private int _extGStateSeq;

    internal PdfPage(PdfDoc document, PdfObjectStore store, PdfReference parentPageTree)
    {
        _document = document;
        _store = store;
        _parent = parentPageTree;
    }

    internal void SetReference(PdfReference reference) => _reference = reference;

    /// <summary>The page object's indirect reference (assigned when the page is added to the document).</summary>
    public PdfReference Reference =>
        _reference ?? throw new InvalidOperationException("Page reference is not assigned until the page is added to a document.");

    /// <summary>The owning document.</summary>
    public PdfDoc Document => _document;

    /// <summary>The page's <see cref="Structure.Resources"/> sub-object (fonts, XObjects, ExtGState, shadings, patterns, properties).</summary>
    public Resources Resources => _resources;

    /// <summary>The page's content stream — every drawing operator (raw or typed) lives here. Created on first access; serialized into <c>/Contents</c> at save.</summary>
    public ContentStream Content => _content ??= new ContentStream(this);

    /// <summary>The page's media box (overrides the page-tree inherited default).</summary>
    public PdfRectangle? MediaBox { get; set; }

    /// <summary>The page's crop box (visible region; pinned to MediaBox by viewers).</summary>
    public PdfRectangle? CropBox { get; set; }

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
        }
    }

    /// <summary>The page's UserUnit scale (default 1.0 == 72 units/inch).</summary>
    public double? UserUnit { get; set; }

    /// <summary>
    /// When true (default), page and form content streams are FlateDecode-compressed
    /// when written. Turn off for debugging or when the consumer can't decode Flate.
    /// </summary>
    public static bool CompressContentStreams = true;

    /// <summary>Register a font on this page (deduplicating via the document), returning the resource name to pass to <c>Tf</c>.</summary>
    public string UseFont(Font font)
    {
        var (name, reference) = _document.UseFont(font);
        _resources.AddFont(name, reference);
        return name;
    }

    /// <summary>Register an ExtGState on this page (dedup by instance), returning the resource name to pass to <c>gs</c>.</summary>
    public string UseExtGState(ExtGState gs)
    {
        if (!_extGStateNames.TryGetValue(gs, out var name))
        {
            name = $"GS{++_extGStateSeq}";
            _resources.AddExtGState(name, _store.Add(gs.Build()));
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
            stream.Dictionary.Add("Filter", new PdfName("FlateDecode"));
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
        _annotations ??= new List<PdfReference>();
        _annotations.Add(annotRef);
        return annotRef;
    }

    /// <summary>Add a Link annotation triggering <paramref name="action"/>.</summary>
    public PdfReference AddLink(PdfRectangle rect, PdfAction action) =>
        AddAnnotation(new LinkAnnotation(rect, action));

    /// <summary>Add a Link annotation that opens <paramref name="url"/>.</summary>
    public PdfReference AddUrlLink(PdfRectangle rect, string url) =>
        AddLink(rect, new UriAction(url));

    /// <summary>Add a Link annotation that jumps to an explicit <see cref="Destination"/>.</summary>
    public PdfReference AddGoToLink(PdfRectangle rect, Destination destination) =>
        AddLink(rect, new GoToAction(destination));

    /// <summary>Add a Link annotation that jumps to a named destination (via the Dests name tree).</summary>
    public PdfReference AddGoToLink(PdfRectangle rect, string namedDestination) =>
        AddLink(rect, new NamedDestinationAction(namedDestination));

    /// <summary>Add a sticky-note Text annotation cross-linked to a Pop-up annotation.</summary>
    public void AddTextNote(PdfRectangle iconRect, string contents, string icon, PdfRectangle popupRect, bool open = true)
    {
        var noteDict = new TextAnnotation(iconRect, contents, icon).Build();
        noteDict.Add("P", Reference);
        var noteRef = _store.Add(noteDict);

        var popupDict = new PopupAnnotation(popupRect, open) { Parent = noteRef }.Build();
        popupDict.Add("P", Reference);
        var popupRef = _store.Add(popupDict);

        noteDict.Add("Popup", popupRef);
        _annotations ??= new List<PdfReference>();
        _annotations.Add(noteRef);
        _annotations.Add(popupRef);
    }

    internal void FlushContent()
    {
        if (_content is not null)
        {
            var stream = MakeContentStream(_content.ToBytes());
            _contentRef = _store.Add(stream);
        }
    }

    public override void Write(Stream stream) => Build().Write(stream);

    private PdfDictionary Build()
    {
        var d = new PdfDictionary
        {
            { "Type", new PdfName("Page") },
            { "Parent", _parent },
        };
        if (MediaBox is { } mb) d.Add("MediaBox", mb.ToArray());
        if (CropBox is { } cb) d.Add("CropBox", cb.ToArray());
        if (_rotation is { } r) d.Add("Rotate", new PdfNumber(r));
        if (UserUnit is { } uu) d.Add("UserUnit", new PdfNumber(uu));
        if (!_resources.IsEmpty) d.Add("Resources", _resources.Build());
        if (_contentRef is { } c) d.Add("Contents", c);
        if (_annotations is { Count: > 0 })
        {
            var arr = new PdfArray();
            foreach (var a in _annotations) arr.Add(a);
            d.Add("Annots", arr);
        }
        return d;
    }
}
