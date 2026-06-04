using PdfSpec.Objects;

namespace PdfSpec.Fonts;

/// <summary>
/// A font registered with the document — the indirect reference to its
/// <c>/Font</c> dictionary plus the bookkeeping needed to build that
/// dictionary at save time. Document-wide identity: any page that uses
/// the same <see cref="Fonts.Font"/> shares the same <see cref="FontResource"/>.
/// </summary>
public sealed class FontResource
{
    /// <summary>The source font (Standard 14, TrueType, …) registered on the document.</summary>
    public Font Font { get; }

    /// <summary>The shared resource name used in page <c>/Resources/Font</c> tables — also the name passed to the <c>Tf</c> operator.</summary>
    public string Name { get; }

    /// <summary>The indirect reference to the font dictionary in the PDF.</summary>
    public PdfReference Reference { get; }

    /// <summary>The actual <c>/Font</c> dictionary the font's <c>Build</c> step writes into at save time.</summary>
    internal PdfDictionary Dictionary { get; }

    internal FontResource(Font font, string name, PdfDictionary dictionary, PdfReference reference)
    {
        Font = font;
        Name = name;
        Dictionary = dictionary;
        Reference = reference;
    }
}
