using CSharpPdf.Objects;

namespace CSharpPdf.Text;

/// <summary>
/// One of the Standard 14 fonts (ISO 32000-1 §9.6.2.2): not embedded, since every
/// reader provides them. Metrics come from the built-in <see cref="FontMetrics"/>
/// tables. Latin faces default to WinAnsiEncoding; Symbol/ZapfDingbats keep their
/// built-in encodings.
/// </summary>
public sealed class Standard14Font : Font
{
    public Standard14Font(string baseFont, string? encoding = null)
    {
        BaseFont = baseFont;
        Encoding = encoding ?? DefaultEncoding(baseFont);
    }

    public override string BaseFont { get; }
    public string? Encoding { get; }

    public override string Key => $"S14:{BaseFont}:{Encoding}";

    public static readonly Standard14Font Helvetica = new(StandardFonts.Helvetica);
    public static readonly Standard14Font HelveticaBold = new(StandardFonts.HelveticaBold);
    public static readonly Standard14Font HelveticaOblique = new(StandardFonts.HelveticaOblique);
    public static readonly Standard14Font TimesRoman = new(StandardFonts.TimesRoman);
    public static readonly Standard14Font TimesBold = new(StandardFonts.TimesBold);
    public static readonly Standard14Font TimesItalic = new(StandardFonts.TimesItalic);
    public static readonly Standard14Font Courier = new(StandardFonts.Courier);

    public override int GetGlyphWidth(char c) => FontMetrics.GlyphWidth(BaseFont, c);

    public override FontVerticalMetrics GetVerticalMetrics(double fontSize) =>
        FontMetrics.GetVerticalMetrics(BaseFont, fontSize);

    internal override void Build(PdfObjectStore store, PdfDictionary fontDictionary)
    {
        fontDictionary["Type"] = new PdfName("Font");
        fontDictionary["Subtype"] = new PdfName("Type1");
        fontDictionary["BaseFont"] = new PdfName(BaseFont);
        if (Encoding is not null)
        {
            fontDictionary["Encoding"] = new PdfName(Encoding);
        }
    }

    private static string? DefaultEncoding(string baseFont) =>
        baseFont is StandardFonts.Symbol or StandardFonts.ZapfDingbats
            ? null
            : StandardFonts.WinAnsiEncoding;
}
