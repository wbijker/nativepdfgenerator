using CSharpPdf.Objects;

namespace CSharpPdf.Text;

/// <summary>
/// The Standard 14 (Base 14) fonts (Chapter 4, "The Font Dictionary") that every
/// PDF reader must provide, so their programs need not be embedded and their
/// metrics are known to the reader. Use <see cref="Create"/> to build a simple
/// Type1 font dictionary for one of them.
/// </summary>
public static class StandardFonts
{
    public const string TimesRoman = "Times-Roman";
    public const string TimesBold = "Times-Bold";
    public const string TimesItalic = "Times-Italic";
    public const string TimesBoldItalic = "Times-BoldItalic";
    public const string Helvetica = "Helvetica";
    public const string HelveticaBold = "Helvetica-Bold";
    public const string HelveticaOblique = "Helvetica-Oblique";
    public const string HelveticaBoldOblique = "Helvetica-BoldOblique";
    public const string Courier = "Courier";
    public const string CourierBold = "Courier-Bold";
    public const string CourierOblique = "Courier-Oblique";
    public const string CourierBoldOblique = "Courier-BoldOblique";
    public const string Symbol = "Symbol";
    public const string ZapfDingbats = "ZapfDingbats";

    /// <summary>WinAnsiEncoding (Windows code page 1252), a superset of Latin-1.</summary>
    public const string WinAnsiEncoding = "WinAnsiEncoding";

    /// <summary>MacRomanEncoding.</summary>
    public const string MacRomanEncoding = "MacRomanEncoding";

    /// <summary>
    /// Build a Type1 font dictionary for a Standard 14 font. When
    /// <paramref name="encoding"/> is null, the Latin text fonts default to
    /// WinAnsiEncoding so that Latin-1 byte strings render correctly; the symbolic
    /// fonts (Symbol, ZapfDingbats) keep their built-in encodings. Pass an explicit
    /// encoding to override.
    /// </summary>
    public static PdfDictionary Create(string baseFont, string? encoding = null)
    {
        var font = new PdfDictionary
        {
            ["Type"] = new PdfName("Font"),
            ["Subtype"] = new PdfName("Type1"),
            ["BaseFont"] = new PdfName(baseFont),
        };

        string? effective = encoding;
        if (effective is null && baseFont is not (Symbol or ZapfDingbats))
        {
            effective = WinAnsiEncoding;
        }
        if (effective is not null)
        {
            font["Encoding"] = new PdfName(effective);
        }
        return font;
    }
}
