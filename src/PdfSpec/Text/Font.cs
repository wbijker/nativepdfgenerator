using PdfSpec.Objects;

namespace PdfSpec.Fonts;

/// <summary>
/// Base type for a font usable when drawing text. A font knows how to measure
/// text and how to build its PDF font dictionary. The document deduplicates
/// fonts by <see cref="Key"/> and builds them once, at save time.
/// </summary>
public abstract class Font
{
    public abstract string Key { get; }
    public abstract string BaseFont { get; }

    /// <summary>Advance width of <paramref name="c"/> in 1000-unit glyph space.</summary>
    public abstract int GetGlyphWidth(char c);

    public abstract FontVerticalMetrics GetVerticalMetrics(double fontSize);

    /// <summary>
    /// The actual vertical extent the glyphs in <paramref name="text"/>
    /// occupy at <paramref name="fontSize"/>, in user units.
    /// <c>YMin</c> is the lowest point below the baseline (positive
    /// magnitude of descent), <c>YMax</c> is the highest point above the
    /// baseline. The default reuses <see cref="GetVerticalMetrics"/>'s
    /// typographic ascent / descent — tight enough for body fonts.
    /// Decorative faces that need a sample-specific bbox (TrueType
    /// override) report tighter values by walking per-glyph yMin /
    /// yMax from the glyf table.
    /// </summary>
    public virtual (double YMin, double YMax) MeasureExtentY(string text, double fontSize)
    {
        var m = GetVerticalMetrics(fontSize);
        return (m.Descent, m.Ascent);
    }

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
