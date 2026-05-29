using CSharpPdf.Geometry;
using CSharpPdf.Objects;

namespace CSharpPdf;

/// <summary>
/// The high-level entry point for authoring a PDF. Manages the document catalog
/// and a (flat) page tree, exposing the document-structure concepts from
/// Chapter 1: the catalog dictionary, the page tree with attribute inheritance,
/// and the name dictionary.
/// </summary>
public sealed class PdfDocument
{
    private readonly PdfObjectStore _store = new();
    private readonly PdfDictionary _catalog = new();
    private readonly PdfDictionary _pageTreeRoot = new();
    private readonly PdfArray _kids = new();
    private readonly PdfReference _pageTreeRef;
    private readonly List<PdfPage> _pages = new();

    public PdfDocument()
    {
        var catalogRef = _store.Add(_catalog);
        _pageTreeRef = _store.Add(_pageTreeRoot);
        _store.Root = catalogRef;

        _catalog["Type"] = new PdfName("Catalog");
        _catalog["Pages"] = _pageTreeRef;

        _pageTreeRoot["Type"] = new PdfName("Pages");
        _pageTreeRoot["Kids"] = _kids;
        _pageTreeRoot["Count"] = new PdfNumber(0L);
    }

    public IReadOnlyList<PdfPage> Pages => _pages;

    /// <summary>Register an arbitrary indirect object (for advanced/low-level use).</summary>
    public PdfReference AddObject(PdfObject obj) => _store.Add(obj);

    // ----- Catalog options -----

    /// <summary>How the viewer lays out pages: SinglePage, OneColumn, TwoPageLeft, ...</summary>
    public void SetPageLayout(string layout) => _catalog["PageLayout"] = new PdfName(layout);

    /// <summary>Navigational chrome to show: UseNone, UseOutlines, UseThumbs, ...</summary>
    public void SetPageMode(string mode) => _catalog["PageMode"] = new PdfName(mode);

    /// <summary>When true, viewers show the document title (from metadata) instead of the filename.</summary>
    public void SetDisplayDocTitle(bool value)
    {
        if (_catalog.Get("ViewerPreferences") is not PdfDictionary prefs)
        {
            prefs = new PdfDictionary();
            _catalog["ViewerPreferences"] = prefs;
        }
        prefs["DisplayDocTitle"] = new PdfBoolean(value);
    }

    // ----- Page tree -----

    /// <summary>
    /// Set a default page size on the page-tree root. Pages added without their
    /// own MediaBox inherit this value (Chapter 1, "Inheritance").
    /// </summary>
    public void SetDefaultMediaBox(PdfRectangle box) => _pageTreeRoot["MediaBox"] = box.ToArray();

    /// <summary>
    /// Add a page. When <paramref name="mediaBox"/> is null the page inherits its
    /// size from the page-tree root (see <see cref="SetDefaultMediaBox"/>).
    /// </summary>
    public PdfPage AddPage(PdfRectangle? mediaBox = null)
    {
        var dictionary = new PdfDictionary();
        var reference = _store.Add(dictionary);

        dictionary["Type"] = new PdfName("Page");
        dictionary["Parent"] = _pageTreeRef;
        if (mediaBox is { } box)
        {
            dictionary["MediaBox"] = box.ToArray();
        }

        var page = new PdfPage(_store, dictionary, reference);
        _pages.Add(page);
        _kids.Add(reference);
        _pageTreeRoot["Count"] = new PdfNumber((long)_pages.Count);
        return page;
    }

    // ----- Name dictionary -----

    /// <summary>
    /// Register a name tree under the document name dictionary, e.g.
    /// <c>SetNameTree("Dests", root)</c> for named destinations.
    /// </summary>
    public void SetNameTree(string category, PdfObject nameTreeRoot)
    {
        if (_catalog.Get("Names") is not PdfDictionary names)
        {
            names = new PdfDictionary();
            _catalog["Names"] = names;
        }
        names[category] = nameTreeRoot;
    }

    private PdfNameTree? _namedDestinations;

    /// <summary>
    /// Register a named destination (Chapter 5, "Named Destinations"): a string
    /// name mapped to an explicit destination, resolvable from this or other PDFs.
    /// </summary>
    public void AddNamedDestination(string name, PdfArray destination)
    {
        _namedDestinations ??= new PdfNameTree();
        _namedDestinations.Add(name, destination);
    }

    // ----- Embedded files -----

    private PdfNameTree? _embeddedFiles;

    /// <summary>
    /// Embed a file in the document and register it in the EmbeddedFiles name tree
    /// (Chapter 8), so it is associated with the document as a whole. Returns the
    /// file specification reference (e.g. for a GoToE action or collection item).
    /// </summary>
    public PdfReference AddEmbeddedFile(string name, string fileName, byte[] data, string mimeType, string? description = null)
    {
        var streamRef = _store.Add(Files.EmbeddedFile.Stream(data, mimeType));
        var specRef = _store.Add(Files.EmbeddedFile.FileSpec(fileName, streamRef, description));
        RegisterEmbeddedFile(name, specRef);
        return specRef;
    }

    /// <summary>Add an existing file specification to the EmbeddedFiles name tree.</summary>
    public void RegisterEmbeddedFile(string name, PdfReference fileSpec)
    {
        _embeddedFiles ??= new PdfNameTree();
        _embeddedFiles.Add(name, fileSpec);
    }

    /// <summary>Set the catalog Collection dictionary to present embedded files as a portfolio.</summary>
    public void SetCollection(PdfDictionary collection) =>
        _catalog["Collection"] = _store.Add(collection);

    // ----- Actions -----

    /// <summary>
    /// Set the document OpenAction, run when the PDF is opened — either an action
    /// dictionary or an explicit destination array (Chapter 5).
    /// </summary>
    public void SetOpenAction(PdfObject actionOrDestination) =>
        _catalog["OpenAction"] = actionOrDestination;

    // ----- Metadata -----

    /// <summary>
    /// Set the document information dictionary (Chapter 12), referenced from the
    /// trailer's Info key. Only non-null fields are written.
    /// </summary>
    public void SetDocumentInfo(
        string? title = null, string? author = null, string? subject = null,
        string? keywords = null, string? creator = null, string? producer = null,
        DateTimeOffset? created = null, DateTimeOffset? modified = null)
    {
        var info = new PdfDictionary();
        void Set(string key, string? value)
        {
            if (value is not null)
            {
                info[key] = new PdfString(value);
            }
        }
        Set("Title", title);
        Set("Author", author);
        Set("Subject", subject);
        Set("Keywords", keywords);
        Set("Creator", creator);
        Set("Producer", producer);

        DateTimeOffset createdAt = created ?? DateTimeOffset.Now;
        info["CreationDate"] = new PdfString(Files.EmbeddedFile.PdfDate(createdAt));
        info["ModDate"] = new PdfString(Files.EmbeddedFile.PdfDate(modified ?? createdAt));

        _store.Info = _store.Add(info);
    }

    /// <summary>Set the document's XMP metadata stream (catalog Metadata), stored as plain-text XML.</summary>
    public void SetXmpMetadata(string xmp)
    {
        var stream = new PdfStream(System.Text.Encoding.UTF8.GetBytes(xmp));
        stream.Dictionary["Type"] = new PdfName("Metadata");
        stream.Dictionary["Subtype"] = new PdfName("XML");
        _catalog["Metadata"] = _store.Add(stream);
    }

    // ----- Logical structure / tagging -----

    /// <summary>
    /// Set the structure tree root and mark the document as tagged (Chapter 11)
    /// by adding MarkInfo with Marked true.
    /// </summary>
    public void SetStructTreeRoot(PdfReference structTreeRoot)
    {
        _catalog["StructTreeRoot"] = structTreeRoot;
        _catalog["MarkInfo"] = new PdfDictionary { ["Marked"] = new PdfBoolean(true) };
    }

    // ----- Optional content (layers) -----

    private PdfArray? _ocgList;
    private PdfDictionary? _ocConfig;

    /// <summary>
    /// Create an optional content group (layer) and register it in the catalog's
    /// OCProperties (Chapter 10). Mark content with it via the page Properties
    /// resource + BDC /OC, or via an XObject/annotation OC key.
    /// </summary>
    public PdfReference AddOptionalContentGroup(string name, string? intent = null)
    {
        EnsureOcProperties();
        var ocg = new PdfDictionary { ["Type"] = new PdfName("OCG"), ["Name"] = new PdfString(name) };
        if (intent is not null)
        {
            ocg["Intent"] = new PdfName(intent);
        }
        var reference = _store.Add(ocg);
        _ocgList!.Add(reference);
        return reference;
    }

    /// <summary>The default optional content configuration dictionary (D), for
    /// setting Order, ON, OFF, RBGroups, AS, etc.</summary>
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
            _ocConfig = new PdfDictionary { ["Name"] = new PdfString("Default"), ["BaseState"] = new PdfName("ON") };
            _catalog["OCProperties"] = new PdfDictionary { ["OCGs"] = _ocgList, ["D"] = _ocConfig };
        }
    }

    // ----- Outlines (bookmarks) -----

    /// <summary>
    /// Build the document outline (bookmark tree) from a list of top-level items,
    /// wiring the First/Last/Next/Prev/Parent links and the signed Count entries,
    /// and set it as the catalog's Outlines.
    /// </summary>
    public void SetOutline(IReadOnlyList<Navigation.PdfOutlineItem> topLevel)
    {
        if (topLevel.Count == 0)
        {
            return;
        }

        var root = new PdfDictionary { ["Type"] = new PdfName("Outlines") };
        var rootRef = _store.Add(root);
        BuildOutlineLevel(topLevel, rootRef, root);
        root["Count"] = new PdfNumber((long)VisibleCount(topLevel));
        _catalog["Outlines"] = rootRef;
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

        parentDict["First"] = refs[0];
        parentDict["Last"] = refs[^1];

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var dict = dicts[i];
            dict["Title"] = new PdfString(item.Title);
            dict["Parent"] = parentRef;
            if (i > 0)
            {
                dict["Prev"] = refs[i - 1];
            }
            if (i < items.Count - 1)
            {
                dict["Next"] = refs[i + 1];
            }
            if (item.Destination is not null)
            {
                dict["Dest"] = item.Destination;
            }
            else if (item.Action is not null)
            {
                dict["A"] = item.Action;
            }
            if (item.Children.Count > 0)
            {
                int magnitude = VisibleCount(item.Children);
                dict["Count"] = new PdfNumber((long)(item.Open ? magnitude : -magnitude));
                BuildOutlineLevel(item.Children, refs[i], dict);
            }
        }
    }

    // Count of items visible when their parents are open (children of a closed
    // item are not counted), summed across all levels.
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

    // ----- Interactive forms (AcroForm) -----

    private PdfDictionary? _acroForm;
    private PdfArray? _formFields;

    /// <summary>
    /// Get (creating on first use) the interactive form dictionary, referenced by
    /// the catalog's AcroForm key (Chapter 7). Exposes Fields and lets callers set
    /// shared defaults such as DR (default resources) and DA (default appearance).
    /// </summary>
    public PdfDictionary AcroForm
    {
        get
        {
            if (_acroForm is null)
            {
                _acroForm = new PdfDictionary();
                _formFields = new PdfArray();
                _acroForm["Fields"] = _formFields;
                _catalog["AcroForm"] = _store.Add(_acroForm);
            }
            return _acroForm;
        }
    }

    /// <summary>Append a top-level field reference to the AcroForm Fields array.</summary>
    public void RegisterFormField(PdfReference field)
    {
        _ = AcroForm;
        _formFields!.Add(field);
    }

    public void Save(string path)
    {
        Finalize();
        _store.Save(path);
    }

    public void Save(Stream stream)
    {
        Finalize();
        _store.Save(stream);
    }

    private void Finalize()
    {
        if (_namedDestinations is not null)
        {
            SetNameTree("Dests", _namedDestinations.Build());
        }
        if (_embeddedFiles is not null)
        {
            SetNameTree("EmbeddedFiles", _embeddedFiles.Build());
        }
        foreach (var page in _pages)
        {
            page.FlushContent();
        }
    }
}
