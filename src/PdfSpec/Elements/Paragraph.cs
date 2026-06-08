using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

public class Paragraph(string text, Font font, double fontSize) : Element
{
    public string Text { get; } = text;
    public Font Font { get; } = font;
    public double FontSize { get; } = fontSize;

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

        return new PdfSizeHint(maxWordWidth, lineHeight, maxWidth, null);
    }

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        double lineHeight = Font.GetVerticalMetrics(FontSize).LineHeight;
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, available.Width);

        if (lines.Count > 0)
        {
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
        }

        return RenderResult.Done(lines.Count * lineHeight);
    }
}
