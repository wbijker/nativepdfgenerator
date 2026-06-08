using PdfSpec.Objects;

namespace PdfSpec.Fonts;

/// <summary>
/// The Standard 14 (Base 14) font names that every PDF reader must provide,
/// plus the encoding names commonly paired with them. Use these constants
/// with <see cref="StandardFont"/>.
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

    /// <summary>
    /// Build a Type1 font dictionary for one of the Standard 14 fonts. Used by
    /// callers that want to register a font by raw resource name + reference on
    /// a page instead of going through <see cref="StandardFont"/>.
    /// </summary>
    public static PdfDictionary Create(string baseFont, string? encoding = null)
    {
        var font = new PdfDictionary();
        font.SetName("Type", "Font");
        font.SetName("Subtype", "Type1");
        font.SetName("BaseFont", baseFont);

        string? effective = encoding;
        if (effective is null && baseFont is not (Symbol or ZapfDingbats))
        {
            effective = WinAnsiEncoding;
        }
        if (effective is not null)
        {
            font.SetName("Encoding", effective);
        }
        return font;
    }
}

/// <summary>
/// One of the Standard 14 fonts (ISO 32000-1 §9.6.2.2): not embedded, since
/// every reader provides them. Latin faces default to WinAnsiEncoding;
/// Symbol/ZapfDingbats keep their built-in encodings. Constructor is private —
/// use the prebuilt static instances (<see cref="Helvetica"/>, …) or
/// <see cref="Create"/> for an arbitrary base font / encoding pair.
/// </summary>
public sealed class StandardFont : Font
{
    private StandardFont(string baseFont, string? encoding = null)
    {
        BaseFont = baseFont;
        Encoding = encoding ?? DefaultEncoding(baseFont);
    }

    public override string BaseFont { get; }
    public string? Encoding { get; }

    public override string Key => $"S14:{BaseFont}:{Encoding}";

    /// <summary>Create a Standard 14 font instance by base-font name.</summary>
    public static StandardFont Create(string baseFont, string? encoding = null) => new(baseFont, encoding);

    public static readonly StandardFont Helvetica = new(StandardFonts.Helvetica);
    public static readonly StandardFont HelveticaBold = new(StandardFonts.HelveticaBold);
    public static readonly StandardFont HelveticaOblique = new(StandardFonts.HelveticaOblique);
    public static readonly StandardFont TimesRoman = new(StandardFonts.TimesRoman);
    public static readonly StandardFont TimesBold = new(StandardFonts.TimesBold);
    public static readonly StandardFont TimesItalic = new(StandardFonts.TimesItalic);
    public static readonly StandardFont Courier = new(StandardFonts.Courier);

    public override int GetGlyphWidth(char c) => FontMetrics.GlyphWidth(BaseFont, c);

    public override FontVerticalMetrics GetVerticalMetrics(double fontSize) =>
        FontMetrics.GetVerticalMetrics(BaseFont, fontSize);

    internal override void Build(PdfObjectStore store, PdfDictionary fontDictionary)
    {
        fontDictionary.SetName("Type", "Font");
        fontDictionary.SetName("Subtype", "Type1");
        fontDictionary.SetName("BaseFont", BaseFont);
        fontDictionary.SetName("Encoding", Encoding);
    }

    private static string? DefaultEncoding(string baseFont) =>
        baseFont is StandardFonts.Symbol or StandardFonts.ZapfDingbats
            ? null
            : StandardFonts.WinAnsiEncoding;
}
