namespace PdfSpec.Fonts;

/// <summary>
/// A bundle of four related <see cref="Font"/> faces (regular, bold,
/// italic, bold-italic) used by <see cref="Elements.Paragraph"/>'s
/// fluent face-switching API (<c>.Bold(...)</c>, <c>.Italic(...)</c>,
/// <c>.BoldItalic(...)</c>). The high-level paragraph form captures one
/// <see cref="FontFamily"/> + size at the root and uses the matching face
/// for each span.
/// </summary>
public sealed class FontFamily
{
    public Font Regular { get; }
    public Font Bold { get; }
    public Font Italic { get; }
    public Font BoldItalic { get; }

    public FontFamily(Font regular, Font bold, Font italic, Font boldItalic)
    {
        Regular = regular;
        Bold = bold;
        Italic = italic;
        BoldItalic = boldItalic;
    }

    /// <summary>Standard 14 Helvetica family — regular / bold / oblique / bold-oblique.</summary>
    public static FontFamily Helvetica { get; } = new(
        StandardFont.Helvetica,
        StandardFont.HelveticaBold,
        StandardFont.HelveticaOblique,
        StandardFont.HelveticaBoldOblique);

    /// <summary>Standard 14 Times family — Roman / bold / italic / bold-italic.</summary>
    public static FontFamily Times { get; } = new(
        StandardFont.TimesRoman,
        StandardFont.TimesBold,
        StandardFont.TimesItalic,
        StandardFont.TimesBoldItalic);

    /// <summary>Standard 14 Courier family — regular / bold / oblique / bold-oblique.</summary>
    public static FontFamily Courier { get; } = new(
        StandardFont.Courier,
        StandardFont.CourierBold,
        StandardFont.CourierOblique,
        StandardFont.CourierBoldOblique);
}
