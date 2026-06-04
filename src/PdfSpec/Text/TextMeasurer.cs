using System.Text;

namespace PdfSpec.Fonts;

/// <summary>
/// Measures text set in the Standard 14 fonts, using <see cref="FontMetrics"/>.
/// Widths account for the text-state parameters that affect glyph displacement
/// (ISO 32000-1 §9.4.4).
/// </summary>
public static class TextMeasurer
{
    public static double MeasureText(string baseFont, double fontSize, string text,
        double charSpacing = 0, double wordSpacing = 0, double horizontalScale = 100)
    {
        double total = 0;
        foreach (char c in text)
        {
            total += FontMetrics.GlyphWidth(baseFont, c) / 1000.0 * fontSize + charSpacing;
            if (c == ' ')
            {
                total += wordSpacing;
            }
        }
        return total * (horizontalScale / 100.0);
    }

    public static List<string> WrapText(string baseFont, double fontSize, string text, double maxWidth,
        double charSpacing = 0, double wordSpacing = 0, double horizontalScale = 100)
    {
        var lines = new List<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            var current = new StringBuilder();
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = current.Length == 0 ? word : $"{current} {word}";
                double width = MeasureText(baseFont, fontSize, candidate, charSpacing, wordSpacing, horizontalScale);
                if (current.Length == 0 || width <= maxWidth)
                {
                    current.Clear();
                    current.Append(candidate);
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
            }
            lines.Add(current.ToString());
        }
        return lines;
    }

    public static List<string> WrapText(Font font, double fontSize, string text, double maxWidth)
    {
        var lines = new List<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            var current = new StringBuilder();
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = current.Length == 0 ? word : $"{current} {word}";
                double width = font.MeasureText(candidate, fontSize);
                if (current.Length == 0 || width <= maxWidth)
                {
                    current.Clear();
                    current.Append(candidate);
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
            }
            lines.Add(current.ToString());
        }
        return lines;
    }
}
