using System.Text;

namespace CSharpPdf.Text;

/// <summary>
/// Measures text set in the Standard 14 fonts, using <see cref="FontMetrics"/>.
/// Widths account for the text-state parameters that affect glyph displacement
/// (ISO 32000-1 §9.4.4): font size, character spacing (Tc), word spacing (Tw,
/// added at each space), and horizontal scaling (Tz). The result is in points
/// (the displacement the text pen advances).
/// </summary>
public static class TextMeasurer
{
    /// <summary>Width in points of <paramref name="text"/> in the given font and size.</summary>
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

    /// <summary>
    /// Greedy word-wrap into lines that each fit within <paramref name="maxWidth"/>
    /// points. Existing line breaks ('\n') are preserved; a single word wider than
    /// the limit is placed on its own line rather than dropped.
    /// </summary>
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

    /// <summary>Greedy word-wrap using any <see cref="Font"/> (works for embedded fonts too).</summary>
    public static List<string> WrapText(Font font, double fontSize, string text, double maxWidth)
        => WrapText(font, fontSize, text, maxWidth, wordWidths: null!, out _);

    /// <summary>
    /// Greedy word-wrap that maintains a running width — measures each unique
    /// word once (via the <paramref name="wordWidths"/> cache when provided) and
    /// adds it to the running line width, instead of re-measuring the full
    /// candidate string per word. Returns the per-line widths via
    /// <paramref name="lineWidths"/> so callers can skip a second measurement
    /// pass for "what's the natural width of each line?".
    /// </summary>
    public static List<string> WrapText(Font font, double fontSize, string text, double maxWidth,
        System.Collections.Generic.Dictionary<string, double> wordWidths,
        out System.Collections.Generic.List<double> lineWidths)
    {
        var lines = new List<string>();
        var widths = new System.Collections.Generic.List<double>();
        double spaceWidth = MeasureWord(font, fontSize, " ", wordWidths);
        foreach (string paragraph in text.Split('\n'))
        {
            var current = new StringBuilder();
            double currentWidth = 0;
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                double wordWidth = MeasureWord(font, fontSize, word, wordWidths);
                double candidateWidth = current.Length == 0
                    ? wordWidth
                    : currentWidth + spaceWidth + wordWidth;
                if (current.Length == 0 || candidateWidth <= maxWidth)
                {
                    if (current.Length > 0) current.Append(' ');
                    current.Append(word);
                    currentWidth = candidateWidth;
                }
                else
                {
                    lines.Add(current.ToString());
                    widths.Add(currentWidth);
                    current.Clear();
                    current.Append(word);
                    currentWidth = wordWidth;
                }
            }
            lines.Add(current.ToString());
            widths.Add(currentWidth);
        }
        lineWidths = widths;
        return lines;
    }

    private static double MeasureWord(Font font, double fontSize, string word,
        System.Collections.Generic.Dictionary<string, double> cache)
    {
        if (cache is not null && cache.TryGetValue(word, out var w))
            return w;
        var measured = font.MeasureText(word, fontSize);
        if (cache is not null) cache[word] = measured;
        return measured;
    }
}
