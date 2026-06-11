using PdfSpec.Actions;
using PdfSpec.Annotations;
using PdfSpec.Content;
using PdfSpec.Elements;
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
    private readonly Dictionary<ExtGState, PdfReference> _extGStateRefs = new();
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
        _reference ??
        throw new InvalidOperationException("Page reference is not assigned until the page is added to a document.");

    /// <summary>The owning document.</summary>
    public PdfDoc Document => _document;

    /// <summary>
    /// 1-based index of this page in <see cref="PdfDoc.Pages"/>. Resolved
    /// by linear lookup — fine for sample-scale documents; the deferred-
    /// component pipeline does the same. Returns 0 if the page is no
    /// longer in the document.
    /// </summary>
    public int PageNumber
    {
        get
        {
            var pages = _document.Pages;
            for (int i = 0; i < pages.Count; i++)
                if (ReferenceEquals(pages[i], this))
                    return i + 1;
            return 0;
        }
    }

    /// <summary>The page's <see cref="Structure.Resources"/> sub-object (fonts, XObjects, ExtGState, shadings, patterns, properties).</summary>
    public Resources Resources => _resources;

    /// <summary>The page's content stream. Created on first access; serialized into <c>/Contents</c> at save.</summary>
    public ContentStream Content => _content ??= new ContentStream(this);

    private PdfRectangle? _mediaBox;

    /// <summary>The page's media box (overrides the page-tree inherited default).</summary>
    public PdfRectangle? MediaBox
    {
        get => _mediaBox;
        set
        {
            _mediaBox = value;
            _dictionary.Set("MediaBox", value?.ToArray());
        }
    }

    /// <summary>The page's width in user units — surfaced on the page's content stream as <see cref="ContentStream.Width"/>. Falls back to the document-level default media box when the page has no override.</summary>
    public double PageWidth => _mediaBox?.Width ?? _document.DefaultMediaBox?.Width ?? PageSizes.A4.Width;

    /// <summary>The page's height in user units — used by the top-left-origin coordinate flip on the page's content stream. Falls back to the document-level default media box when the page has no override.</summary>
    public double PageHeight => _mediaBox?.Height ?? _document.DefaultMediaBox?.Height ?? PageSizes.A4.Height;

    /// <summary>The page's crop box (visible region; pinned to MediaBox by viewers).</summary>
    public PdfRectangle? CropBox
    {
        set => _dictionary.Set("CropBox", value?.ToArray());
    }

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
    public double? UserUnit
    {
        set => _dictionary.SetNumber("UserUnit", value);
    }

    /// <summary>The raw page dictionary — escape hatch for entries not surfaced as typed properties (e.g. StructParents).</summary>
    public PdfDictionary Dictionary => _dictionary;

    /// <summary>Set the (overridden) MediaBox; legacy CSharpPdf-style imperative setter.</summary>
    public void SetMediaBox(PdfRectangle box) => MediaBox = box;

    /// <summary>Set the CropBox; legacy CSharpPdf-style imperative setter.</summary>
    public void SetCropBox(PdfRectangle box) => CropBox = box;

    /// <summary>Set the rotation in degrees clockwise; legacy CSharpPdf-style imperative setter.</summary>
    public void SetRotation(int degrees) => Rotation = degrees;

    /// <summary>Set the page's UserUnit; legacy CSharpPdf-style imperative setter.</summary>
    public void SetUserUnit(double userUnit) => UserUnit = userUnit;

    private Element? _header;
    private Element? _footer;
    private readonly List<Element> _accumulatedBodies = new();

    /// <summary>
    /// Set the shared header element rendered at the top of every PDF
    /// page produced by <see cref="Body"/>. Auto-sized to its content's
    /// <see cref="Layout.PdfSizeHint.MaxHeight"/> (falling back to
    /// MinHeight); the body slot gets whatever's left. Chainable.
    /// </summary>
    public PdfPage Header(Element element)
    {
        _header = element;
        return this;
    }

    /// <summary>
    /// Return an <see cref="IContainer"/> slot for the page header. A
    /// fresh <see cref="BorderElement"/> is installed as
    /// <see cref="_header"/> up-front; the returned container's chrome
    /// setters and content terminal mutate it in place.
    /// </summary>
    public IContainer Header()
    {
        var border = new BorderElement();
        _header = border;
        return new Container(border);
    }

    /// <summary>
    /// Set the shared footer element rendered at the bottom of every PDF
    /// page produced by <see cref="Body"/>. Same sizing rules as
    /// <see cref="Header"/>. A <see cref="Element.DeferredComponent"/>
    /// in the footer registers a fresh entry per PDF page, so each page
    /// can carry its own "Page N of M" with its own page-data snapshot.
    /// Chainable.
    /// </summary>
    public PdfPage Footer(Element element)
    {
        _footer = element;
        return this;
    }

    /// <summary>Return an <see cref="IContainer"/> slot for the page footer — installs a fresh <see cref="BorderElement"/> as <see cref="_footer"/> and returns a facade onto it.</summary>
    public IContainer Footer()
    {
        var border = new BorderElement();
        _footer = border;
        return new Container(border);
    }

    /// <summary>Return an <see cref="IContainer"/> slot that queues one body section — appends a fresh <see cref="BorderElement"/> to <see cref="_accumulatedBodies"/> and returns a facade onto it.</summary>
    public IContainer Body()
    {
        var border = new BorderElement();
        _accumulatedBodies.Add(border);
        return new Container(border);
    }

    /// <summary>
    /// Queue <paramref name="element"/> as a body section to render. The
    /// queue flushes when the surrounding
    /// <see cref="PdfDoc.AddPage(Action{PdfPage})"/> closure returns —
    /// each queued element becomes one logical page with the shared
    /// chrome rebuilt fresh. Chainable.
    /// </summary>
    public PdfPage AddBody(Element element)
    {
        _accumulatedBodies.Add(element);
        return this;
    }

    /// <summary>Queue every element in <paramref name="elements"/> in order. Chainable.</summary>
    public PdfPage AddBody(params Element[] elements)
    {
        foreach (var e in elements) _accumulatedBodies.Add(e);
        return this;
    }

    internal void FlushAccumulatedBodies()
    {
        if (_accumulatedBodies.Count > 0)
        {
            Body(_accumulatedBodies.ToArray());
            _accumulatedBodies.Clear();
        }
    }

    /// <summary>
    /// Render one or more <paramref name="pages"/> into this page's
    /// content stream, paginating across PDF pages as needed. The
    /// chainable <see cref="AddBody(Element)"/> form accumulates
    /// and forwards here when the configuring closure returns;
    /// <see cref="Body"/> can also be called directly with a prepared
    /// imperative element tree.
    ///
    /// <para>
    /// <b>Multi-element form.</b> Each entry in <paramref name="pages"/>
    /// becomes the body of one logical page. When an entry overflows its
    /// body slot (its <see cref="Layout.RenderResult.NextElement"/> is
    /// non-null), the continuation rolls onto a fresh PDF page before
    /// the next entry starts. When the entries cleanly exhaust the
    /// available height, the next entry starts on a fresh PDF page.
    /// </para>
    ///
    /// <para>
    /// <b>Header / Footer.</b> When <see cref="Header"/> or
    /// <see cref="Footer"/> has been set, every PDF page produced —
    /// including pages absorbing overflow continuations — gets the
    /// shared chrome, rebuilt fresh per page (so
    /// <see cref="Element.DeferredComponent"/> in the chrome captures
    /// per-page data).
    /// </para>
    ///
    /// <para>
    /// Returns the <i>last</i> page painted so the caller can keep
    /// drawing onto that page.
    /// </para>
    /// </summary>
    public PdfPage Body(params Element[] pages)
    {
        if (pages.Length == 0) return this;
        var composed = new HeaderFooterPage(_header, pages, _footer);
        return RenderTopLevel(composed);
    }

    private PdfPage RenderTopLevel(Element element)
    {
        var current = this;
        Element? toRender = element;
        while (toRender is not null)
        {
            var content = current.Content;
            var result = toRender.Render(content, content.Size);
            toRender = result.NextElement;
            if (toRender is not null)
                current = current.PageBreak();
        }

        return current;
    }

    /// <summary>
    /// Imperative page break — append a fresh page to the owning document
    /// with the same media-box and rotation as this one, and return it so
    /// the caller can continue writing into its (empty) content stream.
    /// The doc's default font (if any) is forwarded by
    /// <see cref="PdfDoc.AddPage"/>, so the new page emits the same
    /// <c>Tf</c> on its content stream as this one did — no manual
    /// re-setup required.
    /// <code>
    /// var page = doc.AddPage(PageSizes.A4);
    /// // … draw on page …
    /// page = page.PageBreak();
    /// // … draw on the new page …
    /// </code>
    /// </summary>
    public PdfPage PageBreak()
    {
        var next = _document.AddPage(_mediaBox);
        if (_rotation is { } r) next.Rotation = r;
        return next;
    }

    /// <summary>Set the page's content stream from raw operator bytes; legacy CSharpPdf-style direct-write setter.</summary>
    public void SetContent(string content)
    {
        var stream = MakeContentStream(System.Text.Encoding.Latin1.GetBytes(content));
        _dictionary.Add("Contents", _store.Add(stream));
    }

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
    /// Get the per-page resource name (e.g. <c>Fnt1</c>) for a font
    /// reference. If the reference is known to the document but hasn't been
    /// added to this page's resources yet, it's registered on demand.
    /// </summary>
    public string FontNameOf(PdfReference fontRef)
    {
        if (_fontNames.TryGetValue(fontRef, out var name)) return name;
        var resource = _document.FindFont(fontRef);
        if (resource is not null)
        {
            _resources.AddFont(resource.Name, resource.Reference);
            _fontNames[resource.Reference] = resource.Name;
            return resource.Name;
        }

        throw new InvalidOperationException(
            "Font reference is not known to the document. Use PdfPage.AddFont(name, reference) to register it under an explicit resource name.");
    }

    // Per-page lookup from gstate reference → resource name (mirrors fonts).
    private readonly Dictionary<PdfReference, string> _extGStateNamesByRef = new();

    /// <summary>
    /// Register <paramref name="gs"/> on this page (dedup by instance) and
    /// return the indirect reference to its dictionary. The resource name
    /// needed for the <c>gs</c> operator is recoverable via
    /// <see cref="ExtGStateNameOf"/>.
    /// </summary>
    public PdfReference UseExtGState(ExtGState gs)
    {
        if (!_extGStateNames.TryGetValue(gs, out var name))
        {
            name = $"GS{++_extGStateSeq}";
            var reference = _store.Add(gs.Dictionary);
            _resources.AddExtGState(name, reference);
            _extGStateNames[gs] = name;
            _extGStateRefs[gs] = reference;
            _extGStateNamesByRef[reference] = name;
        }

        return _extGStateRefs[gs];
    }

    /// <summary>Get the per-page resource name for an ExtGState reference — needed by content-stream emission for the <c>gs</c> argument.</summary>
    public string ExtGStateNameOf(PdfReference gsRef) =>
        _extGStateNamesByRef.TryGetValue(gsRef, out var name)
            ? name
            : throw new InvalidOperationException(
                "ExtGState reference is not registered on this page. Call PdfPage.UseExtGState first.");

    /// <summary>
    /// Wrap content-stream bytes in a <see cref="PdfStream"/>, applying
    /// FlateDecode when <see cref="CompressContentStreams"/> is on.
    /// </summary>
    public static PdfStream MakeContentStream(byte[] bytes)
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

    // ===== Legacy CSharpPdf-style resource registration ======================

    /// <summary>
    /// Register an XObject (image or form) in the page's resources under
    /// <paramref name="name"/>, paintable via the Do operator.
    /// </summary>
    public void AddXObject(string name, PdfReference xobject) =>
        _resources.AddXObject(name, xobject);

    /// <summary>Register a font in the page's resources under <paramref name="name"/>, selectable via Tf.</summary>
    public void AddFont(string name, PdfReference font)
    {
        _resources.AddFont(name, font);
        _fontNames[font] = name;
    }

    /// <summary>Register a shading in the page's resources, paintable via sh.</summary>
    public void AddShading(string name, PdfReference shading) =>
        _resources.AddShading(name, shading);

    /// <summary>Register a pattern in the page's resources, selectable via scn/SCN.</summary>
    public void AddPattern(string name, PdfReference pattern) =>
        _resources.AddPattern(name, pattern);

    /// <summary>Register a property list (e.g. OCG/OCMD) in the page's Properties resources.</summary>
    public void AddProperty(string name, PdfReference property) =>
        _resources.AddProperty(name, property);

    /// <summary>Register an ExtGState (graphic-state parameter dictionary) under <paramref name="name"/> directly.</summary>
    public void AddExtGState(string name, PdfDictionary graphicState)
    {
        graphicState.SetName("Type", "ExtGState");
        var reference = _store.Add(graphicState);
        _resources.AddExtGState(name, reference);
        _extGStateNamesByRef[reference] = name;
    }

    /// <summary>
    /// Append <paramref name="category"/> → <paramref name="name"/> → <paramref name="value"/>
    /// to the page's /Resources dictionary.
    /// </summary>
    public void AddResource(string category, string name, PdfObject value) =>
        _resources.Add(category, name, value);

    /// <summary>Append an annotation dictionary to /Annots; the /P link is set automatically.</summary>
    public PdfReference AddAnnotation(PdfDictionary annotation)
    {
        annotation.Add("P", Reference);
        var annotRef = _store.Add(annotation);
        if (_annotations is null)
        {
            _annotations = new PdfArray();
            _dictionary.Add("Annots", _annotations);
        }

        _annotations.Add(annotRef);
        return annotRef;
    }

    /// <summary>Add a Link annotation bound to <paramref name="action"/> (a raw action dictionary), with a suppressed border.</summary>
    public PdfReference AddLinkAnnotation(PdfRectangle rect, PdfDictionary action)
    {
        var link = new PdfDictionary();
        link.SetName("Type", "Annot");
        link.SetName("Subtype", "Link");
        link.Add("Rect", rect.ToArray());
        link.Add("Border", new PdfArray(new PdfNumber(0L), new PdfNumber(0L), new PdfNumber(0L)));
        link.Add("A", action);
        return AddAnnotation(link);
    }

    /// <summary>Add a Text ("sticky note") annotation with an associated Pop-up, taking the icon as a string.</summary>
    public void AddTextNote(PdfRectangle iconRect, string contents, string icon, PdfRectangle popupRect,
        bool open = true)
    {
        var note = new PdfDictionary();
        note.SetName("Type", "Annot");
        note.SetName("Subtype", "Text");
        note.Add("Rect", iconRect.ToArray());
        note.SetString("Contents", contents);
        note.SetName("Name", icon);
        var noteRef = AddAnnotation(note);

        var popup = new PdfDictionary();
        popup.SetName("Type", "Annot");
        popup.SetName("Subtype", "Popup");
        popup.Add("Rect", popupRect.ToArray());
        popup.Add("Parent", noteRef);
        popup.SetBoolean("Open", open);
        note.Add("Popup", AddAnnotation(popup));
    }

    public void AddTextNote(PdfRectangle iconRect, string contents, TextAnnotationIcon icon, PdfRectangle popupRect,
        bool open = true)
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