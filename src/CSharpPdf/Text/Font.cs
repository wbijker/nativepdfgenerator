using CSharpPdf.Objects;

namespace CSharpPdf.Text;

/// <summary>
/// Base type for a font usable when drawing text. A font knows how to measure
/// text (glyph advance widths and vertical metrics) and how to build its PDF
/// font dictionary. The document deduplicates fonts by <see cref="Key"/> and
/// builds them once, at save time.
/// </summary>
public abstract class Font
{
    /// <summary>A stable key used to deduplicate identical fonts across the document.</summary>
    public abstract string Key { get; }

    /// <summary>The PostScript / BaseFont name written into the font dictionary.</summary>
    public abstract string BaseFont { get; }

    /// <summary>Advance width of <paramref name="c"/> in 1000-unit glyph space.</summary>
    public abstract int GetGlyphWidth(char c);

    /// <summary>The font's vertical metrics scaled to <paramref name="fontSize"/> points.</summary>
    public abstract FontVerticalMetrics GetVerticalMetrics(double fontSize);

    /// <summary>
    /// Populate the (reserved) font dictionary and add any sub-objects (descriptor,
    /// embedded program) to the store. Called once at save time.
    /// </summary>
    internal abstract void Build(PdfObjectStore store, PdfDictionary fontDictionary);

    /// <summary>
    /// Width in points of <paramref name="text"/> at <paramref name="fontSize"/>,
    /// accounting for character/word spacing and horizontal scaling (ISO 32000-1 §9.4.4).
    /// </summary>
    public double MeasureText(string text, double fontSize,
        double charSpacing = 0, double wordSpacing = 0, double horizontalScale = 100)
    {
        double total = 0;
        foreach (char c in text)
        {
            total += GetGlyphWidth(c) / 1000.0 * fontSize + charSpacing;
            if (c == ' ')
            {
                total += wordSpacing;
            }
        }
        return total * (horizontalScale / 100.0);
    }
}
