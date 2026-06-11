namespace PdfSpec.Fonts;

/// <summary>
/// A font's vertical metrics for a given size, in points (ISO 32000-1 §9.2.4 /
/// the AFM font dimensions). All distances are measured from the baseline, which
/// is the text origin (y = 0). All four ascent/descent magnitudes are positive
/// distances; LineHeight and BaseLine derive from the typographic pair.
///
/// <para>
/// <b>Typographic</b> (<see cref="Ascent"/> / <see cref="Descent"/>): the
/// designer's intended line-leading metric. Sourced from OS/2 sTypoAscender /
/// sTypoDescender on TrueType, or the AFM Ascender/Descender for the
/// Standard-14. Use this for stacking lines of body text.
/// </para>
///
/// <para>
/// <b>Windows-clip</b> (<see cref="WinAscent"/> / <see cref="WinDescent"/>):
/// the actual visible reach of typical glyphs. Sourced from OS/2 usWinAscent /
/// usWinDescent on TrueType. On most fonts this hugs the rendered text
/// closely; on decorative TTFs whose sTypoAscender undershoots their glyph
/// reach, this is the metric that matches what you see. Standard-14 fonts
/// don't expose a separate value here — Adobe's AFM Ascender already matches
/// visual reach — so WinAscent == Ascent and WinDescent == Descent for them.
/// </para>
/// </summary>
public readonly record struct FontVerticalMetrics(
    double Ascent,
    double Descent,
    double WinAscent,
    double WinDescent,
    double LineGap,
    double CapHeight,
    double XHeight)
{
    public double LineHeight => Ascent + Descent + LineGap;
    public double BaseLine => Ascent + LineGap / 2.0;
}
