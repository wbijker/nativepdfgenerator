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
/// (<see cref="PageTreeNode"/>; §7.7.3); the document-level features
/// (Info, viewer preferences, name dictionary, OCGs, output intents, AcroForm)
/// are exposed as typed sub-objects, so the caller never touches a raw
/// <see cref="PdfDictionary"/>.
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
        var catalogRef = _store.Add(_catalog.Dictionary);
        _pageTreeRef = _store.Add(_pageTree.Dictionary);
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

    /// <summary>
    /// Register a font for use, deduplicated by its key. On first use a font
    /// dictionary is reserved; it is populated at save time. Returns the
    /// resource name and reference to place in a page's font resources.
    /// </summary>
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

    /// <summary>Add a page. When <paramref name="mediaBox"/> is null the page inherits its size from the page-tree root.</summary>
    public PdfPage AddPage(PdfRectangle? mediaBox = null)
    {
        var page = new PdfPage(this, _store, _pageTreeRef);
        var reference = _store.Add(page.Dictionary);
        page.SetReference(reference);
        if (mediaBox is { } box) page.MediaBox = box;

        _pages.Add(page);
        _pageTree.AddKid(reference);
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
                _info = new DocumentInfo();
                _info.CreationDate = DateTimeOffset.Now;
                _info.ModDate = _info.CreationDate;
                _store.Info = _store.Add(_info.Dictionary);
            }
            return _info;
        }
    }

    // ----- Name dictionary -----

    private PdfNameTree? _namedDestinations;

    /// <summary>
    /// Register a named destination (ISO 32000-1 §12.3.2.3): a string name
    /// mapped to an explicit <see cref="Destination"/>, resolvable from this
    /// or other PDFs (via <see cref="NamedDestinationAction"/> or a remote
    /// GoTo). Use the static factories on <see cref="Destination"/>.
    /// </summary>
    public void AddNamedDestination(string name, Destination destination)
    {
        _namedDestinations ??= new PdfNameTree();
        _namedDestinations.Add(name, destination.Build());
    }

    // ----- Metadata -----

    /// <summary>Set the document's XMP metadata stream (catalog Metadata), stored as plain-text XML.</summary>
    public void SetXmpMetadata(string xmp)
    {
        var stream = new PdfStream(System.Text.Encoding.UTF8.GetBytes(xmp));
        stream.Dictionary["Type"] = new PdfName("Metadata");
        stream.Dictionary["Subtype"] = new PdfName("XML");
        _catalog.Metadata = _store.Add(stream);
    }

    // ----- Output intents -----

    /// <summary>Add an output intent describing the target colour space (Chapter 13).</summary>
    public void AddOutputIntent(OutputIntent intent) =>
        _catalog.AddOutputIntent(_store.Add(intent.Build()));

    // ----- Optional content (layers) -----

    private PdfArray? _ocgList;
    private PdfDictionary? _ocConfig;

    /// <summary>Register an Optional Content Group and return its reference.</summary>
    public PdfReference AddOptionalContentGroup(OptionalContentGroup ocg)
    {
        EnsureOcProperties();
        var reference = _store.Add(ocg.Build());
        _ocgList!.Add(reference);
        return reference;
    }

    /// <summary>The default optional-content configuration dictionary (D) — set Order, ON, OFF, RBGroups, AS, etc. on it.</summary>
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
            _ocConfig = new PdfDictionary
            {
                ["Name"] = new PdfString("Default"),
                ["BaseState"] = new PdfName("ON"),
            };
            _catalog.OCProperties = new PdfDictionary { ["OCGs"] = _ocgList, ["D"] = _ocConfig };
        }
    }

    // ----- Interactive forms (AcroForm) -----

    private PdfDictionary? _acroForm;
    private PdfArray? _formFields;

    /// <summary>The interactive-form dictionary, lazily created on first access.</summary>
    public PdfDictionary AcroForm
    {
        get
        {
            if (_acroForm is null)
            {
                _acroForm = new PdfDictionary();
                _formFields = new PdfArray();
                _acroForm["Fields"] = _formFields;
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
}
