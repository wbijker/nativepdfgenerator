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
        _dictionary.Add("Type", new PdfName("Catalog"));
    }

    /// <summary>Indirect reference to the root <c>/Pages</c> page-tree node.</summary>
    public PdfReference Pages { set => _dictionary.Add("Pages", value); }

    /// <summary>How the viewer lays out pages: SinglePage, OneColumn, TwoPageLeft, ...</summary>
    public string? PageLayout
    {
        set
        {
            if (value is null) _dictionary.Remove("PageLayout");
            else _dictionary.Add("PageLayout", new PdfName(value));
        }
    }

    /// <summary>Navigational chrome to show: UseNone, UseOutlines, UseThumbs, ...</summary>
    public string? PageMode
    {
        set
        {
            if (value is null) _dictionary.Remove("PageMode");
            else _dictionary.Add("PageMode", new PdfName(value));
        }
    }

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
    public PdfObject? OpenAction
    {
        set
        {
            if (value is null) _dictionary.Remove("OpenAction");
            else _dictionary.Add("OpenAction", value);
        }
    }

    /// <summary>Reference to the root of the structure tree (Chapter 11).</summary>
    public PdfReference? StructTreeRoot
    {
        set
        {
            if (value is { } v)
            {
                _dictionary.Add("StructTreeRoot", v);
                _dictionary.Add("MarkInfo", new PdfDictionary { { "Marked", new PdfBoolean(true) } });
            }
            else
            {
                _dictionary.Remove("StructTreeRoot");
                _dictionary.Remove("MarkInfo");
            }
        }
    }

    /// <summary>The AcroForm dictionary reference (Chapter 7).</summary>
    public PdfReference? AcroForm
    {
        set
        {
            if (value is null) _dictionary.Remove("AcroForm");
            else _dictionary.Add("AcroForm", value);
        }
    }

    /// <summary>Reference to the outlines dictionary (Chapter 12.3.3).</summary>
    public PdfReference? Outlines
    {
        set
        {
            if (value is null) _dictionary.Remove("Outlines");
            else _dictionary.Add("Outlines", value);
        }
    }

    /// <summary>OCProperties dictionary, holding OCGs + the default OC config (Chapter 10).</summary>
    public PdfDictionary? OCProperties
    {
        set
        {
            if (value is null) _dictionary.Remove("OCProperties");
            else _dictionary.Add("OCProperties", value);
        }
    }

    /// <summary>XMP metadata stream reference (Chapter 14.3).</summary>
    public PdfReference? Metadata
    {
        set
        {
            if (value is null) _dictionary.Remove("Metadata");
            else _dictionary.Add("Metadata", value);
        }
    }

    /// <summary>Collection (portfolio) dictionary reference.</summary>
    public PdfReference? Collection
    {
        set
        {
            if (value is null) _dictionary.Remove("Collection");
            else _dictionary.Add("Collection", value);
        }
    }

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
