using PdfSpec.Actions;
using PdfSpec.Geometry;
using PdfSpec.Layers;
using PdfSpec.Objects;
using PdfSpec.Structure;
using PdfSpec.Text;

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

    /// <summary>Low-level escape hatch: register an arbitrary indirect object on the underlying store.</summary>
    internal PdfReference AddObject(PdfObject obj) => _store.Add(obj);

    // ----- Fonts (deduplicated, embedded at save) -----

    private readonly Dictionary<string, (Font Font, string Name, PdfDictionary Dictionary, PdfReference Reference)> _fonts = new();
    private int _fontSequence;

    internal (string Name, PdfReference Reference) UseFont(Font font)
    {
        if (!_fonts.TryGetValue(font.Key, out var registration))
        {
            var dictionary = new PdfDictionary();
            var reference = _store.Add(dictionary);
            registration = (font, $"Fnt{++_fontSequence}", dictionary, reference);
            _fonts[font.Key] = registration;
        }
        return (registration.Name, registration.Reference);
    }

    // ----- Page tree -----

    /// <summary>Default media box on the page-tree root; pages added without their own MediaBox inherit it.</summary>
    public PdfRectangle? DefaultMediaBox
    {
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

    /// <summary>Add a page. When <paramref name="mediaBox"/> is null the page inherits its size from the page-tree root.</summary>
    public PdfPage AddPage(PdfRectangle? mediaBox = null)
    {
        var page = new PdfPage(this, _store);
        var reference = _store.Add(page);
        page.SetReference(reference);
        if (mediaBox is { } box) page.MediaBox = box;

        _pages.Add(page);
        return page;
    }

    // ----- Document info -----

    private DocumentInfo? _info;

    /// <summary>Document information dictionary (title, author, subject, dates …). Lazily created on first access.</summary>
    public DocumentInfo Info
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

    // ----- Metadata -----

    public void SetXmpMetadata(string xmp)
    {
        var stream = new PdfStream(System.Text.Encoding.UTF8.GetBytes(xmp));
        stream.Dictionary.SetName("Type", "Metadata");
        stream.Dictionary.SetName("Subtype", "XML");
        _catalog.Metadata = _store.Add(stream);
    }

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
        foreach (var registration in _fonts.Values)
        {
            registration.Font.Build(_store, registration.Dictionary);
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
