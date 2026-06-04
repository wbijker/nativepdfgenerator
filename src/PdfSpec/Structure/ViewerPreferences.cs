using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// The document <c>/ViewerPreferences</c> dictionary (ISO 32000-1 §12.2):
/// hints to the viewer about how to present the document on open. All entries
/// are optional and emitted only when set.
/// </summary>
public sealed class ViewerPreferences
{
    /// <summary>Show the document title (from <see cref="DocumentInfo.Title"/>) instead of the filename.</summary>
    public bool? DisplayDocTitle { get; set; }

    /// <summary>Hide the viewer's tool bar while the document is open.</summary>
    public bool? HideToolbar { get; set; }

    /// <summary>Hide the viewer's menu bar while the document is open.</summary>
    public bool? HideMenubar { get; set; }

    /// <summary>Resize the viewer window to fit the first displayed page.</summary>
    public bool? FitWindow { get; set; }

    /// <summary>Center the document window on the screen.</summary>
    public bool? CenterWindow { get; set; }

    internal bool IsEmpty =>
        DisplayDocTitle is null && HideToolbar is null && HideMenubar is null
        && FitWindow is null && CenterWindow is null;

    public PdfDictionary Build()
    {
        var d = new PdfDictionary();
        if (DisplayDocTitle is { } v1) d.Add("DisplayDocTitle", new PdfBoolean(v1));
        if (HideToolbar is { } v2) d.Add("HideToolbar", new PdfBoolean(v2));
        if (HideMenubar is { } v3) d.Add("HideMenubar", new PdfBoolean(v3));
        if (FitWindow is { } v4) d.Add("FitWindow", new PdfBoolean(v4));
        if (CenterWindow is { } v5) d.Add("CenterWindow", new PdfBoolean(v5));
        return d;
    }
}
