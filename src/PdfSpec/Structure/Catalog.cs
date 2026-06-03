using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// The document catalog (ISO 32000-1 §7.7.2) — the root object of the document,
/// referenced from the trailer's <c>/Root</c>. Names the page tree, plus the
/// catalog-level features: PageLayout, PageMode, ViewerPreferences, Names,
/// OpenAction, Outlines, OCProperties, StructTreeRoot, AcroForm, OutputIntents,
/// Metadata, MarkInfo, Collection, and so on.
/// </summary>
public sealed class Catalog
{
    internal PdfDictionary Dictionary { get; } = new();

    private ViewerPreferences? _viewerPreferences;
    private PdfDictionary? _names;

    public Catalog()
    {
        Dictionary["Type"] = new PdfName("Catalog");
    }

    /// <summary>Reference to the root <c>/Pages</c> page-tree node.</summary>
    public PdfReference Pages
    {
        set => Dictionary["Pages"] = value;
    }

    /// <summary>How the viewer lays out pages: SinglePage, OneColumn, TwoPageLeft, ...</summary>
    public string? PageLayout
    {
        set
        {
            if (value is null) Dictionary.Remove("PageLayout");
            else Dictionary["PageLayout"] = new PdfName(value);
        }
    }

    /// <summary>Navigational chrome to show: UseNone, UseOutlines, UseThumbs, ...</summary>
    public string? PageMode
    {
        set
        {
            if (value is null) Dictionary.Remove("PageMode");
            else Dictionary["PageMode"] = new PdfName(value);
        }
    }

    /// <summary>Viewer-preferences dictionary, lazily created on first access.</summary>
    public ViewerPreferences ViewerPreferences
    {
        get
        {
            if (_viewerPreferences is null)
            {
                _viewerPreferences = new ViewerPreferences();
                Dictionary["ViewerPreferences"] = _viewerPreferences.Dictionary;
            }
            return _viewerPreferences;
        }
    }

    /// <summary>
    /// Register a name tree under the document's <c>/Names</c> dictionary, e.g.
    /// <c>SetNameTree("Dests", root)</c> for named destinations.
    /// </summary>
    public void SetNameTree(string category, PdfObject nameTreeRoot)
    {
        if (_names is null)
        {
            _names = new PdfDictionary();
            Dictionary["Names"] = _names;
        }
        _names[category] = nameTreeRoot;
    }

    /// <summary>Action or destination triggered when the document is opened.</summary>
    public PdfObject? OpenAction
    {
        set
        {
            if (value is null) Dictionary.Remove("OpenAction");
            else Dictionary["OpenAction"] = value;
        }
    }

    /// <summary>Reference to the root of the structure tree (Chapter 11).</summary>
    public PdfReference? StructTreeRoot
    {
        set
        {
            if (value is null)
            {
                Dictionary.Remove("StructTreeRoot");
                Dictionary.Remove("MarkInfo");
            }
            else
            {
                Dictionary["StructTreeRoot"] = value;
                Dictionary["MarkInfo"] = new PdfDictionary { ["Marked"] = new PdfBoolean(true) };
            }
        }
    }

    /// <summary>The AcroForm dictionary reference (Chapter 7).</summary>
    public PdfReference? AcroForm
    {
        set
        {
            if (value is null) Dictionary.Remove("AcroForm");
            else Dictionary["AcroForm"] = value;
        }
    }

    /// <summary>Reference to the outlines dictionary (Chapter 12.3.3).</summary>
    public PdfReference? Outlines
    {
        set
        {
            if (value is null) Dictionary.Remove("Outlines");
            else Dictionary["Outlines"] = value;
        }
    }

    /// <summary>OCProperties dictionary, holding OCGs + the default OC config (Chapter 10).</summary>
    public PdfDictionary? OCProperties
    {
        get => Dictionary.Get("OCProperties") as PdfDictionary;
        set
        {
            if (value is null) Dictionary.Remove("OCProperties");
            else Dictionary["OCProperties"] = value;
        }
    }

    /// <summary>Output intents array (Chapter 13) — referenced indirect entries.</summary>
    public void AddOutputIntent(PdfReference intent)
    {
        if (Dictionary.Get("OutputIntents") is not PdfArray intents)
        {
            intents = new PdfArray();
            Dictionary["OutputIntents"] = intents;
        }
        intents.Add(intent);
    }

    /// <summary>XMP metadata stream reference (Chapter 14.3).</summary>
    public PdfReference? Metadata
    {
        set
        {
            if (value is null) Dictionary.Remove("Metadata");
            else Dictionary["Metadata"] = value;
        }
    }

    /// <summary>Collection (portfolio) dictionary reference.</summary>
    public PdfReference? Collection
    {
        set
        {
            if (value is null) Dictionary.Remove("Collection");
            else Dictionary["Collection"] = value;
        }
    }
}
