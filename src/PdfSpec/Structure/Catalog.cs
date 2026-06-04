using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// The document catalog (ISO 32000-1 §7.7.2) — the root object of the
/// document, referenced from the trailer's <c>/Root</c>. Holds typed fields
/// for the catalog-level features and emits the <c>/Catalog</c> dictionary
/// fresh at write time.
/// </summary>
public sealed class Catalog : PdfObject
{
    private PdfReference? _pages;
    private ViewerPreferences? _viewerPreferences;
    private List<KeyValuePair<string, PdfObject>>? _names;
    private List<PdfReference>? _outputIntents;

    /// <summary>Indirect reference to the root <c>/Pages</c> page-tree node.</summary>
    public PdfReference Pages { set => _pages = value; }

    /// <summary>How the viewer lays out pages: SinglePage, OneColumn, TwoPageLeft, ...</summary>
    public string? PageLayout { get; set; }

    /// <summary>Navigational chrome to show: UseNone, UseOutlines, UseThumbs, ...</summary>
    public string? PageMode { get; set; }

    /// <summary>The viewer-preferences sub-object, lazily created.</summary>
    public ViewerPreferences ViewerPreferences => _viewerPreferences ??= new ViewerPreferences();

    /// <summary>
    /// Register a name tree under the document's <c>/Names</c> dictionary,
    /// e.g. <c>SetNameTree("Dests", root)</c> for named destinations.
    /// Re-setting the same category replaces the previous value.
    /// </summary>
    public void SetNameTree(string category, PdfObject nameTreeRoot)
    {
        _names ??= new List<KeyValuePair<string, PdfObject>>();
        for (int i = 0; i < _names.Count; i++)
        {
            if (_names[i].Key == category)
            {
                _names[i] = new KeyValuePair<string, PdfObject>(category, nameTreeRoot);
                return;
            }
        }
        _names.Add(new KeyValuePair<string, PdfObject>(category, nameTreeRoot));
    }

    /// <summary>Action or destination triggered when the document is opened.</summary>
    public PdfObject? OpenAction { get; set; }

    /// <summary>Reference to the root of the structure tree (Chapter 11).</summary>
    public PdfReference? StructTreeRoot { get; set; }

    /// <summary>The AcroForm dictionary reference (Chapter 7).</summary>
    public PdfReference? AcroForm { get; set; }

    /// <summary>Reference to the outlines dictionary (Chapter 12.3.3).</summary>
    public PdfReference? Outlines { get; set; }

    /// <summary>OCProperties dictionary, holding OCGs + the default OC config (Chapter 10).</summary>
    public PdfDictionary? OCProperties { get; set; }

    /// <summary>XMP metadata stream reference (Chapter 14.3).</summary>
    public PdfReference? Metadata { get; set; }

    /// <summary>Collection (portfolio) dictionary reference.</summary>
    public PdfReference? Collection { get; set; }

    /// <summary>Append an output intent reference (Chapter 13) to the catalog's OutputIntents array.</summary>
    public void AddOutputIntent(PdfReference intent)
    {
        _outputIntents ??= new List<PdfReference>();
        _outputIntents.Add(intent);
    }

    public override void Write(Stream stream) => Build().Write(stream);

    private PdfDictionary Build()
    {
        var d = new PdfDictionary
        {
            { "Type", new PdfName("Catalog") },
        };
        if (_pages is { } p) d.Add("Pages", p);
        if (PageLayout is not null) d.Add("PageLayout", new PdfName(PageLayout));
        if (PageMode is not null) d.Add("PageMode", new PdfName(PageMode));
        if (_viewerPreferences is { IsEmpty: false } vp) d.Add("ViewerPreferences", vp.Build());
        if (_names is { Count: > 0 })
        {
            var names = new PdfDictionary();
            foreach (var (k, v) in _names) names.Add(k, v);
            d.Add("Names", names);
        }
        if (OpenAction is not null) d.Add("OpenAction", OpenAction);
        if (StructTreeRoot is { } sr)
        {
            d.Add("StructTreeRoot", sr);
            d.Add("MarkInfo", new PdfDictionary { { "Marked", new PdfBoolean(true) } });
        }
        if (AcroForm is { } af) d.Add("AcroForm", af);
        if (Outlines is { } o) d.Add("Outlines", o);
        if (OCProperties is not null) d.Add("OCProperties", OCProperties);
        if (Metadata is { } m) d.Add("Metadata", m);
        if (Collection is { } c) d.Add("Collection", c);
        if (_outputIntents is { Count: > 0 })
        {
            var arr = new PdfArray();
            foreach (var oi in _outputIntents) arr.Add(oi);
            d.Add("OutputIntents", arr);
        }
        return d;
    }
}
