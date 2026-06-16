using PdfSpec.Fonts;

namespace PdfSpec.Elements;

/// <summary>
/// A <see cref="Paragraph"/> bound to a <see cref="FontFamily"/>. Adds the
/// face-aware fluent setters <see cref="Bold"/>, <see cref="Italic"/>,
/// <see cref="Italics"/>, and <see cref="BoldItalic"/> — each appends a
/// span using the matching face from <see cref="Family"/>. Returned by
/// <see cref="Element.Paragraph(FontFamily, double)"/> and the
/// <see cref="IContainer"/> family overloads; the methods narrow the
/// available API at compile time so callers without a family can't reach
/// the face-aware setters at all.
///
/// <para>
/// Each face-aware setter accepts optional <c>size</c> and
/// <see cref="TextAlignment"/> parameters — omit them to inherit the
/// paragraph's defaults.
/// </para>
/// </summary>
public class FamilyParagraph : Paragraph
{
    /// <summary>The font family captured at construction.</summary>
    public FontFamily Family { get; }

    public FamilyParagraph(FontFamily family, double fontSize)
        : base(family.Regular, fontSize)
    {
        Family = family;
    }

    public FamilyParagraph(FontFamily family, double fontSize, Action<FamilyParagraph> build)
        : base(family.Regular, fontSize)
    {
        Family = family;
        build(this);
    }

    /// <summary>Append a span using <see cref="FontFamily.Bold"/>.</summary>
    public FamilyParagraph Bold(string text, double? size = null, TextAlignment align = TextAlignment.Baseline)
    {
        base.Text(text, Family.Bold, size, align);
        return this;
    }

    /// <summary>Append a span using <see cref="FontFamily.Italic"/>.</summary>
    public FamilyParagraph Italic(string text, double? size = null, TextAlignment align = TextAlignment.Baseline)
    {
        base.Text(text, Family.Italic, size, align);
        return this;
    }

    /// <summary>Alias for <see cref="Italic"/> — matches the natural plural in chained form.</summary>
    public FamilyParagraph Italics(string text, double? size = null, TextAlignment align = TextAlignment.Baseline)
        => Italic(text, size, align);

    /// <summary>Append a span using <see cref="FontFamily.BoldItalic"/>.</summary>
    public FamilyParagraph BoldItalic(string text, double? size = null, TextAlignment align = TextAlignment.Baseline)
    {
        base.Text(text, Family.BoldItalic, size, align);
        return this;
    }

    // ===== Narrowed return types for chained inheritance ======================
    // Each base method is shadowed so the chain stays typed as FamilyParagraph
    // and the face-aware setters remain reachable after a .Text / .Newline.

    public new FamilyParagraph Text(string text, Font? font = null, double? size = null,
        TextAlignment align = TextAlignment.Baseline)
    {
        base.Text(text, font, size, align);
        return this;
    }

    public new FamilyParagraph Newline()
    {
        base.Newline();
        return this;
    }
}
