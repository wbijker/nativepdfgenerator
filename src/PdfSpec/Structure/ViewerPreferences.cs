using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// The document <c>/ViewerPreferences</c> dictionary (ISO 32000-1 §12.2):
/// hints to the viewer about how to present the document on open (window UI,
/// PageMode override, print scaling, duplex, etc.). All entries are optional.
/// </summary>
public sealed class ViewerPreferences
{
    internal PdfDictionary Dictionary { get; } = new();

    /// <summary>When true, viewers show the document title (from <see cref="DocumentInfo.Title"/>) instead of the filename.</summary>
    public bool? DisplayDocTitle
    {
        set
        {
            if (value is null) Dictionary.Remove("DisplayDocTitle");
            else Dictionary["DisplayDocTitle"] = new PdfBoolean(value.Value);
        }
    }

    /// <summary>Hide the viewer's tool bar while the document is open.</summary>
    public bool? HideToolbar
    {
        set
        {
            if (value is null) Dictionary.Remove("HideToolbar");
            else Dictionary["HideToolbar"] = new PdfBoolean(value.Value);
        }
    }

    /// <summary>Hide the viewer's menu bar while the document is open.</summary>
    public bool? HideMenubar
    {
        set
        {
            if (value is null) Dictionary.Remove("HideMenubar");
            else Dictionary["HideMenubar"] = new PdfBoolean(value.Value);
        }
    }

    /// <summary>Resize the viewer window to fit the first displayed page.</summary>
    public bool? FitWindow
    {
        set
        {
            if (value is null) Dictionary.Remove("FitWindow");
            else Dictionary["FitWindow"] = new PdfBoolean(value.Value);
        }
    }

    /// <summary>Center the document window on the screen.</summary>
    public bool? CenterWindow
    {
        set
        {
            if (value is null) Dictionary.Remove("CenterWindow");
            else Dictionary["CenterWindow"] = new PdfBoolean(value.Value);
        }
    }
}
