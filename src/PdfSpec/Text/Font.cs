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
    /// occupy at <paramref name="fontSize"/>, in user units. <c>YMin</c>
    /// is the lowest point below the baseline (positive magnitude),
    /// <c>YMax</c> the highest above. The default classifies each char
    /// of the sample as cap / ascender / x-height / descender (an
    /// ASCII-Latin heuristic; non-letters fall to ascent as a safe
    /// upper bound) and picks the appropriate height field from
    /// <see cref="FontVerticalMetrics"/>. Exact for the standard 14
    /// fonts (they're designed to those bands); usable as a default
    /// for any font whose metrics fields are populated. TrueType
    /// overrides this with per-glyph bbox lookup from the glyf table,
    /// which is more accurate for decorative faces whose glyph reach
    /// drifts from the typographic numbers.
    /// </summary>
    public virtual (double YMin, double YMax) MeasureExtentY(string text, double fontSize)
    {
        var m = GetVerticalMetrics(fontSize);
        if (string.IsNullOrEmpty(text)) return (m.Descent, m.Ascent);

        double yMax = 0, yMin = 0;
        bool any = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c)) continue;
            any = true;

            if (c is >= 'a' and <= 'z')
            {
                switch (c)
                {
                    // Plain lowercase ascenders — reach the ascender line.
                    case 'b' or 'd' or 'f' or 'h' or 'i' or 'k' or 'l' or 't':
                        if (m.Ascent > yMax) yMax = m.Ascent;
                        break;
                    // 'j' is unique — ascends like a dotted i AND descends.
                    case 'j':
                        if (m.Ascent > yMax) yMax = m.Ascent;
                        if (m.Descent > yMin) yMin = m.Descent;
                        break;
                    // Lowercase descenders — x-height-tall plus a descender.
                    case 'g' or 'p' or 'q' or 'y':
                        if (m.XHeight > yMax) yMax = m.XHeight;
                        if (m.Descent > yMin) yMin = m.Descent;
                        break;
                    // a c e m n o r s u v w x z — x-height only.
                    default:
                        if (m.XHeight > yMax) yMax = m.XHeight;
                        break;
                }
            }
            else
            {
                // Caps, digits, punctuation, accented chars — treat as
                // ascent-bounded. Strictly caps reach CapHeight, but
                // some accented caps and some symbols reach higher
                // (Á, Ô, hash, etc.), so ascent is the safe upper bound.
                // For fonts where Ascent == CapHeight (most of the
                // standard 14) this collapses to the exact value.
                if (m.Ascent > yMax) yMax = m.Ascent;
            }
        }

        return any ? (yMin, yMax) : (m.Descent, m.Ascent);
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
