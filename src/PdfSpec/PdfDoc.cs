using PdfSpec.Actions;
using PdfSpec.Elements;
using PdfSpec.Geometry;
using PdfSpec.Layers;
using PdfSpec.Objects;
using PdfSpec.Structure;
using PdfSpec.Fonts;

namespace PdfSpec;

/// <summary>
/// The high-level entry point for authoring a PDF. Manages the document
/// <see cref="Catalog"/> (ISO 32000-1 §7.7.2) and a flat page tree
/// (<see cref="PageTreeNode"/>; §7.7.3); document-level features
/// (Info, viewer preferences, name dictionary, OCGs, output intents, AcroForm)
/// are exposed as typed sub-objects.
/// </summary>
public sealed class PdfDoc
{
    private readonly PdfObjectStore _store = new();
    private readonly Catalog _catalog = new();
    private readonly PageTreeNode _pageTree = new();
    private readonly PdfReference _pageTreeRef;
    private readonly List<PdfPage> _pages = new();

    public PdfDoc()
    {
        var catalogRef = _store.Add(_catalog);
        _pageTreeRef = _store.Add(_pageTree);
        _store.Root = catalogRef;
        _catalog.Pages = _pageTreeRef;
    }

    /// <summary>The document catalog — caller-visible for advanced configuration (PageLayout, PageMode, viewer preferences, OpenAction).</summary>
    public Catalog Catalog => _catalog;

    public IReadOnlyList<PdfPage> Pages => _pages;

    // ----- Deferred components -----
    //
    // Two-phase content (page numbers / footers that reference the
    // final page count): Elements.DeferredComponent registers an entry
    // during its Render with the on-page rectangle it reserved; the
    // queue is drained by PrepareForSave once every page is laid out
    // and the page count is final.

    private readonly List<DeferredEntry> _deferred = new();

    private sealed record DeferredEntry(
        PdfPage Page,
        double X, double Y,
        double Width, double Height,
        Func<Layout.PageData, Element> Render);

    /// <summary>
    /// Register a deferred render callback. Called from
    /// <see cref="Element.DeferredComponent"/>; not intended as a
    /// public surface for end users — compose the deferred component
    /// instead.
    /// </summary>
    internal void RegisterDeferred(PdfPage page, double x, double y, double width, double height,
        Func<Layout.PageData, Element> render)
    {
        _deferred.Add(new DeferredEntry(page, x, y, width, height, render));
    }

    /// <summary>Low-level escape hatch: register an arbitrary indirect object on the underlying store.</summary>
    public PdfReference AddObject(PdfObject obj) => _store.Add(obj);

    /// <summary>The underlying object store. Exposed so consumers can pass it to <see cref="PdfNameTree.Build"/> and similar low-level builders.</summary>
    public PdfObjectStore Store => _store;

    // ----- Fonts (deduplicated, embedded at save) -----

    private readonly Dictionary<string, FontResource> _fonts = new();
    private int _fontSequence;

    /// <summary>
    /// Register <paramref name="font"/> on the document (deduped by
    /// <see cref="Font.Key"/>) and return its <see cref="FontResource"/>.
    /// The same font registered twice yields the same resource.
    /// </summary>
    public FontResource UseFont(Font font)
    {
        if (!_fonts.TryGetValue(font.Key, out var resource))
        {
            var dictionary = new PdfDictionary();
            var reference = _store.Add(dictionary);
            resource = new FontResource(font, $"Fnt{++_fontSequence}", dictionary, reference);
            _fonts[font.Key] = resource;
            _fontsByReference[reference] = resource;
        }
        return resource;
    }

    private readonly Dictionary<PdfReference, FontResource> _fontsByReference = new();

    /// <summary>Look up the doc-level <see cref="FontResource"/> for a previously-registered font reference, or <c>null</c> if not known.</summary>
    public FontResource? FindFont(PdfReference reference) =>
        _fontsByReference.TryGetValue(reference, out var r) ? r : null;

    // ----- Page tree -----

    /// <summary>Default media box on the page-tree root; pages added without their own MediaBox inherit it.</summary>
    public PdfRectangle? DefaultMediaBox
    {
        get => _pageTree.MediaBox;
        set => _pageTree.MediaBox = value;
    }

    private int _pagesPerLeaf = 10;
    private int _kidsPerNode = 10;

    /// <summary>
    /// How many leaf <c>/Page</c> kids each leaf <c>/Pages</c> node holds at
    /// save time (default 10). Lower values produce a deeper tree. Values
    /// below 1 are silently clamped to 1.
    /// </summary>
    public int PagesPerLeaf
    {
        get => _pagesPerLeaf;
        set => _pagesPerLeaf = Math.Max(1, value);
    }

    /// <summary>
    /// Maximum <c>/Kids</c> array length at every intermediate <c>/Pages</c>
    /// node, including the root (default 10). Values below 2 are silently
    /// clamped to 2 — otherwise the tree cannot reduce in width as it goes up.
    /// </summary>
    public int KidsPerNode
    {
        get => _kidsPerNode;
        set => _kidsPerNode = Math.Max(2, value);
    }

    /// <summary>Start a new document — equivalent to <c>new PdfDoc()</c>, named for fluent-style chaining (<c>PdfDoc.Create().Info(...).AddPage(...)</c>).</summary>
    public static PdfDoc Create() => new();

    /// <summary>Fluent setter for document-info fields — pass only the non-null arguments you want to set. Chainable.</summary>
    public PdfDoc Info(string? title = null, string? creator = null, string? producer = null,
        string? subject = null, string? author = null, string? keywords = null)
    {
        if (title is not null)    DocumentInfo.Title = title;
        if (creator is not null)  DocumentInfo.Creator = creator;
        if (producer is not null) DocumentInfo.Producer = producer;
        if (subject is not null)  DocumentInfo.Subject = subject;
        if (author is not null)   DocumentInfo.Author = author;
        if (keywords is not null) DocumentInfo.Keywords = keywords;
        return this;
    }

    /// <summary>Fluent alias for <see cref="SetDefaultFont"/>. Chainable.</summary>
    public PdfDoc DefaultFont(Font font, double size)
    {
        SetDefaultFont(font, size);
        return this;
    }

    /// <summary>Fluent alias for <see cref="DefaultMediaBox"/>. Chainable.</summary>
    public PdfDoc DefaultPageSize(PdfRectangle mediaBox)
    {
        DefaultMediaBox = mediaBox;
        return this;
    }

    /// <summary>Set the default media box from a <paramref name="width"/> × <paramref name="height"/> pair in points. Chainable.</summary>
    public PdfDoc DefaultPageSize(double width, double height) =>
        DefaultPageSize(new PdfRectangle(0, 0, width, height));

    /// <summary>Set the default media box from a <paramref name="width"/> × <paramref name="height"/> pair in <paramref name="unit"/>. Chainable.</summary>
    public PdfDoc DefaultPageSize(double width, double height, Unit unit) =>
        DefaultPageSize(
            new Length(width, unit).ToPoints(),
            new Length(height, unit).ToPoints());

    // ----- Doc-level chrome defaults inherited by every AddPage ---------------

    private Element? _defaultHeader;
    private Element? _defaultFooter;
    private double _defaultMarginPt;

    /// <summary>Set the default header rendered at the top of every page added with <see cref="AddPage(Action{PdfPage})"/> / <see cref="Content"/>. Per-page <see cref="PdfPage.Header(Element)"/> overrides it. Chainable.</summary>
    public PdfDoc Header(Element element)
    {
        _defaultHeader = element;
        return this;
    }

    /// <summary>Return an <see cref="IContainer"/> slot for the document-level default header — installs a fresh <see cref="BorderElement"/> as the default and returns a facade onto it. Chainable.</summary>
    public IContainer Header()
    {
        var border = new BorderElement();
        _defaultHeader = border;
        return new Container(border);
    }

    /// <summary>Set the default footer rendered at the bottom of every page. Chainable.</summary>
    public PdfDoc Footer(Element element)
    {
        _defaultFooter = element;
        return this;
    }

    /// <summary>Return an <see cref="IContainer"/> slot for the document-level default footer.</summary>
    public IContainer Footer()
    {
        var border = new BorderElement();
        _defaultFooter = border;
        return new Container(border);
    }

    /// <summary>Default body margin applied as padding around each <see cref="PdfPage.Body()"/> slot. Points. Chainable.</summary>
    public PdfDoc DefaultMargin(double points)
    {
        _defaultMarginPt = points;
        return this;
    }

    /// <summary>Default body margin in <paramref name="unit"/>. Chainable.</summary>
    public PdfDoc DefaultMargin(double value, Unit unit)
    {
        _defaultMarginPt = new Length(value, unit).ToPoints();
        return this;
    }

    /// <summary>Add a page whose body is populated by <paramref name="build"/>. Doc-level header / footer / margin defaults are applied automatically. Chainable.</summary>
    public PdfDoc Content(Action<IContainer> build) =>
        AddPage(p => build(p.Body()));

    /// <summary>
    /// Add a page with a single fluent <paramref name="body"/> and the
    /// (optional) shared <paramref name="header"/> / <paramref name="footer"/>
    /// chrome. The body paginates across overflow PDF pages, and the
    /// chrome rebuilds fresh on each one. Chainable.
    /// </summary>
    public PdfDoc AddPage(Element body, Element? header = null, Element? footer = null)
    {
        var page = AddPage();
        if (header is not null) page.Header(header);
        if (footer is not null) page.Footer(footer);
        page.Body(body);
        return this;
    }

    /// <summary>
    /// Add a page configured through a closure — set
    /// <see cref="PdfPage.Header(Element)"/> /
    /// <see cref="PdfPage.Footer(Element)"/>, add one or more
    /// bodies via <see cref="PdfPage.AddBody(Element)"/>. The
    /// accumulated bodies render once the closure returns. Chainable.
    /// </summary>
    public PdfDoc AddPage(Action<PdfPage> configure)
    {
        var page = AddPage();
        if (_defaultHeader is not null) page.Header(_defaultHeader);
        if (_defaultFooter is not null) page.Footer(_defaultFooter);
        if (_defaultMarginPt > 0)       page.SetDefaultMargin(_defaultMarginPt);
        configure(page);
        page.FlushAccumulatedBodies();
        return this;
    }

    /// <summary>
    /// Register a named destination resolving to <paramref name="pageIndex"/>
    /// (0-based) with the given <paramref name="fit"/> zoom mode (default
    /// <c>"Fit"</c>). Reference from anywhere in the document via
    /// <c>Navigation.PdfAction.GoToNamed(name)</c>. Chainable.
    /// </summary>
    public PdfDoc AddNamedDestination(string name, int pageIndex, string fit = "Fit")
    {
        AddNamedDestination(name, new PdfArray(_pages[pageIndex].Reference, new PdfName(fit)));
        return this;
    }

    /// <summary>
    /// Build an explicit destination array <c>[pageRef /<paramref name="fit"/>]</c>
    /// for <paramref name="pageIndex"/> (0-based). Pass directly to
    /// <c>Navigation.PdfAction.GoTo(...)</c> to wire a link target without
    /// indexing into <see cref="Pages"/> at the call site.
    /// </summary>
    public PdfArray PageDestination(int pageIndex, string fit = "Fit") =>
        new(_pages[pageIndex].Reference, new PdfName(fit));

    /// <summary>Add a page. When <paramref name="mediaBox"/> is null the page inherits its size from the page-tree root.</summary>
    public PdfPage AddPage(PdfRectangle? mediaBox = null)
    {
        var page = new PdfPage(this, _store);
        var reference = _store.Add(page);
        page.SetReference(reference);
        if (mediaBox is { } box) page.MediaBox = box;

        _pages.Add(page);

        // Forward the document-level default font (if any) onto this page —
        // emits a Tf to the page's content stream so every text block
        // inherits it via gstate.
        if (_defaultFont is not null) page.SetDefaultFont(_defaultFont, _defaultFontSize);

        return page;
    }

    // ----- Default font -----

    private Font? _defaultFont;
    private double _defaultFontSize;

    /// <summary>Currently-installed document-wide default font face (set via <see cref="SetDefaultFont"/> / <see cref="DefaultFont(Font, double)"/>). <c>null</c> until one is set.</summary>
    public Font? DefaultFontFace => _defaultFont;

    /// <summary>Currently-installed document-wide default font size in points.</summary>
    public double DefaultFontSize => _defaultFontSize;

    /// <summary>
    /// Set a document-wide default font + size. Each <see cref="Content.Text"/>
    /// block that doesn't call its own <c>SetFont</c> auto-emits <c>Tf</c>
    /// with this font at the start of <c>BT</c>, registering the font on
    /// the owning page. Per-page defaults (<see cref="PdfPage.SetDefaultFont"/>)
    /// take precedence.
    /// </summary>
    public void SetDefaultFont(Font font, double size)
    {
        _defaultFont = font;
        _defaultFontSize = size;
    }

    // ----- Document info -----

    private DocumentInfo? _info;

    /// <summary>Document information dictionary (title, author, subject, dates …). Lazily created on first access. For one-shot fluent setting see <see cref="Info(string?, string?, string?, string?, string?, string?)"/>.</summary>
    public DocumentInfo DocumentInfo
    {
        get
        {
            if (_info is null)
            {
                var now = DateTimeOffset.Now;
                _info = new DocumentInfo
                {
                    CreationDate = now,
                    ModDate = now,
                };
                _store.Info = _store.Add(_info);
            }
            return _info;
        }
    }

    // ----- Name dictionary -----

    private PdfNameTree? _namedDestinations;

    public void AddNamedDestination(string name, Destination destination)
    {
        _namedDestinations ??= new PdfNameTree();
        _namedDestinations.Add(name, destination.Build());
    }

    /// <summary>Legacy overload that accepts a pre-built explicit-destination array.</summary>
    public void AddNamedDestination(string name, PdfArray destinationArray)
    {
        _namedDestinations ??= new PdfNameTree();
        _namedDestinations.Add(name, destinationArray);
    }

    // ----- Legacy CSharpPdf-style document-level setters ---------------------

    /// <summary>Set the catalog /PageLayout entry from the spec name string (e.g. "SinglePage").</summary>
    public void SetPageLayout(string layout) =>
        _catalog.PageLayout = Enum.Parse<PageLayout>(layout);

    /// <summary>Set the catalog /PageMode entry from the spec name string (e.g. "UseOutlines").</summary>
    public void SetPageMode(string mode) =>
        _catalog.PageMode = Enum.Parse<PageMode>(mode);

    /// <summary>Toggle the viewer preference that shows the document title (from metadata) instead of the filename.</summary>
    public void SetDisplayDocTitle(bool value) => _catalog.ViewerPreferences.DisplayDocTitle = value;

    /// <summary>Set a default page size on the page-tree root (pages added without their own MediaBox inherit it).</summary>
    public void SetDefaultMediaBox(PdfRectangle box) => DefaultMediaBox = box;

    /// <summary>Populate the /Info dictionary with the given metadata fields.</summary>
    public void SetDocumentInfo(
        string? title = null, string? author = null, string? subject = null,
        string? keywords = null, string? creator = null, string? producer = null,
        DateTimeOffset? created = null, DateTimeOffset? modified = null)
    {
        var info = DocumentInfo;
        info.Title = title;
        info.Author = author;
        info.Subject = subject;
        info.Keywords = keywords;
        info.Creator = creator;
        info.Producer = producer;
        DateTimeOffset createdAt = created ?? DateTimeOffset.Now;
        info.CreationDate = createdAt;
        info.ModDate = modified ?? createdAt;
    }

    /// <summary>Register a name tree under the document <c>/Names</c> dictionary.</summary>
    public void SetNameTree(string category, PdfObject nameTreeRoot) =>
        _catalog.SetNameTree(category, nameTreeRoot);

    /// <summary>Add an output intent with raw subtype name (e.g. <c>"GTS_PDFA1"</c>).</summary>
    public void AddOutputIntent(string subtype, string outputConditionIdentifier,
        string? info = null, PdfReference? destOutputProfile = null)
    {
        var intent = new PdfDictionary();
        intent.SetName("Type", "OutputIntent");
        intent.SetName("S", subtype);
        intent.SetString("OutputConditionIdentifier", outputConditionIdentifier);
        if (info is not null) intent.SetString("Info", info);
        if (destOutputProfile is not null) intent.Add("DestOutputProfile", destOutputProfile);
        _catalog.AddOutputIntent(_store.Add(intent));
    }

    /// <summary>Set the OpenAction triggered when the document is opened.</summary>
    public void SetOpenAction(PdfObject actionOrDestination) => _catalog.OpenAction = actionOrDestination;

    /// <summary>Set the catalog Collection dictionary (portfolio).</summary>
    public void SetCollection(PdfDictionary collection) =>
        _catalog.Collection = _store.Add(collection);

    /// <summary>Set the catalog structure-tree root (also marks the document as tagged via MarkInfo).</summary>
    public void SetStructTreeRoot(PdfReference structTreeRoot) => _catalog.StructTreeRoot = structTreeRoot;

    /// <summary>Create an optional content group and register it.</summary>
    public PdfReference AddOptionalContentGroup(string name, string? intent = null)
    {
        OptionalContentIntent? intentEnum = intent is null ? null : Enum.Parse<OptionalContentIntent>(intent);
        return AddOptionalContentGroup(new OptionalContentGroup(name, intentEnum));
    }

    // ----- Embedded files (CSharpPdf-only feature) ---------------------------

    private PdfNameTree? _embeddedFiles;

    /// <summary>Embed a file and register it in the EmbeddedFiles name tree, returning the file-spec reference.</summary>
    public PdfReference AddEmbeddedFile(string name, string fileName, byte[] data, string mimeType, string? description = null)
    {
        var streamRef = _store.Add(Files.EmbeddedFile.Stream(data, mimeType));
        var specRef = _store.Add(Files.EmbeddedFile.FileSpec(fileName, streamRef, description));
        RegisterEmbeddedFile(name, specRef);
        return specRef;
    }

    /// <summary>Register an existing file specification reference in the EmbeddedFiles name tree.</summary>
    public void RegisterEmbeddedFile(string name, PdfReference fileSpec)
    {
        _embeddedFiles ??= new PdfNameTree();
        _embeddedFiles.Add(name, fileSpec);
    }

    // ----- Outlines (CSharpPdf-only feature) ---------------------------------

    /// <summary>
    /// Build the document outline (bookmark tree) from a list of top-level
    /// items, wire First/Last/Next/Prev/Parent + signed Count entries, and
    /// set it as the catalog's <c>/Outlines</c>.
    /// </summary>
    public void SetOutline(IReadOnlyList<Navigation.PdfOutlineItem> topLevel)
    {
        if (topLevel.Count == 0) return;

        var root = new PdfDictionary();
        root.SetName("Type", "Outlines");
        var rootRef = _store.Add(root);
        BuildOutlineLevel(topLevel, rootRef, root);
        root.SetInteger("Count", VisibleCount(topLevel));
        _catalog.Outlines = rootRef;
    }

    private void BuildOutlineLevel(
        IReadOnlyList<Navigation.PdfOutlineItem> items, PdfReference parentRef, PdfDictionary parentDict)
    {
        var dicts = new PdfDictionary[items.Count];
        var refs = new PdfReference[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            dicts[i] = new PdfDictionary();
            refs[i] = _store.Add(dicts[i]);
        }

        parentDict.Add("First", refs[0]);
        parentDict.Add("Last", refs[^1]);

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var dict = dicts[i];
            dict.SetString("Title", item.Title);
            dict.Add("Parent", parentRef);
            if (i > 0) dict.Add("Prev", refs[i - 1]);
            if (i < items.Count - 1) dict.Add("Next", refs[i + 1]);
            if (item.Destination is not null) dict.Add("Dest", item.Destination);
            else if (item.Action is not null) dict.Add("A", item.Action);
            if (item.Children.Count > 0)
            {
                int magnitude = VisibleCount(item.Children);
                dict.SetInteger("Count", item.Open ? magnitude : -magnitude);
                BuildOutlineLevel(item.Children, refs[i], dict);
            }
        }
    }

    private static int VisibleCount(IReadOnlyList<Navigation.PdfOutlineItem> items)
    {
        int count = 0;
        foreach (var item in items)
        {
            count += 1;
            if (item.Open && item.Children.Count > 0)
            {
                count += VisibleCount(item.Children);
            }
        }
        return count;
    }

    // ----- Metadata -----

    public void SetXmpMetadata(string xmp)
    {
        var stream = new PdfStream(System.Text.Encoding.UTF8.GetBytes(xmp));
        stream.Dictionary.SetName("Type", "Metadata");
        stream.Dictionary.SetName("Subtype", "XML");
        _catalog.Metadata = _store.Add(stream);
    }

    /// <summary>Set the document's XMP metadata stream from a typed <see cref="XmpMetadata"/> builder.</summary>
    public void SetXmpMetadata(XmpMetadata metadata) => SetXmpMetadata(metadata.Build());

    // ----- Output intents -----

    public void AddOutputIntent(OutputIntent intent) =>
        _catalog.AddOutputIntent(_store.Add(intent.Dictionary));

    // ----- Optional content (layers) -----

    private PdfArray? _ocgList;
    private PdfDictionary? _ocConfig;

    public PdfReference AddOptionalContentGroup(OptionalContentGroup ocg)
    {
        EnsureOcProperties();
        var reference = _store.Add(ocg.Dictionary);
        _ocgList!.Add(reference);
        return reference;
    }

    public PdfDictionary OptionalContentConfig
    {
        get
        {
            EnsureOcProperties();
            return _ocConfig!;
        }
    }

    private void EnsureOcProperties()
    {
        if (_ocConfig is null)
        {
            _ocgList = new PdfArray();
            _ocConfig = new PdfDictionary();
            _ocConfig.SetString("Name", "Default");
            _ocConfig.SetName("BaseState", "ON");

            var ocProps = new PdfDictionary();
            ocProps.Add("OCGs", _ocgList);
            ocProps.Add("D", _ocConfig);
            _catalog.OCProperties = ocProps;
        }
    }

    // ----- Interactive forms (AcroForm) -----

    private PdfDictionary? _acroForm;
    private PdfArray? _formFields;

    public PdfDictionary AcroForm
    {
        get
        {
            if (_acroForm is null)
            {
                _formFields = new PdfArray();
                _acroForm = new PdfDictionary
                {
                    { "Fields", _formFields },
                };
                _catalog.AcroForm = _store.Add(_acroForm);
            }
            return _acroForm;
        }
    }

    public void RegisterFormField(PdfReference field)
    {
        _ = AcroForm;
        _formFields!.Add(field);
    }

    public void Save(string path)
    {
        PrepareForSave();
        _store.Save(path);
    }

    public void Save(Stream stream)
    {
        PrepareForSave();
        _store.Save(stream);
    }

    private void PrepareForSave()
    {
        BuildPageTree();
        if (_namedDestinations is not null)
        {
            _catalog.SetNameTree("Dests", _namedDestinations.Build(_store));
        }
        if (_embeddedFiles is not null)
        {
            _catalog.SetNameTree("EmbeddedFiles", _embeddedFiles.Build(_store));
        }
        foreach (var registration in _fonts.Values)
        {
            registration.Font.Build(_store, registration.Dictionary);
        }

        // Drain the deferred queue before content gets flushed: every
        // DeferredComponent reserved a sub-rectangle during its first-
        // phase Render, and now that pages are all laid out we have
        // the final page count + page index for each entry. We render
        // each callback's element into a fresh sub-stream rooted at
        // the recorded coords and flush it onto the owning page's
        // content. The append-only nature of content streams means
        // deferred content paints on top of whatever was there.
        if (_deferred.Count > 0)
        {
            int totalPages = _pages.Count;
            foreach (var entry in _deferred)
            {
                int pageNumber = _pages.IndexOf(entry.Page) + 1;
                if (pageNumber == 0) continue; // page removed / never added — skip safely
                var data = new Layout.PageData(pageNumber, totalPages);
                var element = entry.Render(data);

                var sub = entry.Page.Content.CreateSubStream(entry.X, entry.Y, entry.Width, entry.Height);
                element.Render(sub, new Layout.PdfSize(entry.Width, entry.Height));
                sub.Build();
            }
        }

        foreach (var page in _pages)
        {
            page.FlushContent();
        }
    }

    /// <summary>
    /// Build a balanced page tree bottom-up. Leaves carry up to
    /// <see cref="PagesPerLeaf"/> <c>/Page</c> kids each; every intermediate
    /// node (including the root) carries up to <see cref="KidsPerNode"/>
    /// kids. When the document is small enough to fit in a single leaf
    /// (<c>n ≤ PagesPerLeaf</c>) the root doubles as the only leaf.
    /// </summary>
    private void BuildPageTree()
    {
        int n = _pages.Count;

        if (n <= _pagesPerLeaf)
        {
            var pageRefs = new List<PdfReference>(n);
            foreach (var page in _pages)
            {
                pageRefs.Add(page.Reference);
                page.SetParent(_pageTreeRef);
            }
            _pageTree.SetKidsAndCount(pageRefs, n);
            return;
        }

        // Leaf level: chunk pages into PagesPerLeaf-sized leaves.
        var level = new List<(PdfReference reference, PageTreeNode node, int count)>();
        for (int start = 0; start < n; start += _pagesPerLeaf)
        {
            int len = Math.Min(_pagesPerLeaf, n - start);
            var leaf = new PageTreeNode();
            var leafRef = _store.Add(leaf);
            var kids = new List<PdfReference>(len);
            for (int i = 0; i < len; i++)
            {
                var page = _pages[start + i];
                kids.Add(page.Reference);
                page.SetParent(leafRef);
            }
            leaf.SetKidsAndCount(kids, len);
            level.Add((leafRef, leaf, len));
        }

        // Intermediate levels: KidsPerNode fan-out per node, bottom-up,
        // until the top level fits in one /Kids array.
        while (level.Count > _kidsPerNode)
        {
            var next = new List<(PdfReference reference, PageTreeNode node, int count)>();
            for (int start = 0; start < level.Count; start += _kidsPerNode)
            {
                int len = Math.Min(_kidsPerNode, level.Count - start);
                var node = new PageTreeNode();
                var nodeRef = _store.Add(node);
                var kids = new List<PdfReference>(len);
                int total = 0;
                for (int i = 0; i < len; i++)
                {
                    var child = level[start + i];
                    kids.Add(child.reference);
                    child.node.Parent = nodeRef;
                    total += child.count;
                }
                node.SetKidsAndCount(kids, total);
                next.Add((nodeRef, node, total));
            }
            level = next;
        }

        // Populate the pre-reserved root with the top level.
        var rootKids = new List<PdfReference>(level.Count);
        int rootTotal = 0;
        foreach (var item in level)
        {
            rootKids.Add(item.reference);
            item.node.Parent = _pageTreeRef;
            rootTotal += item.count;
        }
        _pageTree.SetKidsAndCount(rootKids, rootTotal);
    }
}
