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
        foreach (var word in Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            double w = Font.MeasureText(word, FontSize);
            if (w > maxWordWidth) maxWordWidth = w;
        }

        double singleLineWidth = Font.MeasureText(Text, FontSize);
        double maxWidth = Math.Min(available.Width, singleLineWidth);
        double lineHeight = Font.GetVerticalMetrics(FontSize).LineHeight;

        return new PdfSizeHint(maxWordWidth, lineHeight, maxWidth, null);
    }

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        double lineHeight = Font.GetVerticalMetrics(FontSize).LineHeight;
        var lines = TextMeasurer.WrapText(Font, FontSize, Text, available.Width);

        var txt = cs.AddText().SetFont(Font, FontSize);
        for (int i = 0; i < lines.Count; i++)
        {
            txt.Show(0, i * lineHeight, lines[i]);
        }
        txt.Build();

        return RenderResult.Done(lines.Count * lineHeight);
    }
}
