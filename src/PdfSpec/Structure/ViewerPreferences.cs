using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// The document <c>/ViewerPreferences</c> dictionary (ISO 32000-1 §12.2):
/// hints to the viewer about how to present the document on open. All entries
/// are optional and emitted only when set non-null; setting back to null
/// removes the entry. State is held directly in the dictionary — no per-save
/// allocation.
/// </summary>
public sealed class ViewerPreferences
{
    internal PdfDictionary Dictionary { get; } = new();

    /// <summary>Show the document title (from <see cref="DocumentInfo.Title"/>) instead of the filename.</summary>
    public bool? DisplayDocTitle { set => Dictionary.SetBoolean("DisplayDocTitle", value); }

    /// <summary>Hide the viewer's tool bar while the document is open.</summary>
    public bool? HideToolbar { set => Dictionary.SetBoolean("HideToolbar", value); }

    /// <summary>Hide the viewer's menu bar while the document is open.</summary>
    public bool? HideMenubar { set => Dictionary.SetBoolean("HideMenubar", value); }

    /// <summary>Resize the viewer window to fit the first displayed page.</summary>
    public bool? FitWindow { set => Dictionary.SetBoolean("FitWindow", value); }

    /// <summary>Center the document window on the screen.</summary>
    public bool? CenterWindow { set => Dictionary.SetBoolean("CenterWindow", value); }

    internal bool IsEmpty => Dictionary.Entries.Count == 0;
}
