using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// The document catalog (ISO 32000-1 §7.7.2) — the root object of the
/// document, referenced from the trailer's <c>/Root</c>. Wraps a single
/// <see cref="PdfDictionary"/> mutated in place; <see cref="Write"/>
/// delegates to it.
/// </summary>
public sealed class Catalog : PdfObject
{
    private readonly PdfDictionary _dictionary = new();
    private ViewerPreferences? _viewerPreferences;
    private PdfDictionary? _names;
    private PdfArray? _outputIntents;

    public Catalog()
    {
        _dictionary.SetName("Type", "Catalog");
    }

    /// <summary>Indirect reference to the root <c>/Pages</c> page-tree node.</summary>
    public PdfReference Pages { set => _dictionary.Add("Pages", value); }

    /// <summary>How the viewer lays out pages.</summary>
    public PageLayout? PageLayout { set => _dictionary.SetName("PageLayout", value?.ToString()); }

    /// <summary>Navigational chrome to show on open.</summary>
    public PageMode? PageMode { set => _dictionary.SetName("PageMode", value?.ToString()); }

    /// <summary>The viewer-preferences sub-object — lazily attached to <c>/ViewerPreferences</c> on first access.</summary>
    public ViewerPreferences ViewerPreferences
    {
        get
        {
            if (_viewerPreferences is null)
            {
                _viewerPreferences = new ViewerPreferences();
                _dictionary.Add("ViewerPreferences", _viewerPreferences.Dictionary);
            }
            return _viewerPreferences;
        }
    }

    /// <summary>
    /// Register a name tree under the document's <c>/Names</c> dictionary,
    /// e.g. <c>SetNameTree("Dests", root)</c>. The <c>/Names</c> sub-dict is
    /// created on first call; re-setting the same category replaces its value.
    /// </summary>
    public void SetNameTree(string category, PdfObject nameTreeRoot)
    {
        if (_names is null)
        {
            _names = new PdfDictionary();
            _dictionary.Add("Names", _names);
        }
        _names.Add(category, nameTreeRoot);
    }

    /// <summary>Action or destination triggered when the document is opened.</summary>
    public PdfObject? OpenAction { set => _dictionary.Set("OpenAction", value); }

    /// <summary>Reference to the root of the structure tree (Chapter 11). Setting non-null also marks the document via MarkInfo.</summary>
    public PdfReference? StructTreeRoot
    {
        set
        {
            _dictionary.Set("StructTreeRoot", value);
            _dictionary.Set("MarkInfo", value is null ? null : new PdfDictionary { { "Marked", new PdfBoolean(true) } });
        }
    }

    /// <summary>The AcroForm dictionary reference (Chapter 7).</summary>
    public PdfReference? AcroForm { set => _dictionary.Set("AcroForm", value); }

    /// <summary>Reference to the outlines dictionary (Chapter 12.3.3).</summary>
    public PdfReference? Outlines { set => _dictionary.Set("Outlines", value); }

    /// <summary>OCProperties dictionary, holding OCGs + the default OC config (Chapter 10).</summary>
    public PdfDictionary? OCProperties { set => _dictionary.Set("OCProperties", value); }

    /// <summary>XMP metadata stream reference (Chapter 14.3).</summary>
    public PdfReference? Metadata { set => _dictionary.Set("Metadata", value); }

    /// <summary>Collection (portfolio) dictionary reference.</summary>
    public PdfReference? Collection { set => _dictionary.Set("Collection", value); }

    /// <summary>Append an output intent reference (Chapter 13) to the catalog's OutputIntents array.</summary>
    public void AddOutputIntent(PdfReference intent)
    {
        if (_outputIntents is null)
        {
            _outputIntents = new PdfArray();
            _dictionary.Add("OutputIntents", _outputIntents);
        }
        _outputIntents.Add(intent);
    }

    public override void Write(Stream stream) => _dictionary.Write(stream);
}

/// <summary>
/// Catalog <c>/PageLayout</c> entry (ISO 32000-1 §7.7.2 Table 28). Enum
/// names match the PDF name objects emitted to the file.
/// </summary>
public enum PageLayout
{
    SinglePage,
    OneColumn,
    TwoColumnLeft,
    TwoColumnRight,
    TwoPageLeft,
    TwoPageRight,
}

/// <summary>
/// Catalog <c>/PageMode</c> entry (ISO 32000-1 §7.7.2 Table 28) — what
/// navigational chrome the viewer shows when the document is opened. Enum
/// names match the PDF name objects emitted to the file.
/// </summary>
public enum PageMode
{
    UseNone,
    UseOutlines,
    UseThumbs,
    FullScreen,
    UseOC,
    UseAttachments,
}
