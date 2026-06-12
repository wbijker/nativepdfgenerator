using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

public class Paragraph : Element
{
    public Paragraph(string text, Font font, double fontSize)
    {
        Text = text;
        Font = font;
        FontSize = fontSize;
    }

    public string Text { get; set; }
    public Font Font { get; set; }
    public double FontSize { get; set; }

    /// <summary>Fill colour for the glyphs (and the underline, if drawn). <c>null</c> = device default (black).</summary>
    public PdfColor? Color { get; set; }

    /// <summary>When true, a horizontal rule is drawn under each wrapped line at ~10% of <see cref="FontSize"/> below the baseline.</summary>
    public bool Underline { get; set; }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        double maxWordWidth = 0;
        double singleLineWidth = 0;
        foreach (var word in Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var w = Font.MeasureText(word, FontSize);
            singleLineWidth += w;
            if (w > maxWordWidth) maxWordWidth = w;
        }
        
        return new PdfSizeHint(maxWordWidth, singleLineWidth, null, null);
    }

    protected override RenderResult RenderCore(ContentStream cs, PdfSize available)
    {
        var metrics = Font.GetVerticalMetrics(FontSize);
        double lineHeight = metrics.LineHeight;
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, available.Width);

        if (lines.Count == 0) return RenderResult.Done(0);

        bool scoped = Color is not null;
        if (scoped) { cs.Save(); cs.SetFillColor(Color!); }

        // Set TL = lineHeight once, place the first line with Tm, then
        // use ' (next-line + show, i.e. T* Tj) for every subsequent line
        // so the body of the paragraph is built from PDF-native newline
        // operators instead of a fresh Tm per line.
        var txt = cs.AddText()
            .SetFont(Font, FontSize)
            .SetLeading(lineHeight)
            .Show(0, 0, lines[0]);
        for (int i = 1; i < lines.Count; i++)
            txt.NextLineShowText(lines[i]);
        txt.Build();

        if (Underline)
        {
            // Cap-top sits at local y=0 (Text.SetTextMatrix offsets by ascent),
            // so line N's baseline is at ascent + N*lineHeight. Drop the rule
            // a tenth of the font size below that.
            double ascent = metrics.Ascent;
            double offset = FontSize * 0.1;
            double thickness = Math.Max(0.5, FontSize * 0.05);
            var stroke = Color ?? PdfColor.Gray(0);
            for (int i = 0; i < lines.Count; i++)
            {
                double w = Font.MeasureText(lines[i], FontSize);
                double y = ascent + i * lineHeight + offset;
                cs.DrawRectangle(0, y - thickness / 2, w, thickness, fill: stroke);
            }
        }

        if (scoped) cs.Restore();

        return RenderResult.Done(lines.Count * lineHeight);
    }
}
