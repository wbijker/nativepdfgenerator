using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 10 — the standard 14 fonts: one line per face, registered on
/// the page under its own resource name and drawn at 22 pt. Covers the
/// four Helvetica / Times / Courier variants plus Symbol and
/// ZapfDingbats so the differences in glyph repertoire show up.
/// </summary>
public sealed class Sample10_TextFonts : ISample
{
    public string FileName => "10-text-fonts.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);

        (string resource, string baseFont, Font font, string sample)[] rows =
        {
            ("F1", StandardFonts.Helvetica,             StandardFont.Helvetica,             "Helvetica: Pack my box."),
            ("F2", StandardFonts.HelveticaBoldOblique,  StandardFont.HelveticaBoldOblique,  "Helvetica-BoldOblique"),
            ("F3", StandardFonts.TimesRoman,            StandardFont.TimesRoman,            "Times-Roman: Pack my box."),
            ("F4", StandardFonts.TimesItalic,           StandardFont.TimesItalic,           "Times-Italic: Pack my box."),
            ("F5", StandardFonts.CourierBold,           StandardFont.CourierBold,           "Courier-Bold: Pack my box."),
            ("F6", StandardFonts.Symbol,                StandardFont.Create(StandardFonts.Symbol),       "abcdefghijklmnop"),
            ("F7", StandardFonts.ZapfDingbats,          StandardFont.Create(StandardFonts.ZapfDingbats), "abcdefghijklmnop"),
        };

        var c = page.Content;
        double y = 720;
        foreach (var (resource, baseFont, font, sample) in rows)
        {
            page.AddFont(resource, doc.AddObject(StandardFonts.Create(baseFont)));
            c.AddText(font, 22).Show(60, y, sample).Build();
            y -= 50;
        }

        doc.Save(path);
    }
}
