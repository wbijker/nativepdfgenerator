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

    public void Save(string path) => _store.Save(path);
    public void Save(Stream stream) => _store.Save(stream);
}
