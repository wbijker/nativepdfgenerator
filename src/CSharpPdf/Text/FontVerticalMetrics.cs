namespace CSharpPdf.Text;

/// <summary>
/// A font's vertical metrics for a given size, in points (ISO 32000-1 §9.2.4 /
/// the AFM font dimensions). All distances are measured from the baseline, which
/// is the text origin (y = 0). Ascent and Descent are positive magnitudes above
/// and below the baseline; LineHeight and BaseLine are derived.
/// </summary>
public readonly record struct FontVerticalMetrics(
    double Ascent,
    double Descent,
    double LineGap,
    double CapHeight,
    double XHeight)
{
    /// <summary>The baseline-to-baseline distance for single-spaced lines: ascent + descent + line gap.</summary>
    public double LineHeight => Ascent + Descent + LineGap;

    /// <summary>Distance from the top of the line box down to the baseline (half the line gap sits above the ascent).</summary>
    public double BaseLine => Ascent + LineGap / 2.0;
}
