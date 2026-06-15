namespace PdfSpec.Elements;

/// <summary>
/// Vertical placement of a paragraph span relative to its line. Used by
/// <see cref="Paragraph.Text(string, Fonts.Font?, double?, TextAlignment)"/>
/// and the <see cref="FamilyParagraph"/> face-aware setters.
///
/// <para>
/// <see cref="Baseline"/> is the default — all spans share the line
/// baseline. <see cref="Sub"/> and <see cref="Sup"/> shift the baseline
/// (the span's glyphs use their own size; pair with a smaller size for
/// classic typographic sub/superscript). <see cref="Top"/>,
/// <see cref="Middle"/>, and <see cref="Bottom"/> position the span's
/// glyph box within the line box without affecting the line's height
/// — they're for small marks/badges within a normally-set line.
/// </para>
/// </summary>
public enum TextAlignment
{
    /// <summary>Default — span sits on the line baseline.</summary>
    Baseline,
    /// <summary>Glyph top aligns to the line top.</summary>
    Top,
    /// <summary>Glyph vertical centre aligns to the line vertical centre.</summary>
    Middle,
    /// <summary>Glyph bottom aligns to the line bottom (max descent).</summary>
    Bottom,
    /// <summary>Subscript — baseline shifted down. Span's own size; use a smaller size for typographic sub.</summary>
    Sub,
    /// <summary>Superscript — baseline shifted up. Span's own size; use a smaller size for typographic sup.</summary>
    Sup,
}
