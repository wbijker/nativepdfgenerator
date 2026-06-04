using PdfSpec.Objects;

namespace PdfSpec.Actions;

/// <summary>
/// Base for PDF action dictionaries (ISO 32000-1 §12.6) — what to do when
/// triggered (clicked link, opened document, etc.). Subclasses cover the
/// well-defined action types; <see cref="Build"/> produces the underlying
/// <c>/Type /Action /S /Subtype ...</c> dictionary. Named <c>PdfAction</c>
/// to avoid colliding with <see cref="System.Action"/>.
/// </summary>
public abstract class PdfAction
{
    public abstract PdfDictionary Build();

    /// <summary>Build the <c>/Type /Action</c> base dictionary with the given <c>/S</c> subtype.</summary>
    protected static PdfDictionary Base(string subtype) => new()
    {
        { "Type", new PdfName("Action") },
        { "S", new PdfName(subtype) },
    };
}
