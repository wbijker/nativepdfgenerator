using System.Text;

namespace PdfSpec.Fonts;

/// <summary>
/// Glyph advance widths for the Standard 14 fonts, in 1000-unit glyph space
/// (ISO 32000-1 §9.2.4). Values are the authoritative Adobe Core-14 AFM metrics.
/// Tables cover printable ASCII (codes 32–126); accented Latin-1 letters reuse
/// their base letter's width.
/// </summary>
public static class FontMetrics
{
    private const int CourierWidth = 600;

    private static readonly int[] Helvetica =
    {
        278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278,556,556,556,556,556,556,556,556,
        556,556,278,278,584,584,584,556,1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,
        667,778,722,667,611,722,667,944,667,667,611,278,278,278,469,556,333,556,556,500,556,556,278,556,
        556,222,222,500,222,833,556,556,556,556,333,500,278,556,500,722,500,500,500,334,260,334,584,
    };

    private static readonly int[] HelveticaBold =
    {
        278,333,474,556,556,889,722,238,333,333,389,584,278,333,278,278,556,556,556,556,556,556,556,556,
        556,556,333,333,584,584,584,611,975,722,722,722,722,667,611,778,722,278,556,722,611,833,722,778,
        667,778,722,667,611,722,667,944,667,667,611,333,278,333,584,556,333,556,611,556,611,556,333,611,
        611,278,278,556,278,889,611,611,611,611,389,556,333,611,556,778,556,556,500,389,280,389,584,
    };

    private static readonly int[] Times =
    {
        250,333,408,500,500,833,778,180,333,333,500,564,250,333,250,278,500,500,500,500,500,500,500,500,
        500,500,278,278,564,564,564,444,921,722,667,667,722,611,556,722,722,333,389,722,611,889,722,722,
        556,722,667,556,611,722,722,944,722,722,611,333,278,333,469,500,333,444,500,444,500,444,333,500,
        500,278,278,500,278,778,500,500,500,500,333,389,278,500,500,722,500,500,444,480,200,480,541,
    };

    private static readonly int[] TimesBold =
    {
        250,333,555,500,500,1000,833,278,333,333,500,570,250,333,250,278,500,500,500,500,500,500,500,500,
        500,500,333,333,570,570,570,500,930,722,667,722,722,667,611,778,778,389,500,778,667,944,722,778,
        611,778,722,556,667,722,722,1000,722,722,667,333,278,333,581,500,333,500,556,444,556,444,333,500,
        556,278,333,556,278,833,556,500,556,556,444,389,333,556,500,722,500,500,444,394,220,394,520,
    };

    private static readonly int[] TimesItalic =
    {
        250,333,420,500,500,833,778,214,333,333,500,675,250,333,250,278,500,500,500,500,500,500,500,500,
        500,500,333,333,675,675,675,500,920,611,611,667,722,611,611,722,722,333,444,667,556,833,667,722,
        611,722,611,500,556,722,611,833,611,556,556,389,278,389,422,500,333,500,500,444,500,444,278,500,
        500,278,278,444,278,722,500,500,500,500,389,389,278,500,444,667,444,444,389,400,275,400,541,
    };

    private static readonly int[] TimesBoldItalic =
    {
        250,389,555,500,500,833,778,278,333,333,500,570,250,333,250,278,500,500,500,500,500,500,500,500,
        500,500,333,333,570,570,570,500,832,667,667,667,722,667,667,722,778,389,500,667,611,889,722,722,
        611,722,667,556,611,722,667,889,667,611,611,333,278,333,570,500,333,500,500,444,500,444,333,500,
        556,278,278,500,278,778,556,500,500,500,389,389,278,556,444,667,500,444,389,348,220,348,570,
    };

    public static bool IsMonospaced(string baseFont) => baseFont.StartsWith("Courier", StringComparison.Ordinal);

    public static int Ascender(string baseFont) => RawMetrics(baseFont).Ascender;
    public static int Descender(string baseFont) => RawMetrics(baseFont).Descender;
    public static int CapHeight(string baseFont) => RawMetrics(baseFont).CapHeight;
    public static int XHeight(string baseFont) => RawMetrics(baseFont).XHeight;

    public static FontVerticalMetrics GetVerticalMetrics(string baseFont, double fontSize)
    {
        var m = RawMetrics(baseFont);
        double scale = fontSize / 1000.0;
        double ascent = m.Ascender * scale;
        double descent = -m.Descender * scale;
        return new FontVerticalMetrics(
            Ascent: ascent,
            Descent: descent,
            // Adobe's AFM Ascender / Descender already match the visible
            // reach of the Standard-14 glyphs (Adobe hand-tuned them), so
            // there's no separate Windows-clip metric to surface — typo
            // and win-clip coincide.
            WinAscent: ascent,
            WinDescent: descent,
            LineGap: 0,
            CapHeight: m.CapHeight * scale,
            XHeight: m.XHeight * scale);
    }

    private static (int Ascender, int Descender, int CapHeight, int XHeight) RawMetrics(string baseFont) => baseFont switch
    {
        StandardFonts.Helvetica or StandardFonts.HelveticaOblique => (718, -207, 718, 523),
        StandardFonts.HelveticaBold or StandardFonts.HelveticaBoldOblique => (718, -207, 718, 532),
        StandardFonts.TimesRoman => (683, -217, 662, 450),
        StandardFonts.TimesBold => (683, -217, 676, 461),
        StandardFonts.TimesItalic => (683, -217, 653, 441),
        StandardFonts.TimesBoldItalic => (683, -217, 669, 462),
        StandardFonts.Courier or StandardFonts.CourierBold
            or StandardFonts.CourierOblique or StandardFonts.CourierBoldOblique => (629, -157, 562, 426),
        _ => (718, -207, 718, 523),
    };

    public static int GlyphWidth(string baseFont, char c)
    {
        if (IsMonospaced(baseFont))
        {
            return CourierWidth;
        }

        int[]? table = AsciiTable(baseFont);
        if (table is null)
        {
            return 600;
        }
        if (c is >= ' ' and <= '~')
        {
            return table[c - ' '];
        }

        char baseChar = BaseLatinLetter(c);
        if (baseChar is >= ' ' and <= '~')
        {
            return table[baseChar - ' '];
        }
        return table['0' - ' '];
    }

    private static int[]? AsciiTable(string baseFont) => baseFont switch
    {
        StandardFonts.Helvetica or StandardFonts.HelveticaOblique => Helvetica,
        StandardFonts.HelveticaBold or StandardFonts.HelveticaBoldOblique => HelveticaBold,
        StandardFonts.TimesRoman => Times,
        StandardFonts.TimesBold => TimesBold,
        StandardFonts.TimesItalic => TimesItalic,
        StandardFonts.TimesBoldItalic => TimesBoldItalic,
        _ => null,
    };

    private static char BaseLatinLetter(char c)
    {
        switch (c)
        {
            case (char)0xA0: return ' ';
            case '×': case '÷': return '+';
        }
        string decomposed = c.ToString().Normalize(NormalizationForm.FormD);
        return decomposed.Length > 0 ? decomposed[0] : c;
    }
}
