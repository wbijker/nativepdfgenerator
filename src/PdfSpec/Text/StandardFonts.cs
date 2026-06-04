namespace PdfSpec.Text;

/// <summary>
/// The Standard 14 (Base 14) font names that every PDF reader must provide,
/// plus the encoding names commonly paired with them. Use these constants
/// with <see cref="Standard14Font"/>.
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

    public const string WinAnsiEncoding = "WinAnsiEncoding";
    public const string MacRomanEncoding = "MacRomanEncoding";
}
