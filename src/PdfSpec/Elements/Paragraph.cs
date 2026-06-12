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
        int wordCount = 0;
        foreach (var word in Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            double w = Font.MeasureText(word, FontSize);
            singleLineWidth += w;
            if (w > maxWordWidth) maxWordWidth = w;
            wordCount++;
        }
        // N words → N-1 inter-word spaces. Without this the desired width
        // under-reports and Rows leaves the column too narrow to actually
        // fit on one line.
        if (wordCount > 1)
            singleLineWidth += (wordCount - 1) * Font.MeasureText(" ", FontSize);

        double maxWidth = Math.Min(available.Width, singleLineWidth);
        double lineHeight = Font.GetVerticalMetrics(FontSize).LineHeight;

        // Wrap once at the available width and report the resulting
        // height as MaxHeight. A null MaxHeight would force flex
        // containers (VStack, MultiColumn) to fall back to MinHeight =
        // lineHeight in their fit-checks — that under-estimates by a
        // factor of (lines), and a paragraph that lands near the end of
        // a column can render past the page edge before anyone notices.
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, available.Width);
        double maxHeight = lines.Count * lineHeight;

        return new PdfSizeHint(maxWordWidth, lineHeight, maxWidth, maxHeight);
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
